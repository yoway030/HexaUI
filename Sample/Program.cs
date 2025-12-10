using System.Numerics;

using ELImGui;
using ELImGui.Actor;
using ELImGui.Effect;
using ELImGui.Utils;
using ELImGui.Widget;
using ELImGui.Window;

using Hexa.NET.ImGui;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        ImRenderer.CreateInstance();
        ImRenderer visualizer = ImRenderer.Instance;

        ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////
        ProcessMonitor processMonitor = new("ProcessMonitor");

        string jsonString =
"""
{           
    "name": "John \"Johnny\" Smith",
    "age": 32,
    "email": null,
    "isActive": true,
    "roles": ["admin", "editor", "user"],
    "profile": {
    "address": {
        "street": "123 Main St",
        "city": "New York",
        "zipcode": "10001"
    },
    "phone": "+1-800-555-0199"
    },
    "loginHistory": [
    { "date": "2023-12-01T10:00:00Z", "ip": "192.168.1.1" },
    { "date": "2023-12-05T14:22:13Z", "ip": "192.168.1.23" }
    ]
}
""";

        var tableRole = new DataTableRuleBuilder<PlayerRow>()
            .AddColumn(name: "Name", width: 160,
                getter: (in PlayerRow p) => p.Name,
                renderer: (in PlayerRow p) => ImGuiHelper.TextUnformattedUntil(p.Name, ImGuiHelper.NewLineByte))
            .AddColumn(
                name: "Level",
                width: 60,
                getter: (in PlayerRow p) => p.Level.ToString(),
                renderer: (in PlayerRow p) =>
                {
                    Identicon.RenderIdenticonRect(p.Name);
                    ImGui.SameLine();
                    ImGui.TextUnformatted(p.Level.ToString());
                })
            .AddColumn("DPS", 80, getter: (in PlayerRow p) => p.DPS.ToString())
            .AddColumn("Class", 100, getter: (in PlayerRow p) => p.Class)
            .Build(
                tooltipRender: (in PlayerRow row) =>
                {
                    ImGui.TextUnformatted($"Name: {row.Name}");
                    ImGui.TextUnformatted($"Level: {row.Level}");
                    ImGui.TextUnformatted($"DPS: {row.DPS}");
                    ImGui.TextUnformatted($"Class: {row.Class}");
                },
                getRowToString: (in PlayerRow row) =>
                {
                    return $"{row.Name} {row.Level} {row.DPS} {row.Class}";
                });
        DataTableWindow<PlayerRow> dataTable = new(tableRole, $"{"\U0001F3C4"}LogSurfer");

        SingleWidgetWindow<JsonWidget> jsonWidgetWindow = new("JsonWidgetWindow");
        jsonWidgetWindow.InitializeWidget(new JsonWidget("JsonWidget", jsonWidgetWindow.WindowName));
        jsonWidgetWindow.Widget.JsonText = jsonString;

        SingleWidgetWindow<TextViewWidget> textViewerWindow = new("TextViewerWindow");
        textViewerWindow.InitializeWidget(new TextViewWidget("TextViewWidget", textViewerWindow.WindowName));
        textViewerWindow.Widget.Initialize(jsonString, false);

        SingleWidgetWindow<SimpleTableWidget> simpleTableWindow = new("SimpleTableWindow");
        simpleTableWindow.InitializeWidget(new SimpleTableWidget("SimpleTableWidget", simpleTableWindow.WindowName));
        simpleTableWindow.Widget.Headers = new string[] { "asd", "asd1", "asd2" };

        SingleWidgetWindow<CommandConsoleWidget> ConsoleWindow = new("ConsoleWindow");
        ConsoleWindow.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", ConsoleWindow.WindowName));

        CommandConsole console = new CommandConsole("CommandConsole");
        console.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", console.WindowName));
        console.IsVisibleImObject = false;
        console.Widget.InitSampleCommands();

        VerticalMultiWidgetWindow multiWidgetWindow = new("MultiWidgetWindow");
        var mmjson = new JsonWidget("JsonWidget1", multiWidgetWindow.WindowName);
        mmjson.JsonText = jsonString;
        multiWidgetWindow.AddWidget(mmjson, 0.5f);

        var mmtext = new TextViewWidget("TextViewWidget", multiWidgetWindow.WindowName);
        mmtext.Initialize(jsonString, false);
        multiWidgetWindow.AddWidget(mmtext, 0.5f);

        NodeViewer nodeView = new NodeViewer("NodeViewer");

        visualizer.RenderActionQueue.Post((context) =>
        {
            var windows = new BaseWindow[]
            {
                dataTable,
                processMonitor,
                console,
                nodeView,
                jsonWidgetWindow,
                textViewerWindow,
                simpleTableWindow,
                ConsoleWindow,
                multiWidgetWindow,
            };

            foreach (var window in windows)
            {
                context.MainWindows.Add(window.WindowName, window);
            }
        });

        visualizer.RenderActionQueue.Post((context) =>
        {
            context.ForegroundEffects.Add(
                new HexagonOverlayEffect(
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddSeconds(1.5),
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(5500000),
                    new string[] { "EL", "Server", "On", "Your", "Mark" },
                    new Vector4(1f, 1f, 0.9f, 1), new Vector4(0.3f, 0.3f, 1, 1)));
        });

        ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////
        ///

        ImRenderListActor<string> renderListActor = new();
        var list = renderListActor.GetOuterAdapter();

        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            renderListActor.Initialize(Environment.CurrentManagedThreadId);
            visualizer.Initialize("Sample");

            while (visualizer.IsWindowShouldClose == false)
            {
                renderListActor.Work();
                visualizer.Loop();
            }

            visualizer.Cleanup();
        });
        thread.Start();

        ////////////////////////////

        var result0 = Task.Run(async () =>
        {
            int logIndex = 0;

            while (visualizer.IsWindowShouldClose == false)
            {
                list.AddPost($"{logIndex}AA");
                if (logIndex % 5 == 0)
                {
                    var snapshot = await list.SnapshotAsk();
                    dataTable.PushData(new PlayerRow
                    {
                        Name = String.Join(", ", snapshot.Take(100)),
                        Level = logIndex,
                        Class = "SNAP",
                        DPS = 20,
                    });
                }

                Thread.Sleep(100);
                logIndex++;
            }
        });


        Random random = new Random();
        int logIndex = 0;
        while (visualizer.IsWindowShouldClose == false)
        {
            dataTable.PushData(new PlayerRow { Name = $"{logIndex}AAAAAAAA😀\n\rAAAAAAAAAAAAAAA사나A", Level = logIndex, Class = "EEEE", DPS = 10 });

            Thread.Sleep(100);
            logIndex++;
        }


        thread.Join();
    }
}

public sealed class PlayerRow
{
    public string Name { get; init; } = "";
    public int Level { get; init; }
    public float DPS { get; init; }
    public string Class { get; init; } = "";
}