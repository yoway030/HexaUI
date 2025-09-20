using ELImGui;
using Hexa.NET.ImGui;
using System.Numerics;
using ELImGui.Window;
using ELImGui.Widget;
using ELImGui.Utils;
using Hexa.NET.ImGui.Widgets;

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
            .AddColumn(name: "Name", width: 160, getter: (in PlayerRow p) => p.Name)
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
        const string EmogiCodePointSurfer = "\U0001F3C4";
        DataTableWindow<PlayerRow> dataTable = new($"{EmogiCodePointSurfer}LogSurfer", tableRole);

        SingleWidgetWindow<JsonWidget> jsonWidgetWindow = new("JsonWidgetWindow");
        jsonWidgetWindow.InitializeWidget(new JsonWidget("JsonWidget", jsonWidgetWindow.WindowName));
        jsonWidgetWindow.Widget.JsonText = jsonString;

        DoubleWidgetWindow<JsonWidget, JsonWidget> jsonDoubleWidgetWindow = new("jsonDoubleWidgetWindow");
        jsonDoubleWidgetWindow.InitializeWidgets(
            new JsonWidget("JsonWidgetFirst", jsonDoubleWidgetWindow.WindowName),
            new JsonWidget("JsonWidgetSecond", jsonDoubleWidgetWindow.WindowName));
        jsonDoubleWidgetWindow.WidgetFirst.JsonText = jsonString;
        jsonDoubleWidgetWindow.WidgetSecond.JsonText = jsonString;

        SingleWidgetWindow<TextViewWidget> textViewerWindow = new("TextViewerWindow");
        textViewerWindow.InitializeWidget(new TextViewWidget("TextViewWidget", textViewerWindow.WindowName));
        textViewerWindow.Widget.Initialize(jsonString, false);

        SingleWidgetWindow<CommandConsoleWidget> ConsoleWindow = new("ConsoleWindow");
        ConsoleWindow.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", ConsoleWindow.WindowName));

        CommandConsole console = new CommandConsole("CommandConsole");
        console.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", console.WindowName));
        console.IsVisibleImObject = false;
        console.Widget.InitSampleCommands();
        NodeViewer nodeView = new NodeViewer("NodeViewer");

        visualizer.UiWindows.TryAdd(dataTable.WindowName, dataTable);
        visualizer.UiWindows.TryAdd(processMonitor.WindowName, processMonitor);
        visualizer.UiWindows.TryAdd(console.WindowName, console);
        visualizer.UiWindows.TryAdd(nodeView.WindowName, nodeView);
        visualizer.UiWindows.TryAdd(jsonWidgetWindow.WindowName, jsonWidgetWindow);
        visualizer.UiWindows.TryAdd(jsonDoubleWidgetWindow.WindowName, jsonDoubleWidgetWindow);
        visualizer.UiWindows.TryAdd(textViewerWindow.WindowName, textViewerWindow);
        visualizer.UiWindows.TryAdd(ConsoleWindow.WindowName, ConsoleWindow);

        SampleWindow sample = new();
        visualizer.UiWindows.TryAdd(sample.WindowName, sample);
        Random random = new Random();
        int logIndex = 0;
        while (visualizer.IsWindowShouldClose == false)
        {
            dataTable.PushData(new PlayerRow { Name = $"{logIndex}AAAAAAAA😀AAAAAAAAAAA{EmogiCodePointSurfer}AAAA사나A", Level = logIndex, Class = "EEEE", DPS = 10 });

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