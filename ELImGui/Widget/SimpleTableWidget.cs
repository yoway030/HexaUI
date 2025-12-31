namespace ELImGui.Widget;

using Hexa.NET.ImGui;
using System;
using Utils;

public class SimpleTableWidget : BaseWidget
{
    public enum ColumnType
    {
        Text,
        Button,
    }

    public class ColumnInfo(ColumnType columnType, string text, Action? buttonAction)
    {
        public ColumnType ColumnType { get; set; } = columnType;
        public string Text { get; set; } = text;
        public Action? ButtonAction { get; set; } = buttonAction;

        public static implicit operator ColumnInfo(string text)
        {
            return new ColumnInfo(ColumnType.Text, text, null);
        }
    }

    public SimpleTableWidget(string widgetName, string ownerWindowName) : base(widgetName, ownerWindowName)
    {
    }

    public ImGuiTableFlags TableFlags =
        ImGuiTableFlags.SizingFixedFit
        | ImGuiTableFlags.Resizable
        | ImGuiTableFlags.ScrollX
        | ImGuiTableFlags.ScrollY;

    public string[] Headers = Array.Empty<string>();
    public List<ColumnInfo[]> Rows = new();

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
                int rowIndex = 0;
                foreach (var row in Rows)
                {
                    ImGui.TableNextRow();

                    for (int column = 0; column < Headers.Length; column++)
                    {
                        ImGui.TableSetColumnIndex(column);
                        if (column < row.Length)
                        {
                            var columnInfo = row[column];
                            switch (columnInfo.ColumnType)
                            {
                                case ColumnType.Text:
                                    ImGuiHelper.TextWithTooltip(columnInfo.Text, columnInfo.Text);
                                    break;
                                case ColumnType.Button:
                                    if (ImGui.Button($"{columnInfo.Text}##Column_{rowIndex}"))
                                    {
                                        columnInfo.ButtonAction?.Invoke();
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            ImGui.TextUnformatted(String.Empty);
                        }
                    }

                    ++rowIndex;
                }
            }

            ImGui.EndTable();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
    }
}
