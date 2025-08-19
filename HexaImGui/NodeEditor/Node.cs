namespace ELImGui.NodeEidtor;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

public class Node
{
    public const uint Title_Color = 0x6930c3ff;
    public const uint Title_HoveredColor = 0x5e60ceff;
    public const uint Title_SelectedColor = 0x7400b8ff;

    public Node(int id, string name, NodeEditor editor, uint titleColor = Title_Color)
    {
        Id = id;
        Name = name;
        Editor = editor;

        TitleColor = titleColor;
    }

    public string Name { get; init; }
    public int Id { get; init; }
    
    private NodeEditor Editor { get; init; }

    public uint TitleColor { get; set; }

    public List<Pin> Pins { get; } = new();
    public bool IsHovered { get; set; } = false;
    public Vector2 AdjustPosition = Vector2.Zero;

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
        for (int i = 0; i < Pins.Count; i++)
        {
            var pin = Pins[i];
            if (pin.Id == id)
            {
                return pin;
            }
        }
        return null;
    }

    public Pin? Find(string name)
    {
        for (int i = 0; i < Pins.Count; i++)
        {
            var pin = Pins[i];
            if (pin.Name == name)
            {
                return pin;
            }
        }
        return null;
    }

    public bool PinExists(string name)
    {
        for (int i = 0; i < Pins.Count; i++)
        {
            var pin = Pins[i];
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
            if (link.OutputPin.Parent == other)
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
            if (link.InputPin.Parent == other)
            {
                return link;
            }
        }
        return null;
    }

    public Pin CreatePin(NodeEditor editor, string name, Pin.PinKind kind, ImNodesPinShape shape)
    {
        Pin pin = new(editor.GetUniqueId(), name, this, shape, kind);
        return AddPin(pin);
    }

    public Pin AddPin(Pin pin)
    {
        Pin? old = Find(pin.Name);

        if (old != null)
        {
            int index = Pins.IndexOf(old);
            old.Destroy();

            Pins[index] = pin;
        }
        else
        {
            Pins.Add(pin);
        }

        return pin;
    }

    public void DestroyPin(Pin pin)
    {
        pin.Destroy();
        Pins.Remove(pin);
    }

    public void Destroy()
    {
        for (int i = 0; i < Pins.Count; i++)
        {
            Pins[i].Destroy();
        }
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

        if (Pins.Any() == false)
        {
            // 여기에 뭐라도 그리지 않으면 노드가 비정상적으로 출력됨
            ImGui.Text(" ");
        }
        else
        {
            foreach (var pin in Pins)
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

        for (int i = 0; i < Pins.Count; i++)
        {
            if (Pins[i].Kind == Pin.PinKind.Input)
            {
                var center = Pins[i].Center;
                Pins[i].Center = new Vector2(nodePos.X, center?.Y ?? 0f);
            }

            if (Pins[i].Kind == Pin.PinKind.Output)
            {
                var center = Pins[i].Center;
                Pins[i].Center = new Vector2(nodePos.X + nodeSize.X, center?.Y ?? 0f);
            }
        }

        ImNodes.EndNode();
        ImNodes.PopColorStyle();
    }
}