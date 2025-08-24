namespace ELImGui.NodeEditor;

using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Diagnostics.CodeAnalysis;
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

    public Node? CreateNode(string name, int layer = 0, uint titleColor = Node.Title_Color)
    {
        Node node = new(GetUniqueId(), name, layer, this, titleColor);
        if (TryAddNode(node) == false)
        {
            return null;
        }

        return node;
    }

    public bool TryAddNode(Node node)
    {
        if (TryGetNode(node.Id, out _) == true ||
            TryGetNode(node.Name, out _) == true)
        {
            return false;
        }

        _nodesById.Add(node.Id, node);
        _nodesByName.Add(node.Name, node);

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

    public bool TryRemoveNode(string nodeName)
    {
        if (TryGetNode(nodeName, out var node) == false)
        {
            return false;
        }

        return TryRemoveNode(nodeName);
    }

    public bool TryRemoveNode(Node node)
    {
        if (_nodesById.Remove(node.Id) == false)
        {
            return false;
        }

        if (_nodesByName.Remove(node.Name) == false)
        {
            return false;
        }

        if (_nodesByLayer[node.Layer] == null)
        {
            return false;
        }

        return _nodesByLayer[node.Layer].Remove(node);
    }

    public bool TryGetNode(int id, [NotNullWhen(true)] out Node? node)
        => _nodesById.TryGetValue(id, out node);

    public bool TryGetNode(string name, [NotNullWhen(true)] out Node? node)
        => _nodesByName.TryGetValue(name, out node);

    public Link? CreateLink(Pin from, Pin to)
    {
        Link link = new(GetUniqueId(), this, from, to);
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



    public void Render(DateTime utcNow, double deltaSec)
    {
        ImNodes.EditorContextSet(_editorContext);
        ImNodes.BeginNodeEditor();

        if (AdjustCenter != Vector2.Zero)
        {
            ImNodes.EditorContextResetPanning(AdjustCenter);
            AdjustCenter = Vector2.Zero;
        }

        foreach (var link in Links)
        {
            link.Render();
        }

        RenderLinkFlows(utcNow, deltaSec);

        foreach (var node in _nodesById.Values)
        {
            node.Render();
        }

        RendLinkHover();

        ImNodes.MiniMap();
        ImNodes.EndNodeEditor();

        ImNodes.EditorContextSet(null);
    }

    private void RenderLinkFlows(DateTime utcNow, double deltaSec)
    {
        const float HandleScale = 0.25f; // 제어점 스케일(α)

        var drawList = ImGui.GetWindowDrawList();

        foreach (var link in Links)
        {
            if (link.OutputPin.Center == null)
            {
                continue;
            } 
            else if (link.InputPin.Center == null)
            {
                continue;
            }
            else if (link.Dots.Any() == false)
            {
                continue;
            }

            var p3 = link.InputPin.Center.Value;
            var p0 = link.OutputPin.Center.Value;
            float dx = Vector2.Distance(p0, p3);
            var p1 = p0 + new Vector2(dx * HandleScale, 0f);
            var p2 = p3 - new Vector2(dx * HandleScale, 0f);

            foreach (var dot in link.Dots.ToList())
            {
                TimeSpan timeSpan = utcNow - dot.CreatedTime;
                float timeProgress = (float)timeSpan.TotalMilliseconds / (float)dot.DurationMSec;
                if (timeProgress > 1.0f)
                {
                    link.Dots.Remove(dot);
                }

                float positionRate = dot.Destination == PinKind.Input ? timeProgress : 1.0f - timeProgress;
                Vector2 flowPos = CubicBezier(p0, p1, p2, p3, positionRate);
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

    private void RendLinkHover()
    {
        int id = 0;
        if (ImNodes.IsLinkHovered(ref id))
        {
            Console.WriteLine($"Link hovered: {id}");
            ImGui.BeginTooltip();
            ImGui.EndTooltip();
        }
    }

    public void Destroy()
    {
        foreach (var node in _nodesById.Values)
        {
            node.Destroy();
        }

        _nodesById.Clear();
        _nodesByName.Clear();
        _nodesByLayer.Clear();

        ImNodes.EditorContextFree(_editorContext);
        _editorContext = null;
    }
}