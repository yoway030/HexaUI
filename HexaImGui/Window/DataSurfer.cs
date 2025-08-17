using Hexa.NET.ImGui;
using ELImGui.Utils;
using ELImGui.Widget;
using System.Collections.Concurrent;
using System.Text;

namespace ELImGui.Window;

public class DataSurfer<TData> : BaseWindow, IDisposable
    where TData : SurfableIndexingData, new()
{
    public DataSurfer(string windowName = $"{nameof(DataSurfer<TData>)}", int maxLocalStorage = 10_000, int windowDepth = 0)
        : base(windowName, windowDepth)
    {
        MaxLocalStorage = maxLocalStorage;
        DataIdx = 1;

        _filterWidget = new("Filter", WindowId);
        _filterWidget.FilterChangingFunc += OnFilterChanging;
    }

    public DataSurfer(DataSurfer<TData> parentWnd, int maxLocalStorage)
        : base(parentWnd.WindowName, parentWnd.WindowDepth + 1)
    {
        MaxLocalStorage = maxLocalStorage;
        DataIdx = parentWnd.DataIdx;

        _filterWidget = new("Filter", WindowId);
        _filterWidget.FilterChangingFunc += OnFilterChanging;
    }

    public void Dispose()
    {
        if (_duplicateSurfer != null)
        {
            DuplicateWindow = false;
            _duplicateSurfer.Dispose();
            _duplicateSurfer = null;
        }
    }

    public bool Freeze = false;
    public int MaxLocalStorage { get; init; }
    public ConcurrentQueue<TData> DataQueue = new();
    private List<TData> _localStorage = new();
    private List<TData> _showStorage = null!;

    public uint DataIdx { get; private set; }
    private ImGuiSelectionBasicStorage _selection = new();
    
    public bool DuplicateWindow = false;
    private DataSurfer<TData>? _duplicateSurfer = null;

    private FilterWidget _filterWidget;
    private List<TData>? _filteredStorage = null;

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        _duplicateSurfer?.RenderVisualizer(utcNow, deltaSec);
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        // Freeze check box
        ImGui.Checkbox($"Freeze##{WindowId}", ref Freeze);
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

        // Duplicate window checkbox
        if (ImGui.Checkbox($"Duplicate##{WindowId}", ref DuplicateWindow) == true)
        {
            OnDuplicateWindowCheckChange();
        }
        ImGuiHelper.HelpMarkerSameLine("동일 데이터 출력위젯 생성\n원본 데이터 출력과 필터링 데이터 출력을 분리하고 싶을 경우 사용");

        // Filter
        _filterWidget.RenderWidget(utcNow, deltaSec);
        ImGuiHelper.HelpMarkerSameLine(
            "엔터키로 필터링 적용",
            "Highlight를 끌 경우 필터링된 데이터만 출력");

        if (_showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = _showStorage[0];

        if (ImGui.BeginTable("Datas", initData.GetColumnSetupActions().Count() + 1, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            // 헤더 고정
            ImGui.TableSetupScrollFreeze(0, 1);

            // 선택기능을 위한 첫번째 컬럼
            ImGui.TableSetupColumn($"##Idx#{WindowId}", ImGuiTableColumnFlags.WidthFixed, 0);

            // 데이터 출력하는 컬럼
            foreach (var action in initData.GetColumnSetupActions())
            {
                action();
            }

            ImGui.TableHeadersRow();

            // 멀티셀렉트 처리
            ImGuiMultiSelectIOPtr ms_io = ImGui.BeginMultiSelect(
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
                    return (uint)_showStorage[index].Index;
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
                    TData data = _showStorage[displayIndex];
                    var fieldsToString = data.FieldsToString;
                    bool isHighlighted = _filterWidget.IsFiltering &&
                        fieldsToString.Contains(_filterWidget.FilterText, StringComparison.OrdinalIgnoreCase) == true;

                    // row시작
                    ImGui.TableNextRow();

                    if (isHighlighted)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ColorTextHighLight));
                    }

                    ImGui.TableNextColumn();
                    {
                        // 선택기능을 위한 첫번째 컬럼
                        bool item_is_selected = _selection.Contains(data.Index);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable($"##{data.IndexString}#{WindowId}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

                    // 데이터 필드 출력
                    foreach (var drawAction in data.GetFieldDrawActions())
                    {
                        ImGui.TableNextColumn();
                        drawAction();
                    }

                    if (isHighlighted)
                    {
                        ImGui.PopStyleColor();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        data.RenderTooltip();
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

        _showStorage = _filteredStorage == null ? _localStorage : _filteredStorage;
        _duplicateSurfer?.OnUpdate(utcNow, deltaSec);
    }

    public void PushData(TData data)
    {
        DataQueue.Enqueue(data);
    }

    private void AdjustData()
    {
        while (DataQueue.TryDequeue(out var data) == true)
        {
            data.Index = DataIdx++;
            _localStorage.Add(data);
            _duplicateSurfer?.PushData(data);

            if (_filteredStorage != null &&
                data.FieldsToString.Contains(_filterWidget.FilterText, StringComparison.OrdinalIgnoreCase))
            {
                _filteredStorage.Add(data);
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
        if (_filterWidget.IsOnlyFileterd == true)
        {
            _filteredStorage = [ .. _localStorage
                .Where(data => data.FieldsToString.Contains(_filterWidget.FilterText, StringComparison.OrdinalIgnoreCase))
                .ToList(), ];
        }
        else
        {
            _filteredStorage = null;
        }
    }

    private void OnDuplicateWindowCheckChange()
    {
        if (DuplicateWindow == true)
        {
            _duplicateSurfer = new(this, 1_000);
        }
        else
        {
            _duplicateSurfer?.Dispose();
            _duplicateSurfer = null;
        }
    }

    public override void OnWindowFocused()
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                var surfableDataIndex = _selection.Storage.Data[i].Key;
                var showStorageStartIndex = DataIdx - (uint)_showStorage.Count;
                var surfableDataIndexInShowStorage = surfableDataIndex - showStorageStartIndex;

                if (surfableDataIndexInShowStorage < 0 || surfableDataIndexInShowStorage >= _showStorage.Count)
                {
                    continue;
                }

                sb.AppendLine(_showStorage[(int)surfableDataIndexInShowStorage].FieldsToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}
