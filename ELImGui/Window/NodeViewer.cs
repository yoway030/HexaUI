namespace ELImGui.Window;

using Hexa.NET.ImNodes;
using ELImGui.NodeEditor;
using System;

public class NodeViewer : BaseWindow
{
    public NodeViewer(string windowName = nameof(NodeViewer))
        : base(windowName, null)
    {
    }

    public NodeEditor Editor { get; } = new();

    public readonly List<BaseWindow> ChildWindows = new();

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        Editor.Render(utcNow, deltaSec);
        ChildWindows.ForEach(w => w.RenderImObject(utcNow, deltaSec, imInternalContext));
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        Editor.Update(utcNow, deltaSec);
        ChildWindows.ForEach(w => w.UpdateImObject(utcNow, deltaSec, imInternalContext));
    }

    public void InitSample()
    {
        var node1 = Editor.CreateNode("Node 1", -1, 0xff0000ff);
        if (node1 == null)
        {
            return;
        }

        node1.CreatePin("In", PinKind.Input, ImNodesPinShape.Circle);
        var node1Out = node1.CreatePin("Out", PinKind.Output, ImNodesPinShape.Circle);

        var node2 = Editor.CreateNode("Node 2", 0);
        if (node2 == null)
        {
            return;
        }

        var node2In = node2.CreatePin("In", PinKind.Input, ImNodesPinShape.Circle);
        var node2Out = node2.CreatePin("Out", PinKind.Output, ImNodesPinShape.Circle);

        var node3 = Editor.CreateNode("Node 3", 1);
        if (node3 == null)
        {
            return;
        }

        var node3In = node3.CreatePin("In", PinKind.Input, ImNodesPinShape.QuadFilled);
        node3.CreatePin("Out", PinKind.Output, ImNodesPinShape.TriangleFilled);

        var link1to2 = Editor.CreateLink(node1Out!, node2In!);
        link1to2?.Dots.Add(new() { DurationMSec = 10000, Color = 0xff00ff00, Destination = PinKind.Output });
        link1to2?.Dots.Add(new() { DurationMSec = 20000, Color = 0xff00ffff, Destination = PinKind.Input });

        var link2to3 = Editor.CreateLink(node2Out!, node3In!);
        link2to3?.Dots.Add(new() { DurationMSec = 15000, Color = 0xf0f0f000, Destination = PinKind.Output });
        link2to3?.Dots.Add(new() { DurationMSec = 25000, Color = 0xffff00ff, Destination = PinKind.Input });
    }
}
