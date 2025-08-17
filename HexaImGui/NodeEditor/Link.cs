namespace ELImGui.NodeEidtor;

using Hexa.NET.ImNodes;

public class Link
{
    public Link(int id, Node outputNode, Pin output, Node inputNode, Pin input)
    {
        Id = id;
        OutputNode = outputNode;
        OutputPin = output;
        InputNode = inputNode;
        InputPin = input;
    }

    public NodeEditor? Editor { get; private set; }
    public int Id { get; init; }
    public Node OutputNode { get; init; }
    public Pin OutputPin { get; init; }
    public Node InputNode { get; init; }
    public Pin InputPin { get; init; }

    public void Render()
    {
        ImNodes.Link(Id, OutputPin.Id, InputPin.Id);
    }

    public void Destroy()
    {
        if (Editor == null)
        {
            return;
        }

        Editor.RemoveLink(this);
        OutputNode.RemoveLink(this);
        OutputPin.RemoveLink(this);
        InputNode.RemoveLink(this);
        InputPin.RemoveLink(this);
        Editor = null;
    }

    public void Initialize(NodeEditor editor)
    {
        Editor = editor;
        OutputNode.AddLink(this);
        OutputPin.AddLink(this);
        InputNode.AddLink(this);
        InputPin.AddLink(this);
    }
}