namespace ELImGui.Window;

using Hexa.NET.ImNodes;
using ELImGui.NodeEditor;
using System;

public class NodeViewer : BaseWindow
{
    public NodeViewer(string windowName = nameof(NodeViewer))
        : base(windowName, 0, null)
    {
        InitSample();
    }

    public NodeEditor Editor { get; } = new();

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        Editor.Render(utcNow, deltaSec);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
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

        node1.CreatePin(Editor, "In", PinKind.Input, ImNodesPinShape.Circle);
        node1.CreatePin(Editor, "Out", PinKind.Output, ImNodesPinShape.Circle);

        var node2 = Editor.CreateNode("Node 2", 1);
        if (node2 == null)
        {
            return;
        }

        node2.CreatePin(Editor, "In", PinKind.Input, ImNodesPinShape.Circle);
        node2.CreatePin(Editor, "Out", PinKind.Output, ImNodesPinShape.Circle);

        var node21 = Editor.CreateNode("Node 2-1", 1);
        if (node21 == null)
        {
            return;
        }

        node21.CreatePin(Editor, "In", PinKind.Input, ImNodesPinShape.Quad);

        if (node1.TryGetPin("Out", out var out1) == false)
        {
            return;
        }

        if (node2.TryGetPin("In", out var in2) == false)
        {
            return;
        }

        var link = Editor.CreateLink(out1, in2);
        link?.Dots.Add(new() { DurationMSec = 2000, Color = 0xff00ff00, Destination = PinKind.Output });
        //link?.Dots.Add(new() { DurationMSec = 5000, Color = 0xff00ff00, Destination = PinKind.Output });
    }
}
