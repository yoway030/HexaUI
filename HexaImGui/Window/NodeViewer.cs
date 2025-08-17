namespace ELImGui.Window;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using ELImGui.NodeEidtor;
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
        Editor.Render();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public void InitSample()
    {
        var node1 = Editor.CreateNode("Node 1", 1);
        var in1 = node1.CreatePin(Editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        var out1 = node1.CreatePin(Editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);

        var node2 = Editor.CreateNode("Node 2", 2);
        var in2 = node2.CreatePin(Editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        var out2 = node2.CreatePin(Editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);

        var node21 = Editor.CreateNode("Node 2-1", 2);
        node21.CreatePin(Editor, "In", Pin.PinKind.Input, ImNodesPinShape.Quad);

        var node3 = Editor.CreateNode("Node 3", 3);
        var in3 = node3.CreatePin(Editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        node3.CreatePin(Editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);

        var node31 = Editor.CreateNode("Node 3-1", 3);

        Editor.CreateLink(in2, out1);
        Editor.CreateLink(in3, out1);
        Editor.CreateLink(in3, out2);
    }
}
