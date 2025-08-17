namespace ELImGui.Window;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using ELImGui.NodeEidtor;
using System;

public class NodeViewer : BaseWindow
{
    private NodeEditor editor = new();

    public NodeViewer(string windowName = nameof(NodeViewer))
        : base(windowName, 0, null)
    {
        InitSample();
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        editor.Render();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public void InitSample()
    {
        var node1 = editor.CreateNode("Node");
        node1.CreatePin(editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        var out1 = node1.CreatePin(editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);
        var node2 = editor.CreateNode("Node");
        var in2 = node2.CreatePin(editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        var out2 = node2.CreatePin(editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);
        var node3 = editor.CreateNode("Node");
        var in3 = node3.CreatePin(editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        node3.CreatePin(editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);
        editor.CreateLink(in2, out1);
        editor.CreateLink(in3, out1);
        editor.CreateLink(in3, out2);
    }

    public void AddNode(string nodeName)
    {
        var node = editor.CreateNode(nodeName);
        node.CreatePin(editor, "In", Pin.PinKind.Input, ImNodesPinShape.Circle);
        node.CreatePin(editor, "Out", Pin.PinKind.Output, ImNodesPinShape.Circle);
    }
}
