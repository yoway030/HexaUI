using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

namespace ELImGui.NodeEidtor;

public class NodeEditor
{
    private string? state;
    private ImNodesEditorContextPtr context;

    private readonly List<Node> nodes = new();
    private readonly List<Link> links = new();
    private int idState;

    public NodeEditor()
    {
    }

    public event EventHandler<Node>? NodeAdded;

    public event EventHandler<Node>? NodeRemoved;

    public event EventHandler<Link>? LinkAdded;

    public event EventHandler<Link>? LinkRemoved;

    public List<Node> Nodes => nodes;

    public List<Link> Links => links;

    public int IdState { get => idState; set => idState = value; }

    public string State { get => SaveState(); set => RestoreState(value); }

    public virtual void Initialize()
    {
        if (context.IsNull)
        {
            context = ImNodes.EditorContextCreate();

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Initialize(this);
            }
            for (int i = 0; i < links.Count; i++)
            {
                links[i].Initialize(this);
            }
        }
    }

    public int GetUniqueId()
    {
        return idState++;
    }

    public Node GetNode(int id)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i];
            if (node.Id == id)
                return node;
        }
        throw new();
    }

    public T GetNode<T>() where T : Node
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node is T t)
                return t;
        }
        throw new KeyNotFoundException();
    }

    public Link GetLink(int id)
    {
        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link.Id == id)
                return link;
        }

        throw new KeyNotFoundException();
    }

    public Node CreateNode(string name, bool removable = true, bool isStatic = false)
    {
        Node node = new(GetUniqueId(), name, removable, isStatic);
        AddNode(node);
        return node;
    }

    public void AddNode(Node node)
    {
        if (context.IsNull)
            node.Initialize(this);
        nodes.Add(node);
        NodeAdded?.Invoke(this, node);
    }

    public void RemoveNode(Node node)
    {
        nodes.Remove(node);
        NodeRemoved?.Invoke(this, node);
    }

    public void AddLink(Link link)
    {
        if (context.IsNull)
            link.Initialize(this);
        links.Add(link);
        LinkAdded?.Invoke(this, link);
    }

    public void RemoveLink(Link link)
    {
        links.Remove(link);
        LinkRemoved?.Invoke(this, link);
    }

    public Link CreateLink(Pin input, Pin output)
    {
        Link link = new(GetUniqueId(), output.Parent, output, input.Parent, input);
        AddLink(link);
        return link;
    }

    public unsafe string SaveState()
    {
        return ImNodes.SaveEditorStateToIniStringS(context, null);
    }

    public void RestoreState(string state)
    {
        if (context.IsNull)
        {
            this.state = state;
            return;
        }
        ImNodes.LoadEditorStateFromIniString(context, state, (uint)state.Length);
    }

    public void Draw()
    {
        if (context.IsNull)
            Initialize();
        ImNodes.EditorContextSet(context);
        ImNodes.BeginNodeEditor();

        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        // 좌상단에 그리드가 바로 붙는 스타일이라면 보통 아래처럼 절반을 패닝으로 준다
        ImNodes.EditorContextResetPanning(new System.Numerics.Vector2(winSize.X * 0.5f, winSize.Y * 0.5f));

        //// 현재 Node Editor가 속한 윈도우의 DrawList 가져오기
        //var drawList = ImGui.GetWindowDrawList();

        //// Node Editor 좌표 → 스크린 좌표 변환
        //Vector2 origin = ImGui.GetWindowPos();
        //Vector2 rectMin = origin;
        //Vector2 rectMax = origin + new Vector2(100, 100);

        //// 색상
        //uint color = ImGui.GetColorU32(new Vector4(1f, 0f, 0f, 1f));

        //// 사각형 그리기
        //drawList.AddRectFilled(rectMin, rectMax, color);

        for (int i = 0; i < Nodes.Count; i++)
        {
            Nodes[i].Draw();
        }

        float Speed = 0.1f;   // t 증가 속도 (초당)
        float HandleScale = 0.25f; // 제어점 스케일(α)
        float DotRadius = 4.0f;
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

            // 각 링크마다 위상 차이를 줘서 구슬이 겹치지 않게
            float t = (float)(time * Speed % 1.0f);

            Vector2 pos = CubicBezier(p0, p1, p2, p3, t);
            uint col = ImGui.GetColorU32(new Vector4(1, 1, 1, 1)); // 필요시 색상/알파 조절
            drawList.AddCircleFilled(pos, DotRadius, col);
        }

        for (int i = 0; i < Links.Count; i++)
        {
            Links[i].Render();
        }

        ImNodes.MiniMap();
        ImNodes.EndNodeEditor();



        int idNode1 = 0;
        int idNode2 = 0;
        int idpin1 = 0;
        int idpin2 = 0;
        bool createdFromSnap = false;
        
        if (ImNodes.IsLinkCreated(ref idNode1, ref idpin1, ref idNode2, ref idpin2, ref createdFromSnap))
        {
            var pino = GetNode(idNode1).GetOuput(idpin1);
            var pini = GetNode(idNode2).GetInput(idpin2);
            if (pini.CanCreateLink(pino) && pino.CanCreateLink(pini))
                CreateLink(pini, pino);
        }

        int idLink = 0;
        if (ImNodes.IsLinkDestroyed(ref idLink))
        {
            GetLink(idLink).Destroy();
        }
        if (ImGui.IsKeyPressed(ImGuiKey.Delete))
        {
            int numLinks = ImNodes.NumSelectedLinks();
            if (numLinks != 0)
            {
                int[] links = new int[numLinks];
                ImNodes.GetSelectedLinks(ref links[0]);
                for (int i = 0; i < links.Length; i++)
                {
                    GetLink(links[i]).Destroy();
                }
            }
            int numNodes = ImNodes.NumSelectedNodes();
            if (numNodes != 0)
            {
                int[] nodes = new int[numNodes];
                ImNodes.GetSelectedNodes(ref nodes[0]);
                for (int i = 0; i < nodes.Length; i++)
                {
                    var node = GetNode(nodes[i]);
                    if (node.Removable)
                    {
                        node.Destroy();
                    }
                }
            }
        }
        int idpinStart = 0;
        if (ImNodes.IsLinkStarted(ref idpinStart))
        {
        }

        for (int i = 0; i < Nodes.Count; i++)
        {
            var id = Nodes[i].Id;
            Nodes[i].IsHovered = ImNodes.IsNodeHovered(ref id);
        }

        ImNodes.EditorContextSet(null);

        if (state != null)
        {
            RestoreState(state);
            state = null;
        }
    }

    public void Destroy()
    {
        var nodes = this.nodes.ToArray();
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].Destroy();
        }
        this.nodes.Clear();
        ImNodes.EditorContextFree(context);
        context = null;
    }

    public static bool Validate(Pin startPin, Pin endPin)
    {
        Node node = startPin.Parent;
        Stack<(int, Node)> walkstack = new();
        walkstack.Push((0, node));
        while (walkstack.Count > 0)
        {
            (int i, node) = walkstack.Pop();
            if (i > node.Links.Count)
                continue;
            Link link = node.Links[i];
            i++;
            walkstack.Push((i, node));
            if (link.OutputNode == node)
            {
                if (link.OutputPin == endPin)
                    return true;
                else
                    walkstack.Push((0, link.InputNode));
            }
        }

        return false;
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

    public static Node[] TreeTraversal(Node root, bool includeStatic)
    {
        Stack<Node> stack1 = new();
        Stack<Node> stack2 = new();

        Node node = root;
        stack1.Push(node);
        while (stack1.Count != 0)
        {
            node = stack1.Pop();
            if (stack2.Contains(node))
            {
                RemoveFromStack(stack2, node);
            }
            stack2.Push(node);

            for (int i = 0; i < node.Links.Count; i++)
            {
                if (node.Links[i].InputNode == node)
                {
                    var src = node.Links[i].OutputNode;
                    if (includeStatic && src.IsStatic || !src.IsStatic)
                        stack1.Push(node.Links[i].OutputNode);
                }
            }
        }

        return stack2.ToArray();
    }

    public static Node[][] TreeTraversal2(Node root, bool includeStatic)
    {
        Stack<(int, Node)> stack1 = new();
        Stack<(int, Node)> stack2 = new();

        int priority = 0;
        Node node = root;
        stack1.Push((priority, node));
        int groups = 0;
        while (stack1.Count != 0)
        {
            (priority, node) = stack1.Pop();
            var n = FindStack(stack2, x => x.Item2 == node);
            if (n.Item2 != null && n.Item1 < priority)
            {
                RemoveFromStack(stack2, x => x.Item2 == node);
                stack2.Push((priority, node));
            }
            else if (n.Item2 == null)
            {
                stack2.Push((priority, node));
            }

            for (int i = 0; i < node.Links.Count; i++)
            {
                if (node.Links[i].InputNode == node)
                {
                    var src = node.Links[i].OutputNode;
                    if (includeStatic && src.IsStatic || !src.IsStatic)
                        stack1.Push((priority + 1, node.Links[i].OutputNode));
                }
            }

            if (groups < priority)
                groups = priority;
        }
        groups++;
        Node[][] nodes = new Node[groups][];

        var pNodes = stack2.ToArray();

        for (int i = 0; i < groups; i++)
        {
            List<Node> group = new();
            for (int j = 0; j < pNodes.Length; j++)
            {
                if (pNodes[j].Item1 == i)
                    group.Add(pNodes[j].Item2);
            }
            nodes[i] = group.ToArray();
        }

        return nodes;
    }

    public static void RemoveFromStack<T>(Stack<T> values, T value) where T : class
    {
        Stack<T> swap = new();
        while (values.Count > 0)
        {
            var val = values.Pop();
            if (val.Equals(value))
                break;
            swap.Push(val);
        }
        while (swap.Count > 0)
        {
            values.Push(swap.Pop());
        }
    }

    public static void RemoveFromStack2<T>(Stack<T> values, T value) where T : IEquatable<T>
    {
        Stack<T> swap = new();
        while (values.Count > 0)
        {
            var val = values.Pop();
            if (val.Equals(value))
                break;
            swap.Push(val);
        }
        while (swap.Count > 0)
        {
            values.Push(swap.Pop());
        }
    }

    public static void RemoveFromStack<T>(Stack<T> values, Func<T, bool> compare)
    {
        Stack<T> swap = new();
        while (values.Count > 0)
        {
            var val = values.Pop();
            if (compare(val))
                break;
            swap.Push(val);
        }
        while (swap.Count > 0)
        {
            values.Push(swap.Pop());
        }
    }

    public static T FindStack<T>(Stack<T> values, Func<T, bool> compare)
    {
        for (int i = 0; i < values.Count; i++)
        {
            var value = values.ElementAt(i);
            if (compare(value))
                return value;
        }
#pragma warning disable CS8603 // Possible null reference return.
        return default;
#pragma warning restore CS8603 // Possible null reference return.
    }
}