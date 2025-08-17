namespace ELImGui.NodeEidtor;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

public class NodeEditor
{
    private ImNodesEditorContextPtr _editorContext;
    private int _idOffset;

    public NodeEditor()
    {
        _editorContext = ImNodes.EditorContextCreate();
    }

    public List<Node> Nodes { get; } = new();
    public List<Link> Links { get; } = new();

    public int GetUniqueId()
    {
        return _idOffset++;
    }

    public Node? GetNode(int id)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            Node node = Nodes[i];
            if (node.Id == id)
                return node;
        }

        return null;
    }

    public Link? GetLink(int id)
    {
        for (int i = 0; i < Links.Count; i++)
        {
            var link = Links[i];
            if (link.Id == id)
            {
                return link;
            }
        }

        return null;
    }

    public Node CreateNode(string name)
    {
        Node node = new(GetUniqueId(), name, this);
        AddNode(node);
        return node;
    }

    public void AddNode(Node node)
    {
        Nodes.Add(node);
    }

    public void RemoveNode(Node node)
    {
        Nodes.Remove(node);
    }

    public Link CreateLink(Pin input, Pin output)
    {
        Link link = new(GetUniqueId(), this, output, input);
        AddLink(link);
        return link;
    }

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
        ImNodes.EditorContextSet(_editorContext);
        ImNodes.BeginNodeEditor();

        for (int i = 0; i < Nodes.Count; i++)
        {
            Nodes[i].Render();
        }

        RenderLinkFlows();

        for (int i = 0; i < Links.Count; i++)
        {
            Links[i].Render();
        }

        ImNodes.MiniMap();
        ImNodes.EndNodeEditor();

        for (int i = 0; i < Nodes.Count; i++)
        {
            var id = Nodes[i].Id;
            Nodes[i].IsHovered = ImNodes.IsNodeHovered(ref id);
        }

        ImNodes.EditorContextSet(null);
    }

    private void RenderLinkFlows()
    {
        const float Speed = 0.1f;   // t 증가 속도 (초당)
        const float HandleScale = 0.25f; // 제어점 스케일(α)
        const float DotRadius = 4.0f;

        var drawList = ImGui.GetWindowDrawList();
        double time = ImGui.GetTime();

        foreach (var link in Links)
        {
            if (link.OutputPin.Center == null)
            {
                continue;
            }

            if (link.InputPin.Center == null)
            {
                continue;
            }

            var p3 = link.InputPin.Center.Value;
            var p0 = link.OutputPin.Center.Value;

            float dx = Vector2.Distance(p0, p3);
            var p1 = p0 + new Vector2(dx * HandleScale, 0f);
            var p2 = p3 - new Vector2(dx * HandleScale, 0f);

            float t = (float)(time * Speed % 1.0f);

            Vector2 pos = CubicBezier(p0, p1, p2, p3, t);
            uint col = ImGui.GetColorU32(new Vector4(1, 1, 1, 1));
            drawList.AddCircleFilled(pos, DotRadius, col);
        }
    }

    public Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u, tt = t * t;
        float uuu = uu * u, ttt = tt * t;

        Vector2 p = uuu * p0;
        p += 3f * uu * t * p1;
        p += 3f * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    public void Destroy()
    {
        foreach (var node in Nodes)
        {
            node.Destroy();
        }

        Nodes.Clear();

        ImNodes.EditorContextFree(_editorContext);
        _editorContext = null;
    }
}