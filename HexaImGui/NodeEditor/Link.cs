namespace ELImGui.NodeEditor;

using Hexa.NET.ImNodes;

public class Link
{
    public Link(int id, NodeEditor editor, Pin output, Pin input)
    {
        Id = id;
        Editor = editor;
        OutputPin = output;
        InputPin = input;
    }

    public int Id { get; init; }
    public NodeEditor Editor { get; init; }
    public Pin OutputPin { get; init; }
    public Pin InputPin { get; init; }

    public List<LinkFlowPoint> OutToInFlowPoint { get; init; } = new();
    public List<LinkFlowPoint> InToOutFlowPoint { get; init; } = new();

    public void Render()
    {
        ImNodes.Link(Id, OutputPin.Id, InputPin.Id);
    }

    public void Destroy()
    {
        Editor.RemoveLink(this);
        OutputPin.RemoveLink(this);
        InputPin.RemoveLink(this);
    }
}