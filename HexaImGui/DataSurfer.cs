using Hexa.NET.ImGui;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;

namespace HexaImGui;

public interface ISurfableData
{
    DateTime DateTime => DateTime.UtcNow;
    public string Level => string.Empty;
    public string Message => string.Empty;
    public string ToClipboard => string.Empty;
}

public class DataSurfer<TLog>
    where TLog : ISurfableData, new()
{
    public const int MaxLocalStorage = 10_000;

    public DataSurfer()
    {
    }

    private List<TLog> _localStorage = new();
    private ImGuiSelectionBasicStorage _selection = new();

    public ConcurrentQueue<TLog> Queue = new();
    public bool Freeze = false;

    public void AddMessage(TLog message)
    {
        Queue.Enqueue(message);
    }

    private void AdjustMessage()
    {
        while (Queue.TryDequeue(out var message) == true)
        {
            _localStorage.Add(message);
        }

        while (_localStorage.Count > MaxLocalStorage)
        {
            _localStorage.RemoveAt(0);
        }
    }

    public void Draw()
    {
        if (Freeze == false)
        {
            AdjustMessage();
        }

        ImGui.Begin("LogSurfer");

        ImGui.Checkbox("Freeze Log", ref Freeze);
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        ImGui.Text($"LogQueue:{Queue.Count}");
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
                sb.AppendLine(_localStorage[(int)data.Key].ToClipboard);
            }

            ImGui.SetClipboardText(sb.ToString());
        }

        if (ImGui.BeginTable("LogTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("✔", ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 180);
            ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            ImGuiMultiSelectIOPtr ms_io = ImGui.BeginMultiSelect(
                ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                _selection.Size,
                MaxLocalStorage);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= _localStorage.Count)
                    {
                        return 0;
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
                for (int rowIdx = clipper.DisplayStart; rowIdx < clipper.DisplayEnd; rowIdx++)
                {
                    if (rowIdx >= _localStorage.Count || rowIdx < 0)
                    {
                        break;
                    }

                    TLog log = _localStorage[rowIdx];

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    {
                        bool item_is_selected = _selection.Contains((uint)rowIdx);
                        ImGui.SetNextItemSelectionUserData(rowIdx);
                        ImGui.Selectable($"{rowIdx}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(log.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));

                    ImGui.TableNextColumn();
                    ImGui.TextColored(GetLevelColor(log.Level), log.Level);

                    ImGui.TableNextColumn();
                    float maxTextWidth = ImGui.GetColumnWidth();
                    ImGui.TextUnformatted(log.Message);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(log.Message);
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

    public Vector4 GetLevelColor(string level) => level switch
    {
        "ERROR" => new Vector4(1, 0.2f, 0.2f, 1),
        "WARN" => new Vector4(1, 0.7f, 0.2f, 1),
        "DEBUG" => new Vector4(0.5f, 0.7f, 1f, 1),
        _ => new Vector4(1, 1, 1, 1),
    };
}