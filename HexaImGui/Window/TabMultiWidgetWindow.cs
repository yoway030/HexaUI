
namespace ELImGui.Window;

using Hexa.NET.ImGui;
using System;
using System.Numerics;

public class TabMultiWidgetWindow : MultiWidgetWindow
{
    public TabMultiWidgetWindow(string windowName, Vector2? parentPosition = null)
        : base(windowName, parentPosition)
    {
    }

    public ImGuiTabBarFlags TabFlags = ImGuiTabBarFlags.None;

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        if (ImGui.BeginTabBar(WindowName, TabFlags))
        {
            foreach (var widget in Widgets)
            {
                if (ImGui.BeginTabItem(widget.WidgetName) == true)
                {
                    widget.OnRender(utcNow, deltaSec);
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }
}
