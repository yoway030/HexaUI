using Hexa.NET.ImGui;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HexaImGui.Window;

public class RecentDataViewer : BaseWindow
{
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

    public readonly ConcurrentQueue<(string Key, ViewableData Data)> DataQueue = new();

    private readonly Dictionary<string, Entry> _entries = new();
    private readonly List<Entry> _sortedEntries = new();

    public void PushData(string key, ViewableData data)
    {
        if (string.IsNullOrWhiteSpace(key) || data == null)
        {
            return;
        }

        DataQueue.Enqueue((key, data));
    }

    private void AdjustData()
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
                entry.UpdateTime = DateTime.UtcNow;
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
                var count = b.Count - a.Count;
                if (count == 0)
                {
                    return b.UpdateTime.CompareTo(a.UpdateTime);
                }
                return count;
            });
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        var initEntry = _sortedEntries.First();

        if (ImGui.BeginTable($"##Table{WindowId}", 2 + initEntry.Data.GetColumnSetupActions().Count(), ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn($"Time##Column{WindowId}", ImGuiTableColumnFlags.WidthFixed, 60);

            foreach (var action in initEntry.Data.GetColumnSetupActions())
            {
                action();
            }

            ImGui.TableSetupColumn($"Count##Column{WindowId}", ImGuiTableColumnFlags.WidthFixed, 60);

            ImGui.TableHeadersRow();

            foreach (var entry in _sortedEntries)
            {
                var data = entry.Data;

                ImGui.TableNextColumn();
                {
                    ImGui.TextUnformatted(entry.UpdateTime.ToString());
                }

                foreach (var drawAction in data.GetFieldDrawActions())
                {
                    ImGui.TableNextColumn();
                    drawAction();
                }

                ImGui.TableNextColumn();
                {
                    ImGui.TextUnformatted(entry.Count.ToString());
                }

                if (ImGui.IsItemHovered())
                {
                    data.RenderTooltip();
                }
            }

            ImGui.EndTable();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        AdjustData();
    }
}