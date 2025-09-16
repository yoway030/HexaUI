
namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;

public class DoubleWidgetWindow<TWidget1, TWidget2> : BaseWindow
    where TWidget1 : BaseWidget, new()
    where TWidget2 : BaseWidget, new()
{
    public DoubleWidgetWindow(string windowName)
        : base(windowName)
    {
        WidgetFirst = new TWidget1();
        WidgetFirst.InitializeName($"{windowName}#{typeof(TWidget1).Name}", windowName);

        WidgetSecond = new TWidget2();
        WidgetSecond.InitializeName($"{windowName}#{typeof(TWidget2).Name}", windowName);
    }

    public TWidget1 WidgetFirst { get; set; }
    public TWidget2 WidgetSecond { get; set; }

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
