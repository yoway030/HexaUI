using Hexa.NET.ImGui;
using System.Collections.Concurrent;
using System.Text;

namespace HexaImGui;

public class DataSurfer<TData> : IDisposable
    where TData : SurfableIndexingData, new()
{
    public DataSurfer(string widgetName = $"{nameof(DataSurfer<TData>)}", int maxLocalStorage = 10_000, int widgetDepth = 0)
    {
        WidgetName = widgetName;
        MaxLocalStorage = maxLocalStorage;
        WidgetDepth = widgetDepth;
        DataIdx = 1;
    }

    public DataSurfer(DataSurfer<TData> parentWidget, int maxLocalStorage)
    {
        WidgetName = parentWidget.WidgetName;
        MaxLocalStorage = maxLocalStorage;

        WidgetDepth = parentWidget.WidgetDepth + 1;
        DataIdx = parentWidget.DataIdx;
    }

    public void Dispose()
    {
        if (_duplicateSurfer != null)
        {
            DuplicateWidget = false;
            _duplicateSurfer.Dispose();
            _duplicateSurfer = null;
        }
    }

    public string WidgetName { get; init; }
    public int WidgetDepth { get; init; }

    public bool Freeze = false;
    public int MaxLocalStorage { get; init; }
    public ConcurrentQueue<TData> DataQueue = new();
    private List<TData> _localStorage = new();
    private List<TData> _showStorage = null!;

    public uint DataIdx { get; private set; }
    private ImGuiSelectionBasicStorage _selection = new();
    
    public bool DuplicateWidget = false;
    private DataSurfer<TData>? _duplicateSurfer = null;

    public string FilterText = string.Empty;
    private List<TData>? _filteredStorage = null;

    public void DrawDataSurf()
    {
        if (Freeze == false)
        {
            AdjustData();
        }

        if (_duplicateSurfer != null)
        {
            _duplicateSurfer.DrawDataSurf();
        }

        _showStorage = _filteredStorage != null ? _filteredStorage : _localStorage;

        ImGui.Begin($"{WidgetName}#{WidgetDepth}");

        // input
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
        {
            OnWindowFocused();
        }

        // Freeze check box
        ImGui.Checkbox($"Freeze Log##{WidgetDepth}", ref Freeze);
        ImGuiHelper.HelpMarkerSameLine("큐에 쌓이고 있는 데이터 화면 출력을 정지");
        ImGuiHelper.SpacingSameLine();

        // Log Queue size
        ImGui.Text($"LogQueue:{DataQueue.Count}");
        ImGuiHelper.HelpMarkerSameLine("화면에 출력되지 않고 큐에 쌓인 데이터 수");
        ImGuiHelper.SpacingSameLine();

        // Selection info
        ImGui.Text($"Select:{_selection.Size} / {_localStorage.Count}");
        ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");

        // Duplicate widget checkbox
        if (ImGui.Checkbox($"Duplicate##{WidgetDepth}", ref DuplicateWidget) == true)
        {
            OnDuplicateWidgetCheckChange();
        }
        ImGuiHelper.HelpMarkerSameLine("동일 데이터 출력위젯 생성\n원본 데이터 출력과 필터링 데이터 출력을 분리하고 싶을 경우 사용");
        ImGuiHelper.SpacingSameLine();

        // filter input
        ImGui.Text("Filter:");
        ImGuiHelper.SpacingSameLine();
        if (ImGui.InputText($"##Filter{WidgetDepth}", ref FilterText, 256, ImGuiInputTextFlags.EnterReturnsTrue) == true)
        {
            OnFilterTextChange();
        }
        ImGuiHelper.HelpMarkerSameLine("엔터키로 필터링 적용");

        if (_showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            ImGui.End();
            return;
        }

        var initData = _showStorage[0];

        if (ImGui.BeginTable("Datas", initData.DrawableFieldCount + 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("##Idx", ImGuiTableColumnFlags.WidthFixed, 0);

            foreach (var action in initData.GetColumnSetupActions())
            {
                action();
            }

            ImGui.TableHeadersRow();

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
                    return (uint)index;
                });

            _selection.ApplyRequests(ms_io);

            ImGuiListClipper clipper = new();
            clipper.Begin(_showStorage.Count);

            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            while (clipper.Step())
            {
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    if (displayIndex >= _showStorage.Count || displayIndex < 0)
                    {
                        break;
                    }

                    TData data = _showStorage[displayIndex];

                    // row구분 인덱스값 별도 처리
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    {
                        bool item_is_selected = _selection.Contains((uint)displayIndex);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable($"##{data.Label}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

                    // SurfableIndexingData 필드 그리기
                    foreach (var drawAction in data.GetFieldDrawActions())
                    {
                        ImGui.TableNextColumn();
                        drawAction();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        data.TooltipDraw();
                    }
                }
            }

            if (Freeze == false)
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ms_io = ImGui.EndMultiSelect();
            _selection.ApplyRequests(ms_io);
            ImGui.EndTable();
        }

        ImGui.End();
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
                data.FieldsToString.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            {
                _filteredStorage.Add(data);
            }
        }

        while (_localStorage.Count > MaxLocalStorage)
        {
            _localStorage.RemoveAt(0);
        }
    }

    private void OnFilterTextChange()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            _filteredStorage = null;
        }
        else
        {
            _filteredStorage = [ .. _localStorage
                .Where(data => data.FieldsToString.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                .ToList(), ];
        }
    }

    private void OnDuplicateWidgetCheckChange()
    {
        if (DuplicateWidget == true)
        {
            _duplicateSurfer = new(this, 1_000);
        }
        else
        {
            _duplicateSurfer?.Dispose();
            _duplicateSurfer = null;
        }
    }

    private void OnWindowFocused()
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                var data = _selection.Storage.Data[i];
                sb.AppendLine(_showStorage[(int)data.Key].FieldsToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}