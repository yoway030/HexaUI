using Hexa.NET.ImGui;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HexaImGui;

public class LogViewer
{
    public const int MaxLocalStorage = 1000;

    public LogViewer()
    {
    }

    public ConcurrentQueue<LogMessage> MessageQueue = new ConcurrentQueue<LogMessage>();

    public List<LogMessage> MessageLocalStorage = new List<LogMessage>();

    public bool Freeze = false;

    public void AddMessage(LogMessage message)
    {
        MessageQueue.Enqueue(message);
    }

    public void AdjustMessage()
    {
        while (MessageQueue.TryDequeue(out var message) == true)
        {
            MessageLocalStorage.Add(message);
        }

        while (MessageLocalStorage.Count > MaxLocalStorage)
        {
            MessageLocalStorage.RemoveAt(0);
        }
    }

    public void Draw()
    {
        if (Freeze == false)
        {
            AdjustMessage();
        }

        ImGui.Begin("LogViewer");

        ImGui.Checkbox("Freeze Log", ref Freeze);

        if (ImGui.BeginTable("LogTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, 300)))
        {
            ImGui.TableSetupScrollFreeze(0, 1); // 헤더 고정 (X=0, Y=1)
            ImGui.TableSetupColumn("✔", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 180);
            ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            int rowIdx = 0;
            foreach (var log in MessageLocalStorage)
            {
                ImGui.TableNextRow();

                if (ImGui.TableNextColumn())
                {
                    ImGui.PushID(rowIdx);
                    ImGui.Checkbox("", ref log.IsChecked);
                    ImGui.PopID();
                }


                if (ImGui.TableNextColumn())
                {
                    ImGui.TextUnformatted(log.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                if (ImGui.TableNextColumn())
                {
                    ImGui.TextColored(GetLevelColor(log.Level), log.Level);
                }

                if (ImGui.TableNextColumn())
                {
                    float maxTextWidth = ImGui.GetColumnWidth();
                    ImGui.TextUnformatted(TruncateWithEllipsis(log.Message, maxTextWidth));
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(log.Message);
                    }
                }

                rowIdx++;
            }

            if (Freeze == false)
            {
                ImGui.SetScrollHereY(1.0f);
            }

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

    public string TruncateWithEllipsis(string text, float maxWidth)
    {
        var size = ImGui.CalcTextSize(text);
        if (size.X <= maxWidth)
            return text;

        int left = 0;
        int right = text.Length;
        string result = text;

        while (left < right)
        {
            int mid = (left + right) / 2;
            var sub = text[..mid] + "...";
            var subSize = ImGui.CalcTextSize(sub);
            if (subSize.X > maxWidth)
            {
                right = mid;
            }
            else
            {
                result = sub;
                left = mid + 1;
            }
        }

        return result;
    }
}

public record LogMessage
{
    public DateTime DateTime { get; set; } = DateTime.MinValue;
    
    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsChecked = false;

    public bool IsSelected = false;
}
