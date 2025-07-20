using Hexa.NET.ImGui;
using System.Collections.Concurrent;
using System.Text;

namespace HexaImGui;

public class DataSurfer<TData>
    where TData : SurfableIndexingData, new()
{
    public DataSurfer(string widgetName = $"{nameof(DataSurfer<TData>)}", int maxLocalStorage = 1_000)
    {
        WidgetName = widgetName;
        MaxLocalStorage = maxLocalStorage;
    }

    private uint _index = 0;
    private List<TData> _localStorage = new();
    private ImGuiSelectionBasicStorage _selection = new();

    public string WidgetName { get; init; }
    public int MaxLocalStorage { get; init; }
    public ConcurrentQueue<TData> DataQueue = new();
    public bool Freeze = false;

    public void PushData(TData data)
    {
        DataQueue.Enqueue(data);
    }

    private void AdjustData()
    {
        while (DataQueue.TryDequeue(out var data) == true)
        {
            data.Index = _index++;
            _localStorage.Add(data);
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

        ImGui.Begin(WidgetName);

        ImGui.Checkbox("Freeze Log", ref Freeze);
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        ImGui.Text($"LogQueue:{DataQueue.Count}");
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        ImGui.Text($"Select:{_selection.Size} / {_localStorage.Count}");

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

        if (_localStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = _localStorage[0];

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
                _localStorage.Count);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= _localStorage.Count)
                    {
                        return unchecked((uint)-1);
                    }
                    return (uint)index;
                });

            _selection.ApplyRequests(ms_io);

            ImGuiListClipper clipper = new();
            clipper.Begin(_localStorage.Count);

            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            while (clipper.Step())
            {
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    if (displayIndex >= _localStorage.Count || displayIndex < 0)
                    {
                        break;
                    }

                    TData data = _localStorage[displayIndex];

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    {
                        bool item_is_selected = _selection.Contains((uint)displayIndex);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable(data.Label, item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

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
}