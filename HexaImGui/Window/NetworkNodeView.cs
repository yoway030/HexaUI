
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using ELImGui.NodeEidtor;

namespace ELImGui.Window;

public class NetworkNodeView
{
    private NodeEditor editor = new();

    public NetworkNodeView()
    {
        editor.Initialize();
        var node1 = editor.CreateNode("Node");
        node1.CreatePin(editor, "In", PinKind.Input, PinType.DontCare, ImNodesPinShape.Circle);
        var out1 = node1.CreatePin(editor, "Out", PinKind.Output, PinType.DontCare, ImNodesPinShape.Circle);
        var node2 = editor.CreateNode("Node");
        var in2 = node2.CreatePin(editor, "In", PinKind.Input, PinType.DontCare, ImNodesPinShape.Circle);
        var out2 = node2.CreatePin(editor, "Out", PinKind.Output, PinType.DontCare, ImNodesPinShape.Circle);
        var node3 = editor.CreateNode("Node");
        var in3 = node3.CreatePin(editor, "In", PinKind.Input, PinType.DontCare, ImNodesPinShape.Circle);
        node3.CreatePin(editor, "Out", PinKind.Output, PinType.DontCare, ImNodesPinShape.Circle);
        editor.CreateLink(in2, out1);
        editor.CreateLink(in3, out1);
        editor.CreateLink(in3, out2);
    }

    public void Draw()
    {
        if (!ImGui.Begin("Demo ImNodes", ImGuiWindowFlags.MenuBar))
        {
            ImGui.End();
            return;
        }

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.MenuItem("New Node"))
            {
                var node = editor.CreateNode("Node");
                node.CreatePin(editor, "In", PinKind.Input, PinType.DontCare, ImNodesPinShape.Circle);
                node.CreatePin(editor, "Out", PinKind.Output, PinType.DontCare, ImNodesPinShape.Circle);
            }

            ImGui.EndMenuBar();
        }

        editor.Draw();

        ImGui.End();
    }
}
