namespace ELImGui.NodeEidtor;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

public class Node
{
    public const uint TitleColor = 0x6930c3ff;
    public const uint TitleHoveredColor = 0x5e60ceff;
    public const uint TitleSelectedColor = 0x7400b8ff;

    private NodeEditor? _editor;
    private int _id;
    private Vector2 position = Vector2.Zero;

    private bool _isEditing;
    private readonly List<Pin> _pins = new();
    private readonly List<Link> _links = new();

    public Node(int id, string name, bool removable, bool isStatic)
    {
        this._id = id;
        Name = name;
        Removable = removable;
        IsStatic = isStatic;
    }

    public readonly bool Removable = true;
    public readonly bool IsStatic;
    public string Name;

    public event EventHandler<Pin>? PinAdded;

    public event EventHandler<Pin>? PinRemoved;

    public event EventHandler<Link>? LinkAdded;

    public event EventHandler<Link>? LinkRemoved;

    public int Id => _id;

    public List<Link> Links => _links;

    public List<Pin> Pins => _pins;

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
        }
    }

    public bool IsHovered { get; set; }

    public virtual void Initialize(NodeEditor editor)
    {
        _editor = editor;
        if (_id == 0)
        {
            _id = editor.GetUniqueId();
        }

        for (int i = 0; i < _pins.Count; i++)
        {
            _pins[i].Initialize(editor, this);
        }
    }

    public Pin GetInput(int id)
    {
        Pin? pin = Find(id);
        if (pin == null || pin.Kind != Pin.PinKind.Input)
        {
            throw new();
        }
        return pin;
    }

    public Pin GetOuput(int id)
    {
        Pin? pin = Find(id);
        if (pin == null || pin.Kind != Pin.PinKind.Output)
        {
            throw new();
        }
        return pin;
    }

    public Pin? Find(int id)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            var pin = _pins[i];
            if (pin.Id == id)
            {
                return pin;
            }
        }
        return null;
    }

    public Pin? Find(string name)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            var pin = _pins[i];
            if (pin.Name == name)
            {
                return pin;
            }
        }
        return null;
    }

    public bool PinExists(string name)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            var pin = _pins[i];
            if (pin.Name == name)
            {
                return true;
            }
        }
        return false;
    }

    public static Link? FindSourceLink(Pin pin, Node other)
    {
        for (int i = 0; i < pin.Links.Count; i++)
        {
            Link link = pin.Links[i];
            if (link.OutputNode == other)
            {
                return link;
            }
        }
        return null;
    }

    public static Link? FindTargetLink(Pin pin, Node other)
    {
        for (int i = 0; i < pin.Links.Count; i++)
        {
            Link link = pin.Links[i];
            if (link.InputNode == other)
            {
                return link;
            }
        }
        return null;
    }

    public virtual Pin CreatePin(NodeEditor editor, string name, Pin.PinKind kind, ImNodesPinShape shape)
    {
        Pin pin = new(editor.GetUniqueId(), name, shape, kind);
        return AddPin(pin);
    }

    public virtual Pin CreateOrGetPin(NodeEditor editor, string name, Pin.PinKind kind, ImNodesPinShape shape)
    {
        Pin pin = new(editor.GetUniqueId(), name, shape, kind);
        return AddOrGetPin(pin);
    }

    public virtual T AddPin<T>(T pin) where T : Pin
    {
        Pin? old = Find(pin.Name);

        if (old != null)
        {
            int index = _pins.IndexOf(old);
            old.Destroy();
            if (_editor != null)
            {
                pin.Initialize(_editor, this);
            }

            _pins[index] = pin;
        }
        else
        {
            if (_editor != null)
            {
                pin.Initialize(_editor, this);
            }

            _pins.Add(pin);
            PinAdded?.Invoke(this, pin);
        }

        return pin;
    }

    public virtual T AddOrGetPin<T>(T pin) where T : Pin
    {
        Pin? old = Find(pin.Name);

        if (old != null)
        {
            return (T)old;
        }
        else
        {
            if (_editor != null)
            {
                pin.Initialize(_editor, this);
            }

            _pins.Add(pin);
            PinAdded?.Invoke(this, pin);
        }

        return pin;
    }

    public virtual void DestroyPin<T>(T pin) where T : Pin
    {
        pin.Destroy();
        _pins.Remove(pin);
        PinRemoved?.Invoke(this, pin);
    }

    public virtual void AddLink(Link link)
    {
        _links.Add(link);
        LinkAdded?.Invoke(this, link);
    }

    public virtual void RemoveLink(Link link)
    {
        _links.Remove(link);
        LinkRemoved?.Invoke(this, link);
    }

    public virtual void Destroy()
    {
        if (_editor == null)
        {
            return;
        }

        for (int i = 0; i < _pins.Count; i++)
        {
            _pins[i].Destroy();
        }
        _editor.RemoveNode(this);
        _editor = null;
    }

    public virtual void Draw()
    {
        ImNodes.PushColorStyle(ImNodesCol.TitleBar, TitleColor);
        ImNodes.PushColorStyle(ImNodesCol.TitleBarHovered, TitleHoveredColor);
        ImNodes.PushColorStyle(ImNodesCol.TitleBarSelected, TitleSelectedColor);

        ImNodes.BeginNode(_id);
        ImNodes.BeginNodeTitleBar();

        if (_isEditing)
        {
            string name = Name;
            ImGui.PushItemWidth(100);
            if (ImGui.InputText("Name", ref name, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                Name = name;
                _isEditing = false;
            }
            ImGui.PopItemWidth();
        }
        else
        {
            ImGui.Text(Name);
            ImGui.SameLine();
            if (ImGui.SmallButton("Edit")) // TODO: Replace with icon
            {
                _isEditing = true;
            }
            ImGui.Text(position.ToString());
        }

        ImNodes.EndNodeTitleBar();

        DrawContentBeforePins();

        for (int i = 0; i < _pins.Count; i++)
        {
            _pins[i].Draw();
        }

        DrawContent();

        var nodePos = ImNodes.GetNodeScreenSpacePos(Id);
        var nodeSize = ImNodes.GetNodeDimensions(Id);

        for (int i = 0; i < _pins.Count; i++)
        {
            if (_pins[i].Kind == Pin.PinKind.Input)
            {
                var center = _pins[i].Center;
                _pins[i].Center = new Vector2(nodePos.X, center?.Y ?? 0f);
            }

            if (_pins[i].Kind == Pin.PinKind.Output)
            {
                var center = _pins[i].Center;
                _pins[i].Center = new Vector2(nodePos.X + nodeSize.X, center?.Y ?? 0f);
            }
        }

        ImNodes.EndNode();
        ImNodes.PopColorStyle();
    }

    protected virtual void DrawContentBeforePins()
    {
    }

    protected virtual void DrawContent()
    {
    }

    public override string ToString()
    {
        return Name;
    }
}