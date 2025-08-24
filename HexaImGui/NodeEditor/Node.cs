namespace ELImGui.NodeEditor;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

public class Node
{
    public const uint Title_Color = 0x6930c3ff;
    public const uint Title_HoveredColor = 0x5e60ceff;
    public const uint Title_SelectedColor = 0x7400b8ff;

    private Dictionary<int, Pin> _pinsById = new();
    private Dictionary<string, Pin> _pinsByName = new();

    public Node(int id, string name, int layer, NodeEditor editor, uint titleColor = Title_Color)
    {
        Id = id;
        Name = name;
        Layer = layer;
        Editor = editor;
        TitleColor = titleColor;
    }

    public int Id { get; init; }
    public string Name { get; init; }
    public int Layer { get; private set; }
    
    private NodeEditor Editor { get; init; }

    public uint TitleColor { get; set; }

    public bool IsHovered { get; set; } = false;
    public Vector2 AdjustPosition = Vector2.Zero;

    public Pin? CreatePin(NodeEditor editor, string name, PinKind kind, ImNodesPinShape shape)
    {
        Pin pin = new(editor.GetUniqueId(), name, this, shape, kind);
        if (TryAddPin(pin) == false)
        {
            return null;
        }

        return pin;
    }

    public bool TryAddPin(Pin pin)
    {
        if (TryGetPin(pin.Id, out _) == true ||
            TryGetPin(pin.Name, out _) == true)
        {
            return false;
        }

        _pinsById.Add(pin.Id, pin);
        _pinsByName.Add(pin.Name, pin);

        return true;
    }

    public bool TryGetPin(int id, [NotNullWhen(true)] out Pin? pin)
        => _pinsById.TryGetValue(id, out pin);

    public bool TryGetPin(string name, [NotNullWhen(true)] out Pin? pin)
        => _pinsByName.TryGetValue(name, out pin);

    public void RemovePin(Pin pin)
    {
        _pinsById.Remove(pin.Id);
        _pinsByName.Remove(pin.Name);

        pin.Destroy();
    }

    public IEnumerable<Link> GetLinks(Node other)
    {
        foreach (var pin in _pinsById.Values)
        {
            foreach (var link in pin.Links)
            {
                if (link.OutputPin.Parent == other || link.InputPin.Parent == other)
                {
                    yield return link;
                }
            }
        }

        yield break;
    }

    public void Render()
    {
        ImNodes.PushColorStyle(ImNodesCol.TitleBar, TitleColor);
        ImNodes.PushColorStyle(ImNodesCol.TitleBarHovered, Title_HoveredColor);
        ImNodes.PushColorStyle(ImNodesCol.TitleBarSelected, Title_SelectedColor);

        ImNodes.BeginNode(Id);
        ImNodes.BeginNodeTitleBar();
        ImGui.Text(Name);
        ImNodes.EndNodeTitleBar();

        var pins = _pinsById.Values.ToList();
        if (pins.Any() == false)
        {
            // 여기에 뭐라도 그리지 않으면 노드가 비정상적으로 출력됨
            ImGui.Text(" ");
        }
        else
        {
            foreach (var pin in pins)
            {
                pin.Render();
            }
        }

        var nodePos = ImNodes.GetNodeScreenSpacePos(Id);
        var nodeSize = ImNodes.GetNodeDimensions(Id);

        if (AdjustPosition != Vector2.Zero)
        {
            nodePos += AdjustPosition;
            ImNodes.SetNodeScreenSpacePos(Id, nodePos);
            AdjustPosition = Vector2.Zero;
        }

        foreach (var pin in pins)
        {
            var center = pin.Center;

            if (pin.Kind == PinKind.Input)
            {
                pin.Center = new Vector2(nodePos.X, center?.Y ?? 0f);
            }
            else if (pin.Kind == PinKind.Output)
            {
                pin.Center = new Vector2(nodePos.X + nodeSize.X, center?.Y ?? 0f);
            }
        }

        ImNodes.EndNode();
        ImNodes.PopColorStyle();
    }

    public void Destroy()
    {
        foreach (var pin in _pinsById.Values.ToList())
        {
            RemovePin(pin);
        }
    }
}