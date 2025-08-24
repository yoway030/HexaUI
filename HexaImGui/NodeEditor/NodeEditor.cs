namespace ELImGui.NodeEditor;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

public class NodeEditor
{
    private ImNodesEditorContextPtr _editorContext;
    private int _idOffset;

    private Dictionary<int, Node> _nodesById = new();
    private Dictionary<string, Node> _nodesByName = new();
    private Dictionary<int, HashSet<Node>> _nodesByLayer { get; } = new();

    public const float NodeWidth = 100f;
    public const float NodeHeight= 100f;

    public NodeEditor()
    {
        _editorContext = ImNodes.EditorContextCreate();
    }

    public List<Link> Links { get; } = new();

    public Vector2 AdjustCenter = new Vector2(NodeWidth, NodeHeight);

    public int GetUniqueId()
    {
        return _idOffset++;
    }

    public Node? GetNode(int id) => _nodesById.TryGetValue(id, out Node? node) == true ? node : null;

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

    public Node? CreateNode(string name, int layer = 0, uint titleColor = Node.Title_Color)
    {
        Node node = new(GetUniqueId(), name, layer, this, titleColor);
        if (TryAddNode(node) == false)
        {
            return null;
        }

        return node;
    }

    private bool TryAddNode(Node node)
    {
        if (_nodesById.TryAdd(node.Id, node) == false)
        {
            return false; // 이미 존재하는 ID
        }

        if (_nodesByName.TryAdd(node.Name, node) == false)
        {
            return false; // 이미 존재하는 이름
        }

        if (_nodesByLayer.ContainsKey(node.Layer) == false)
        {
            _nodesByLayer[node.Layer] = new HashSet<Node>();
        }

        int prevCount = _nodesByLayer[node.Layer].Count;
        _nodesByLayer[node.Layer].Add(node);

        node.AdjustPosition.X = NodeWidth * node.Layer;
        node.AdjustPosition.Y = NodeHeight * ((prevCount + 1) / 2) * (prevCount % 2 == 0 ? 1 : -1); // 위아래위래 배치

        return true;
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

    public void Render(DateTime utcNow, double deltaSec)
    {
        ImNodes.EditorContextSet(_editorContext);
        ImNodes.BeginNodeEditor();

        if (AdjustCenter != Vector2.Zero)
        {
            ImNodes.EditorContextResetPanning(AdjustCenter);
            AdjustCenter = Vector2.Zero;
        }

        foreach (var node in _nodesById.Values)
        {
            node.Render();
        }

        RenderLinkFlows(utcNow, deltaSec);

        for (int i = 0; i < Links.Count; i++)
        {
            Links[i].Render();
        }

        ImNodes.MiniMap();
        ImNodes.EndNodeEditor();

        ImNodes.EditorContextSet(null);
    }

    private void RenderLinkFlows(DateTime utcNow, double deltaSec)
    {
        const float HandleScale = 0.25f; // 제어점 스케일(α)

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

            foreach (var dot in link.InToOutFlowPoint.ToList())
            {
                TimeSpan timeSpan = utcNow - dot.CreatedTime;
                float progress = (float)(timeSpan.Divide(dot.FlowDuration));
                if (progress > 1.0f)
                {
                    link.InToOutFlowPoint.Remove(dot);
                }

                Vector2 flowPos = CubicBezier(p0, p1, p2, p3, 1.0f - progress);
                drawList.AddCircleFilled(flowPos, dot.DotRadius, dot.Color);
            }

            foreach (var dot in link.OutToInFlowPoint.ToList())
            {
                TimeSpan timeSpan = utcNow - dot.CreatedTime;
                float progress = (float)(timeSpan.Divide(dot.FlowDuration));
                if (progress > 1.0f)
                {
                    link.OutToInFlowPoint.Remove(dot);
                }

                Vector2 flowPos = CubicBezier(p0, p1, p2, p3, progress);
                drawList.AddCircleFilled(flowPos, dot.DotRadius, dot.Color);
            }
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
        foreach (var node in _nodesById.Values)
        {
            node.Destroy();
        }

        _nodesById.Clear();

        ImNodes.EditorContextFree(_editorContext);
        _editorContext = null;
    }
}