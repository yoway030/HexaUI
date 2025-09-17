namespace ELImGui.Window;

using Hexa.NET.ImGui;
using ELImGui.Utils;
using ELImGui.Widget;
using System.Collections.Concurrent;
using System.Text;
using System.Numerics;
using System;
using System.Data;

public class DataTableWidget<TData> : BaseWidget
{
    public static readonly Vector4 ColorTextHighLight = new(0.0f, 1.0f, 0.0f, 0.5f);
    public static readonly Vector4 ColorBgHighLight = new(0.4f, 1.0f, 0.4f, 0.3f);

    public DataTableWidget(string widgetName, DataTableRole<TData> role)
        : this($"{nameof(DataTableWidget<TData>)}", role, String.Empty)
    {
    }

    public DataTableWidget(string widgetName, DataTableRole<TData> role, string parentWindowName, int maxLocalStorage = 10_000, int windowDepth = 0)
        : base(widgetName, parentWindowName)
    {
        Role = role;
        MaxLocalStorage = maxLocalStorage;
        DataIdx = 1;

        _filterWidget = new("Filter", ParentWindowName);
        _filterWidget.FilterChangingFunc += OnFilterChanging;
    }

    public DataTableRole<TData> Role;

    public bool Freeze = false;
    public int MaxLocalStorage { get; init; }
    public ConcurrentQueue<TData> DataQueue = new();
    private List<IndexedRow<TData>> _localStorage = new();
    private List<IndexedRow<TData>> _showStorage = null!;

    public uint DataIdx { get; private set; }
    private ImGuiSelectionBasicStorage _selection = new();

    private FilterWidget _filterWidget;
    private List<IndexedRow<TData>>? _filteredStorage = null;

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        // Freeze check box
        ImGui.Checkbox($"Freeze##{ParentWindowName}", ref Freeze);
        ImGuiHelper.HelpMarkerSameLine("큐에 쌓이고 있는 데이터 화면 출력을 정지");
        ImGuiHelper.SpacingSameLine();

        // Queue size
        ImGui.Text($"Queue:{DataQueue.Count}");
        ImGuiHelper.HelpMarkerSameLine("화면에 출력되지 않고 큐에 쌓인 데이터 수");
        ImGuiHelper.SpacingSameLine();

        // Selection info
        ImGui.Text($"Select:{_selection.Size}/{_localStorage.Count}");
        ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");
        ImGuiHelper.SpacingSameLine();

        // Filter
        _filterWidget.RenderImObject(utcNow, deltaSec);
        ImGuiHelper.HelpMarkerSameLine(
            "엔터키로 필터링 적용",
            "Highlight를 끌 경우 필터링된 데이터만 출력");

        if (_showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = _showStorage[0];

        if (ImGui.BeginTable("Datas", Role.Columns.Length + 1, Role.TableFlags))
        {
            // 헤더 고정
            ImGui.TableSetupScrollFreeze(0, 1);

            // 선택기능을 위한 첫번째 컬럼
            ImGui.TableSetupColumn($"##Idx#{ParentWindowName}", ImGuiTableColumnFlags.WidthFixed, 0);
            Role.SetupColumns();
            ImGui.TableHeadersRow();

            // 멀티셀렉트 처리
            var ms_io = ImGui.BeginMultiSelect(
                ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                _selection.Size,
                _showStorage.Count);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= _showStorage.Count)
                    {
                        return unchecked((uint)-1);
                    }

                    return _showStorage[index].Index;
                });
            _selection.ApplyRequests(ms_io);

            // 대량 데이터를 위한 클리퍼
            ImGuiListClipper clipper = new();
            clipper.Begin(_showStorage.Count);
            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            while (clipper.Step())
            {
                // 클리핑 처리
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    var indexedRow = _showStorage[displayIndex];
                    string fieldsToString = Role.RowToString(indexedRow.RowData);
                    bool isHighlighted = _filterWidget.IsFiltering &&
                        fieldsToString.Contains(_filterWidget.FilterText, StringComparison.OrdinalIgnoreCase) == true;

                    // row시작
                    ImGui.TableNextRow();

                    if (isHighlighted)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ColorBgHighLight));
                    }

                    ImGui.TableNextColumn();
                    {
                        // 선택기능을 위한 첫번째 컬럼
                        bool item_is_selected = _selection.Contains(indexedRow.Index);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable($"##{indexedRow.Index}#{ParentWindowName}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

                    // 데이터 필드 출력
                    Role.RenderRow(indexedRow.RowData);

                    if (ImGui.IsItemHovered())
                    {
                        Role.RenderTooltip(indexedRow.RowData);
                    }
                }
            }

            if (Freeze == false && _selection.Size == 0)
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ms_io = ImGui.EndMultiSelect();

            try
            {
                _selection.ApplyRequests(ms_io);
            }
            catch (Exception)
            {
                _selection.Clear();
            }

            ImGui.EndTable();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        if (Freeze == false)
        {
            AdjustData();
        }

        _showStorage = _filteredStorage ?? _localStorage;
    }

    public void PushData(TData data)
    {
        DataQueue.Enqueue(data);
    }

    private void AdjustData()
    {
        while (DataQueue.TryDequeue(out var data) == true)
        {
            uint index = DataIdx++;
            var indexedRow = new IndexedRow<TData>(index, data);
            _localStorage.Add(indexedRow);

            string rowToString = Role.RowToString(indexedRow.RowData);

            if (_filteredStorage != null &&
                rowToString.Contains(_filterWidget.FilterText, StringComparison.OrdinalIgnoreCase))
            {
                _filteredStorage.Add(indexedRow);
            }
        }

        // 선택한 데이터가 없는 경우 MaxLocalStorage 적용
        if (_selection.Size == 0)
        {
            // 로컬스토리지는 MaxLocalStorage 만큼만 데이터 저장
            while (_localStorage.Count > MaxLocalStorage)
            {
                _localStorage.RemoveAt(0);
            }

            // 선택한 데이터가 있는 경우 MaxLocalStorage 적용을 유예시키는 이유는 MultiSelect중 앞의 데이터가 삭제될때,
            // 선택된 데이터가 삭제될수도 있기 때문이고(크래시등의 문제 확인 필요)
            // 선택이 정상적으로 유지되지 않는 버그 스러운 문제 때문.
        }

        // 선택한 데이터가 있는 경우라도, 현재 데이터의 범위가 MaxLocalStorage * 2를 초과하면 선택된 데이터를 초기화시키고, 삭제될수 있도록 처리
        if (_localStorage.Count > MaxLocalStorage * 2)
        {
            _selection.Clear();
        }
    }

    private void OnFilterChanging()
    {
        _filteredStorage = _filterWidget.IsOnlyFileterd == true ?
            [ .. _localStorage
                .Where(indexedRow => Role.RowToString(indexedRow.RowData).Contains(_filterWidget.FilterText, StringComparison.OrdinalIgnoreCase))
                .ToList(), ]
            : null;
    }

    public void OnWindowFocused()
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            var sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                uint dataIndexKey = _selection.Storage.Data[i].Key;
                uint showStorageStartIndex = DataIdx - (uint)_showStorage.Count;
                uint surfableDataIndexInShowStorage = dataIndexKey - showStorageStartIndex;

                if (surfableDataIndexInShowStorage < 0 || surfableDataIndexInShowStorage >= _showStorage.Count)
                {
                    continue;
                }

                string rowToString = Role.RowToString(_showStorage[(int)surfableDataIndexInShowStorage].RowData);
                sb.AppendLine(rowToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}
