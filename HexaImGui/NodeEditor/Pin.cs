using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

namespace ELImGui.NodeEditor;

public enum PinKind
{
    Input,
    Output,
}

public class Pin
{
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

    public void Render()
    {
        if (Kind == PinKind.Input)
        {
            ImNodes.BeginInputAttribute(Id, Shape);
            RenderContent();
            ImNodes.EndInputAttribute();
        }
        else if (Kind == PinKind.Output)
        {
            ImNodes.BeginOutputAttribute(Id, Shape);
            RenderContent();
            ImNodes.EndOutputAttribute();
        }
    }

    private void RenderContent()
    {
        ImGui.Text(Name);

        UpdateCenterPos();
    }

    private void UpdateCenterPos()
    {
        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();

        float y = (min.Y + max.Y) * 0.5f;
        Center = new Vector2(min.X, y); // x position incorrect
    }

    public void Destroy()
    {
        var links = Links.ToArray();
        foreach (var link in links)
        {
            link.Destroy();
        }

        Links.Clear();
    }
}