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

    private NodeEditor? _editor;
    private Node _parent;
    private int _id;
    private readonly List<Link> _links = new();

#pragma warning disable CS8618 // Non-nullable field 'parent' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.

    public Pin(int id, string name, ImNodesPinShape shape, PinKind kind)
#pragma warning restore CS8618 // Non-nullable field 'parent' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
    {
        this._id = id;
        Name = name;
        Shape = shape;
        Kind = kind;
    }

    public readonly string Name;
    public ImNodesPinShape Shape;
    public PinKind Kind;
    public uint MaxLinks;

    public Vector2? Center { get; set; } = null;

    public event EventHandler<Link>? LinkCreated;

    public event EventHandler<Link>? LinkRemoved;

    public int Id { get => _id; }

    public Node Parent { get => _parent; }

    public List<Link> Links => _links;

    public void AddLink(Link link)
    {
        _links.Add(link);
        LinkCreated?.Invoke(this, link);
    }

    public void RemoveLink(Link link)
    {
        _links.Remove(link);
        LinkRemoved?.Invoke(this, link);
    }

    public virtual bool CanCreateLink(Pin other)
    {
        if (_id == other._id)
        {
            return false;
        }

        if (Kind == other.Kind)
        {
            return false;
        }

        return true;
    }

    public void Draw()
    {
        if (Kind == PinKind.Input)
        {
            ImNodes.BeginInputAttribute(_id, Shape);
            DrawContent();
            ImNodes.EndInputAttribute();
        }

        if (Kind == PinKind.Output)
        {
            ImNodes.BeginOutputAttribute(_id, Shape);
            DrawContent();
            ImNodes.EndOutputAttribute();
        }

        if (Kind == PinKind.Static)
        {
            ImNodes.BeginStaticAttribute(_id);
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
        _editor = editor;
        _parent = parent;

        if (_id == 0)
        {
            _id = editor.GetUniqueId();
        }
    }

    public virtual void Destroy()
    {
        if (_editor == null)
        {
            return;
        }

        var links = Links.ToArray();
        for (int i = 0; i < links.Length; i++)
        {
            links[i].Destroy();
        }

        _editor = null;
    }
}