using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

namespace ELImGui.NodeEditor;

public class Pin
{
    public enum PinKind
    {
        Input,
        Output,
        Static
    }

    public Pin(int id, string name, Node parent, ImNodesPinShape shape, PinKind kind)
    {
        Id = id;
        Name = name;
        Shape = shape;
        Kind = kind;
        Parent = parent;
    }

    public int Id { get; init; }
    public string Name { get; set; }
    public Node Parent { get; init; }
    public ImNodesPinShape Shape { get; set; }
    public PinKind Kind { get; init; }
    public Vector2? Center { get; set; } = null;
    public List<Link> Links { get; init; } = new();

    public void AddLink(Link link)
    {
        Links.Add(link);
    }

    public void RemoveLink(Link link)
    {
        Links.Remove(link);
    }

    public bool CanCreateLink(Pin other)
    {
        if (Id == other.Id)
        {
            return false;
        }

        if (Kind == other.Kind)
        {
            return false;
        }

        return true;
    }

    public void Render()
    {
        if (Kind == PinKind.Input)
        {
            ImNodes.BeginInputAttribute(Id, Shape);
            RenderContent();
            ImNodes.EndInputAttribute();
        }

        if (Kind == PinKind.Output)
        {
            ImNodes.BeginOutputAttribute(Id, Shape);
            RenderContent();
            ImNodes.EndOutputAttribute();
        }

        if (Kind == PinKind.Static)
        {
            ImNodes.BeginStaticAttribute(Id);
            RenderContent();
            ImNodes.EndStaticAttribute();
        }
    }

    private void RenderContent()
    {
        ImGui.Text(Name);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        
        float y = (min.Y + max.Y) * 0.5f;
        Center = new Vector2(min.X, y); // x position incorrect
    }

    public void Destroy()
    {
        var links = Links.ToArray();
        for (int i = 0; i < links.Length; i++)
        {
            links[i].Destroy();
        }
    }
}