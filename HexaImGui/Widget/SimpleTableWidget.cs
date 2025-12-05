namespace ELImGui.Widget;

using Hexa.NET.ImGui;
using System;

public class SimpleTableWidget : BaseWidget
{
    public SimpleTableWidget(string widgetName, string ownerWindowName) : base(widgetName, ownerWindowName)
    {
    }

    public ImGuiTableFlags TableFlags =
        ImGuiTableFlags.SizingFixedFit
        | ImGuiTableFlags.Resizable
        | ImGuiTableFlags.ScrollX
        | ImGuiTableFlags.ScrollY;

    public string[] Headers = Array.Empty<string>();
    public List<string[]> Rows = new();

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        if (Headers.Length <= 0)
        {
            return;
        }

        if (ImGui.BeginTable($"{WidgetName}#table", Headers.Length + 1, TableFlags))
        {
            foreach (string h in Headers)
            {
                ImGui.TableSetupColumn(h, ImGuiTableColumnFlags.WidthFixed);
            }

            ImGui.TableHeadersRow();

            if (Rows.Count > 0)
            {
                foreach (string[] row in Rows)
                {
                    ImGui.TableNextRow();

                    for (int column = 0; column < Headers.Length; column++)
                    {
                        ImGui.TableSetColumnIndex(column);
                        if (column < row.Length)
                        {
                            ImGui.TextUnformatted(row[column]);

                            if (ImGui.IsItemHovered())
                            {
                                if (ImGui.BeginTooltip())
                                {
                                    ImGui.TextUnformatted(row[column]);
                                    ImGui.Separator();
                                    ImGui.TextUnformatted("Double click to copy to clipboard.");
                                    ImGui.EndTooltip();
                                }

                                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                                {
                                    ImGui.SetClipboardText(row[column]);
                                }
                            }
                        }
                        else
                        {
                            ImGui.TextUnformatted(String.Empty);
                        }
                    }
                }
            }

            ImGui.EndTable();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }
}
