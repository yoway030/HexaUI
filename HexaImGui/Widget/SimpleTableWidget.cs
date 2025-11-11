namespace ELImGui.Widget;

using Hexa.NET.ImGui;
using System;
using System.Net.Mime;

public class SimpleTableWidget : BaseWidget
{
    public SimpleTableWidget(string widgetName, string ownerWindowName) : base(widgetName, ownerWindowName)
    {
    }

    public List<string> Headers = new();

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.ContextMenuInBody;

        if (Headers.Count <= 0)
        {
            return;
        }

        if (ImGui.BeginTable("table1", Headers.Count, flags))
        {
            // Display headers so we can inspect their interaction with borders
            // (Headers are not the main purpose of this section of the demo, so we are not elaborating on them now. See other sections for details)
            if (Headers.Count > 0)
            {
                ImGui.TableSetupColumn("One");
                ImGui.TableSetupColumn("Two");
                ImGui.TableSetupColumn("Three");
                ImGui.TableHeadersRow();
            }

            for (int row = 0; row < 5; row++)
            {
                ImGui.TableNextRow();
                for (int column = 0; column < 3; column++)
                {
                    ImGui.TableSetColumnIndex(column);
                    ImGui.TextUnformatted($"asdf");
                }
            }
            ImGui.EndTable();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }
}
