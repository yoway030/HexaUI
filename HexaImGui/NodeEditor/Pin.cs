using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

namespace ELImGui.NodeEidtor;

public class Pin
{
    public enum PinKind
    {
        Input,
        Output,
        Static
    }

    private NodeEditor? editor;
    private Node parent;
    private int id;

    public readonly string Name;
    public ImNodesPinShape Shape;
    public PinKind Kind;
    public uint MaxLinks;
    public Vector2? Center { get; set; } = null;

    private readonly List<Link> links = new();

#pragma warning disable CS8618 // Non-nullable field 'parent' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.

    public Pin(int id, string name, ImNodesPinShape shape, PinKind kind, uint maxLinks = uint.MaxValue)
#pragma warning restore CS8618 // Non-nullable field 'parent' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
    {
        this.id = id;
        Name = name;
        Shape = shape;
        Kind = kind;
        MaxLinks = maxLinks;
    }

    public event EventHandler<Link>? LinkCreated;

    public event EventHandler<Link>? LinkRemoved;

    public int Id => id;

    public Node Parent => parent;

    public List<Link> Links => links;

    public void AddLink(Link link)
    {
        links.Add(link);
        LinkCreated?.Invoke(this, link);
    }

    public void RemoveLink(Link link)
    {
        links.Remove(link);
        LinkRemoved?.Invoke(this, link);
    }

    public virtual bool CanCreateLink(Pin other)
    {
        if (id == other.id) return false;
        if (Links.Count == MaxLinks) return false;
        if (Kind == other.Kind) return false;

        return true;
    }

    public void Draw()
    {
        if (Kind == PinKind.Input)
        {
            ImNodes.BeginInputAttribute(id, Shape);
            DrawContent();
            ImNodes.EndInputAttribute();
        }
        if (Kind == PinKind.Output)
        {
            ImNodes.BeginOutputAttribute(id, Shape);
            DrawContent();
            ImNodes.EndOutputAttribute();
        }
        if (Kind == PinKind.Static)
        {
            ImNodes.BeginStaticAttribute(id);
            DrawContent();
            ImNodes.EndStaticAttribute();
        }
    }

    protected virtual void DrawContent()
    {
        ImGui.Text(Name);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        float y = (min.Y + max.Y) * 0.5f;
        Center = new Vector2(min.X, y); // x position incorrect
    }

    public virtual void Initialize(NodeEditor editor, Node parent)
    {
        this.editor = editor;
        this.parent = parent;
        if (id == 0)
            id = editor.GetUniqueId();
    }

    public virtual void Destroy()
    {
        if (editor == null) return;
        var links = Links.ToArray();
        for (int i = 0; i < links.Length; i++)
        {
            links[i].Destroy();
        }

        editor = null;
    }
}