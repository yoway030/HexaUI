
namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;

public class DoubleWidgetWindow<TWidget1, TWidget2> : BaseWindow
    where TWidget1 : BaseWidget
    where TWidget2 : BaseWidget
{
    public DoubleWidgetWindow(string windowName)
        : base(windowName)
    {
    }

    public TWidget1 WidgetFirst { get; set; } = null!;
    public TWidget2 WidgetSecond { get; set; } = null!;

    public virtual void InitializeWidgets(TWidget1 widgetFirst, TWidget2 widgetSecond)
    {
        WidgetFirst = widgetFirst;
        WidgetSecond = widgetSecond;
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        if (ImGui.BeginChild($"{WindowName}Panel1", ImGuiChildFlags.AutoResizeY) == true)
        {
            WidgetFirst.OnRender(utcNow, deltaSec);
        }
        ImGui.EndChild();

        if (ImGui.BeginChild($"{WindowName}Panel2", ImGuiChildFlags.AutoResizeY) == true)
        {
            WidgetSecond.OnRender(utcNow, deltaSec);
        }
        ImGui.EndChild();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        WidgetFirst.OnUpdate(utcNow, deltaSec);
        WidgetSecond.OnUpdate(utcNow, deltaSec);
    }
}
