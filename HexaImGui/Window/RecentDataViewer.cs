using Hexa.NET.ImGui;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace HexaImGui.Window;

public class RecentDataViewer : BaseWindow
{
    public const int HighlightTimeMs = 2000;

    private class Entry
    {
        public DateTime UpdateTime;
        public string Key;
        public ViewableData Data;
        public int Count;

        public Entry(string key, ViewableData data)
        {
            Key = key;
            Data = data;
            UpdateTime = DateTime.UtcNow;
            Count = 1;
        }
    }

    public RecentDataViewer(string windowName)
        : base(windowName, 0)
    {
    }

    private readonly Dictionary<string, Entry> _entries = new();
    private readonly List<Entry> _sortedEntries = new();

    public readonly ConcurrentQueue<(string Key, ViewableData Data)> DataQueue = new();

    public void PushData(string key, ViewableData data)
    {
        if (string.IsNullOrWhiteSpace(key) || data == null)
        {
            return;
        }

        DataQueue.Enqueue((key, data));
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        bool needSort = false;

        while (DataQueue.TryDequeue(out var item))
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Data == null)
            {
                return;
            }

            if (_entries.TryGetValue(item.Key, out var entry))
            {
                entry.Data = item.Data;
                entry.UpdateTime = utcNow;
                entry.Count++;
            }
            else
            {
                entry = new Entry(item.Key, item.Data);
                _entries[item.Key] = entry;
            }

            needSort = true;
        }

        if (needSort == true)
        {
            _sortedEntries.Clear();
            _sortedEntries.AddRange(_entries.Values);
            _sortedEntries.Sort((a, b) =>
            {
                var time = b.UpdateTime.CompareTo(a.UpdateTime);
                if (time == 0)
                {
                    var count = b.Count - a.Count;
                    return count;
                }
                return time;
            });
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        if (_sortedEntries.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initEntry = _sortedEntries.First();
        if (ImGui.BeginTable($"##Table{WindowId}", 2 + initEntry.Data.GetColumnSetupActions().Count(), ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn($"Time##Column{WindowId}", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn($"Count##Column{WindowId}", ImGuiTableColumnFlags.WidthFixed, 60);
            foreach (var action in initEntry.Data.GetColumnSetupActions())
            {
                action();
            }
            ImGui.TableHeadersRow();

            foreach (var entry in _sortedEntries)
            {
                var data = entry.Data;

                // row시작
                ImGui.TableNextRow();

                // row bgcolor highlight
                {
                    var span = utcNow - entry.UpdateTime;
                    if (span <= TimeSpan.FromMilliseconds(HighlightTimeMs))
                    {
                        float color = 1.0f - (float)span.TotalMilliseconds / HighlightTimeMs;

                        var bgColor = new System.Numerics.Vector4(color, color, 0.0f, 0.7f);
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(bgColor));
                    }
                }

                ImGui.TableNextColumn();
                {
                    bool selected = ImGui.Selectable($"{entry.UpdateTime:yyyy-MM-dd HH:mm:ss.fff}##{entry.Key}", 
                        ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick);

                    if (selected)
                    {
                        Console.WriteLine($"Selected: {entry.Key} at {entry.UpdateTime:yyyy-MM-dd HH:mm:ss.fff}");
                    }
                }
                ImGui.TableNextColumn();
                {
                    ImGui.TextUnformatted(entry.Count.ToString());
                }

                foreach (var drawAction in data.GetFieldDrawActions())
                {
                    ImGui.TableNextColumn();
                    drawAction();
                }

                if (ImGui.IsItemHovered())
                {
                    data.RenderTooltip();
                }
            }

            ImGui.EndTable();
        }
    }
}