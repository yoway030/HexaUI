using ELImGui;
using Hexa.NET.ImGui;
using System.Numerics;
using ELImGui.Window;
using ELImGui.Widget;
using ELImGui.Utils;
using Hexa.NET.ImGui.Widgets;
using ELImGui.Effect;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        ImVisualizer visualizer = new ImVisualizer();

        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            visualizer.Initialize("Sample");

            while (visualizer.IsWindowShouldClose == false)
            {
                visualizer.Loop();
            }

            visualizer.Cleanup();
        });
        thread.Start();

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
                tooltipRender : (in PlayerRow row) =>
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted($"Name: {row.Name}");
                    ImGui.TextUnformatted($"Level: {row.Level}");
                    ImGui.TextUnformatted($"DPS: {row.DPS}");
                    ImGui.TextUnformatted($"Class: {row.Class}");
                    ImGui.EndTooltip();
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

        visualizer.UiWindows.TryAdd(dataTable.WindowName, dataTable);
        visualizer.UiWindows.TryAdd(processMonitor.WindowName, processMonitor);
        visualizer.UiWindows.TryAdd(console.WindowName, console);
        visualizer.UiWindows.TryAdd(nodeView.WindowName, nodeView);
        visualizer.UiWindows.TryAdd(jsonWidgetWindow.WindowName, jsonWidgetWindow);
        visualizer.UiWindows.TryAdd(textViewerWindow.WindowName, textViewerWindow);
        visualizer.UiWindows.TryAdd(simpleTableWindow.WindowName, simpleTableWindow);
        visualizer.UiWindows.TryAdd(ConsoleWindow.WindowName, ConsoleWindow);
        visualizer.UiWindows.TryAdd(multiWidgetWindow.WindowName, multiWidgetWindow);


        visualizer.ForegroundEffects.Enqueue(new HexagonOverlayEffect(
            DateTime.UtcNow, 
            DateTime.UtcNow.AddSeconds(1.5),
            new TimeSpan(0,0,0),
            new TimeSpan(5500000),
            new string[] { "EL", "Server", "On", "Your", "Mark" },
            new Vector4(1f, 1f, 0.9f, 1), new Vector4(0.3f, 0.3f, 1, 1)));

        visualizer.ForegroundEffects.Enqueue(new HexagonOverlayEffect(
            DateTime.UtcNow.AddSeconds(3), DateTime.UtcNow.AddSeconds(4),
            new TimeSpan(3000000),
            new TimeSpan(2000000),
            new string[] { "Error", "Err" },
            new Vector4(0.7f, 0, 0, 1), new Vector4(0, 0, 0, 1)));

        ////////////////////////////
        ///Log

        SampleWindow sample = new();
        visualizer.UiWindows.TryAdd(sample.WindowName, sample);
        Random random = new Random();
        int logIndex = 0;
        while (visualizer.IsWindowShouldClose == false)
        {
            dataTable.PushData(new PlayerRow { Name = $"{logIndex}AAAAAAAA😀\n\rAAAAAAAAAAAAAAA사나A", Level = logIndex, Class = "EEEE", DPS = 10 });

            Thread.Sleep(100);
            logIndex++;

            //{
            //    int rnd = random.Next();
            //    string key = $"SampleKey{rnd % 100}";
            //    string value = $"SampleValue{rnd % 100}";
            //    recentDataViewer.PushData(key, new DataSample { Column1 = key, Column2 = value });
            //}
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