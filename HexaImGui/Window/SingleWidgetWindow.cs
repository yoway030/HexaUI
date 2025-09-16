
namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;

public class SingleWidgetWindow<TWidget> : BaseWindow
    where TWidget : BaseWidget, new()
{
    public SingleWidgetWindow(string windowName)
        : base(windowName)
    {
        Widget = new TWidget();
        Widget.InitializeName($"{windowName}#{typeof(TWidget).Name}", windowName);
    }

    public TWidget Widget { get; set; }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        Widget.OnRender(utcNow, deltaSec);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        Widget.OnUpdate(utcNow, deltaSec);
    }
}
