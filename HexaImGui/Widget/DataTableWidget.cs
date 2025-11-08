namespace ELImGui.Window;

using Hexa.NET.ImGui;
using ELImGui.Utils;
using ELImGui.Widget;
using System.Collections.Concurrent;
using System.Text;
using System;
using System.Data;
using System.Numerics;

public class DataTableWidget<TData> : BaseWidget
{
    public DataTableWidget(DataTableRule<TData> rule, string widgetName)
        : this(rule, $"{nameof(DataTableWidget<TData>)}", String.Empty)
    {
    }

    public DataTableWidget(DataTableRule<TData> rule, string widgetName, string ownerWindowName, int maxLocalStorage = 10_000, int windowDepth = 0)
        : base(widgetName, ownerWindowName)
    {
        Rule = rule;
        MaxLocalStorage = maxLocalStorage;
        DataIdx = 1;

        _findWidget = new("Find", OwnerWindowName);
        _findWidget.FindingTargetChangedFunc += OnFindingTargetChanged;
        _findWidget.FoundedFocusMovedFunc += OnFoundedFocusMoved;
    }

    public DataTableRule<TData> Rule;
    public HighlightHelper HighlightHelper = new();

    public bool Freeze = false;
    public int MaxLocalStorage { get; init; }
    public ConcurrentQueue<TData> DataQueue = new();
    private List<IndexedRow<TData>> _localStorage = new();
    private List<IndexedRow<TData>> _showStorage = null!;

    public uint DataIdx { get; private set; }
    private ImGuiSelectionBasicStorage _selection = new();
    private IndexedRow<TData>? _focusedRow = null;

    private FindTextWidget<IndexedRow<TData>> _findWidget;

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        // Freeze check box
        ImGui.Checkbox($"Freeze##{OwnerWindowName}", ref Freeze);
        ImGuiHelper.HelpMarkerSameLine("데이터가 추가 대기\n" +
            "로그창의 경우 로그 행을 선택하면 자동 스크롤이 정지\n(로그는 계속 추가되고 있음)\n" +
            "Freeze는 로그창에 로그 추가를 대기\n");

        // Queue size
        ImGuiHelper.SpacingSameLine();
        ImGui.Text($"Queue:{DataQueue.Count}");
        ImGuiHelper.HelpMarkerSameLine("추가 대기 중인 데이터 수");

        // Selection info
        ImGuiHelper.SpacingSameLine();
        ImGui.Text($"Select:{_selection.Size}/{_localStorage.Count}");
        ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");

        // Filter
        ImGuiHelper.SpacingSameLine();
        _findWidget.RenderImObject(utcNow, deltaSec);

        if (_showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = _showStorage[0];

        if (ImGui.BeginTable("Datas", Rule.Columns.Length + 1, Rule.TableFlags))
        {
            // 헤더 고정
            ImGui.TableSetupScrollFreeze(0, 1);

            // 설정값 로딩
            float currentRowHeight = ImGui.GetTextLineHeightWithSpacing();

            // 선택기능을 위한 첫번째 컬럼
            ImGui.TableSetupColumn($"##Idx#{OwnerWindowName}", ImGuiTableColumnFlags.WidthFixed, 0);
            Rule.SetupColumns();
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

            int displayRowCount = 0;
            while (clipper.Step())
            {
                // 클리핑 처리
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    displayRowCount++;
                    Vector4 colorEffect = Vector4.Zero;
                    var indexedRow = _showStorage[displayIndex];
                    string fieldsToString = Rule.RowToString(indexedRow.RowData);
                    bool isHighlighted = _findWidget.IsMachted(fieldsToString);
                    bool isRowHovered = false;

                    // color 조정
                    colorEffect = _findWidget.IsMachted(fieldsToString) ? HighlightHelper.HighLightColor : Vector4.Zero;
                    colorEffect = _focusedRow?.Index == indexedRow.Index ? new(1.0f, 0.0f, 0.0f, 0.2f) : colorEffect;

                    // row시작
                    ImGui.TableNextRow();

                    if (colorEffect != Vector4.Zero)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(colorEffect));
                    }

                    Rule.RenderRowHead(indexedRow.RowData);

                    ImGui.TableNextColumn();
                    {
                        // 선택기능을 위한 첫번째 컬럼
                        bool item_is_selected = _selection.Contains(indexedRow.Index);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable($"##{indexedRow.Index}#{OwnerWindowName}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

                        if (ImGui.IsItemHovered())
                        {
                            isRowHovered = true;
                        }
                    }

                    // 데이터 필드 출력
                    Rule.RenderRow(indexedRow.RowData);
                    Rule.RenderRowFoot(indexedRow.RowData);

                    if (isRowHovered)
                    {
                        Rule.RenderTooltip(indexedRow.RowData);
                    }
                }
            }

            if (_focusedRow != null)
            {
                // 포커스된 행이 있으면 해당 행이 보이도록 스크롤 조정
                float posY = 0;
                if (_findWidget.IsOnlyFiltered == true)
                {
                    posY = ImGui.GetCursorStartPos().Y + _findWidget.FoundedFocusIndex * currentRowHeight;
                }
                else
                {
                    posY = ImGui.GetCursorStartPos().Y + (float)(_focusedRow?.Index ?? 0) * currentRowHeight;
                }
                    
                ImGui.SetScrollFromPosY(posY, 0.5f);
            }
            else if (Freeze == false && _selection.Size == 0)
            {
                // Freeze가 걸려있지 않고, 선택된 데이터가 없으면 마지막 행이 보이도록 스크롤 조정
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

        if (_findWidget.IsFinding && _findWidget.IsOnlyFiltered && _findWidget.FoundedList != null)
        {
            _showStorage = _findWidget.FoundedList;
        }
        else
        {
            _showStorage = _localStorage;
        }
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

            string rowToString = Rule.RowToString(indexedRow.RowData);
            if (_findWidget.IsMachted(rowToString) == true)
            {
                _findWidget.FoundedList?.Add(indexedRow);
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

    private uint GetLocalStorageStartIndex()
    {
        if (_localStorage.Any() == false)
        {
            return 0;
        }

        return _localStorage[0].Index;
    }

    private void OnFindingTargetChanged()
    {
        _selection.Clear();
        _focusedRow = null;

        _findWidget.FoundedList =
            [ .. _localStorage
                .Where(indexedRow => _findWidget.IsMachted(Rule.RowToString(indexedRow.RowData)))
                .ToList(), ];
    }

    private void OnFoundedFocusMoved()
    {
        if (_findWidget.IsFinding == false)
        {
            return;
        }

        _selection.Clear();

        var focusedRow = _findWidget.FoundedList?[_findWidget.FoundedFocusIndex - 1];
        int showStorageIndex = _showStorage.FindIndex(r => r.Index == focusedRow?.Index);
        if (showStorageIndex != -1)
        {
            _focusedRow = _showStorage[showStorageIndex];
        }
    }

    public override void OnWindowFocused(BaseWindow baseWindow)
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            var sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                uint dataKey = _selection.Storage.Data[i].Key;
                uint storageStartIndex = GetLocalStorageStartIndex();
                uint dataStorageIndex = dataKey - storageStartIndex;

                if (dataStorageIndex < 0 || dataStorageIndex >= _localStorage.Count)
                {
                    continue;
                }

                string rowToString = Rule.RowToString(_localStorage[(int)dataStorageIndex].RowData);
                sb.AppendLine(rowToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}
