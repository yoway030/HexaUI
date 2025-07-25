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
        WidgetDepth = parentWidget.WidgetDepth + 1;
        WidgetName = $"{parentWidget.WidgetName}##{WidgetDepth}";
        MaxLocalStorage = maxLocalStorage;
        DataIdx = parentWidget.DataIdx;
    }

    public void Dispose()
    {
        if (_filteredSurfer != null)
        {
            FilterWidget = false;
            _filteredSurfer.Dispose();
            _filteredSurfer = null;
        }
    }

    public string WidgetName { get; init; }
    public int WidgetDepth { get; init; }

    public ConcurrentQueue<TData> DataQueue = new();
    private List<TData> _localStorage = new();
    public uint DataIdx { get; private set; }
    private ImGuiSelectionBasicStorage _selection = new();
    public int MaxLocalStorage { get; init; }
    public bool Freeze = false;

    public string FilterText = string.Empty;
    public bool FilterWidget = false;
    private DataSurfer<TData>? _filteredSurfer = null;
    private List<TData>? _filteredStorage = null;

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

            if (_filteredStorage != null &&
                data.FieldsToString.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            {
                _filteredStorage.Add(data);
            }

            _filteredSurfer?.PushData(data);
        }

        while (_localStorage.Count > MaxLocalStorage)
        {
            _localStorage.RemoveAt(0);
        }
    }

    public void DrawDataSurf()
    {
        if (Freeze == false)
        {
            AdjustData();
        }

        if (_filteredSurfer != null)
        {
            _filteredSurfer.DrawDataSurf();
        }

        ImGui.Begin(WidgetName);

        ImGui.Checkbox($"Freeze Log##{WidgetDepth}", ref Freeze);
        ImGui.SameLine(); 
        ImGui.Spacing();
        ImGui.SameLine();
        ImGui.Text($"LogQueue:{DataQueue.Count}");
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        ImGui.Text($"Select:{_selection.Size} / {_localStorage.Count}");

        ImGui.Text("Filter:");
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        if (ImGui.InputText($"##Filter{WidgetDepth}", ref FilterText, 256, ImGuiInputTextFlags.EnterReturnsTrue) == true)
        {
            OnFilterTextChange();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        if (ImGui.Checkbox($"FilterWidget##{WidgetDepth}", ref FilterWidget) == true)
        {
            OnFilterWidgetCheckChange();
        }
        
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                var data = _selection.Storage.Data[i];
                sb.AppendLine(_localStorage[(int)data.Key].FieldsToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }

        var showStorage = _filteredStorage != null ? _filteredStorage : _localStorage;

        if (showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            ImGui.End();
            return;
        }

        var initData = showStorage[0];

        if (ImGui.BeginTable("Datas", initData.DrawableFieldCount + 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Idx", ImGuiTableColumnFlags.WidthFixed, 200);

            for (int i = 0; i < initData.DrawableFieldCount; i++)
            {
                initData.FieldSetupColumn(i);
            }

            ImGui.TableHeadersRow();

            ImGuiMultiSelectIOPtr ms_io = ImGui.BeginMultiSelect(
                ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                _selection.Size,
                showStorage.Count);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= showStorage.Count)
                    {
                        return unchecked((uint)-1);
                    }
                    return (uint)index;
                });

            _selection.ApplyRequests(ms_io);

            ImGuiListClipper clipper = new();
            clipper.Begin(showStorage.Count);

            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            while (clipper.Step())
            {
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    if (displayIndex >= showStorage.Count || displayIndex < 0)
                    {
                        break;
                    }

                    TData data = showStorage[displayIndex];

                    // row구분 인덱스값 별도 처리
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    {
                        bool item_is_selected = _selection.Contains((uint)displayIndex);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable(data.Label, item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

                    // SurfableIndexingData 필드 그리기
                    for (int i = 0; i < data.DrawableFieldCount; i++)
                    {
                        ImGui.TableNextColumn();
                        data.FieldDraw(i);
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

    private void OnFilterTextChange()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            _filteredStorage = null;
        }
        else
        {
            _filteredStorage =
            [
                .. _localStorage
                        .Where(data => data.FieldsToString.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                        .ToList(),
                ];
        }
    }

    private void OnFilterWidgetCheckChange()
    {
        if (FilterWidget == true)
        {
            _filteredSurfer = new(this, 1_000);
        }
        else
        {
            _filteredSurfer?.Dispose();
            _filteredSurfer = null;
        }
    }
}