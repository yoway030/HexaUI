namespace ELImGui.Window;

using Hexa.NET.ImNodes;
using ELImGui.NodeEditor;
using System;

public class NodeViewer : BaseWindow
{
    public NodeViewer(string windowName = nameof(NodeViewer))
        : base(windowName, null)
    {
        //InitSample();
    }

    public NodeEditor Editor { get; } = new();

    public readonly List<BaseWindow> ChildWindows = new();

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        Editor.Render(utcNow, deltaSec);
        ChildWindows.ForEach(w => w.RenderImObject(utcNow, deltaSec, imInternalContext));
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        Editor.Update(utcNow, deltaSec);
        ChildWindows.ForEach(w => w.UpdateImObject(utcNow, deltaSec));
    }

    public void InitSample()
    {
        Editor.CreateNode("Node -1-1", -1);
        Editor.CreateNode("Node -1-2", -1);
        Editor.CreateNode("Node -1-3", -1);

        var node1 = Editor.CreateNode("Node 1", 0, 0xff0000ff);
        if (node1 == null)
        {
            return;
        }

        node1.CreatePin("In", PinKind.Input, ImNodesPinShape.Circle);
        node1.CreatePin("Out", PinKind.Output, ImNodesPinShape.Circle);

        var node2 = Editor.CreateNode("Node 2", 1);
        if (node2 == null)
        {
            return;
        }

        node2.CreatePin("In", PinKind.Input, ImNodesPinShape.Circle);
        node2.CreatePin("Out", PinKind.Output, ImNodesPinShape.Circle);

        var node21 = Editor.CreateNode("Node 2-1", 1);
        if (node21 == null)
        {
            return;
        }

        node21.CreatePin("In", PinKind.Input, ImNodesPinShape.Quad);

        if (node1.TryGetPin("Out", out var out1) == false)
        {
            return;
        }

        if (node2.TryGetPin("In", out var in2) == false)
        {
            return;
        }

        var link = Editor.CreateLink(out1, in2);
        link?.Dots.Add(new() { DurationMSec = 10000, Color = 0xff00ff00, Destination = PinKind.Output });
        link?.Dots.Add(new() { DurationMSec = 10000, Color = 0xff00ffff, Destination = PinKind.Input });
        //link?.Dots.Add(new() { DurationMSec = 5000, Color = 0xff00ff00, Destination = PinKind.Output });
    }
}
