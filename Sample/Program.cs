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
        DataTableWindow<PlayerRow> dataTable = new(tableRole, $"{"\U0001F3C4"}LogSurfer");

        SingleWidgetWindow<JsonWidget> jsonWidgetWindow = new("JsonWidgetWindow");
        jsonWidgetWindow.InitializeWidget(new JsonWidget("JsonWidget", jsonWidgetWindow.WindowName));
        jsonWidgetWindow.Widget.JsonText = jsonString;

        SingleWidgetWindow<TextViewWidget> textViewerWindow = new("TextViewerWindow");
        textViewerWindow.InitializeWidget(new TextViewWidget("TextViewWidget", textViewerWindow.WindowName));
        textViewerWindow.Widget.Initialize(jsonString, false);

        SingleWidgetWindow<CommandConsoleWidget> ConsoleWindow = new("ConsoleWindow");
        ConsoleWindow.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", ConsoleWindow.WindowName));

        CommandConsole console = new CommandConsole("CommandConsole");
        console.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", console.WindowName));
        console.IsVisibleImObject = false;
        console.Widget.InitSampleCommands();

        MultiWidgetWindow multiWidgetWindow = new("MultiWidgetWindow");
        var mmjson = new JsonWidget("JsonWidget1", multiWidgetWindow.WindowName);
        mmjson.JsonText = jsonString;
        multiWidgetWindow.AddWidget(mmjson);

        var mmtext = new TextViewWidget("TextViewWidget", multiWidgetWindow.WindowName);
        mmtext.Initialize(jsonString, false);
        multiWidgetWindow.AddWidget(mmtext);


        NodeViewer nodeView = new NodeViewer("NodeViewer");

        visualizer.UiWindows.TryAdd(dataTable.WindowName, dataTable);
        visualizer.UiWindows.TryAdd(processMonitor.WindowName, processMonitor);
        visualizer.UiWindows.TryAdd(console.WindowName, console);
        visualizer.UiWindows.TryAdd(nodeView.WindowName, nodeView);
        visualizer.UiWindows.TryAdd(jsonWidgetWindow.WindowName, jsonWidgetWindow);
        visualizer.UiWindows.TryAdd(textViewerWindow.WindowName, textViewerWindow);
        visualizer.UiWindows.TryAdd(ConsoleWindow.WindowName, ConsoleWindow);
        visualizer.UiWindows.TryAdd(multiWidgetWindow.WindowName, multiWidgetWindow);

        //DateTime now = DateTime.UtcNow.AddSeconds(2);
        //visualizer.PostRenderFunc += () =>
        //{
        //    const float size = 40;
        //    const long tickUnit = 3000000;

        //    uint red50 = ImGui.ColorConvertFloat4ToU32(new(0.7f, 0f, 0f, 1f));
        //    uint boarderColor = ImGui.ColorConvertFloat4ToU32(new(0f, 0f, 0f, 1.0f));
        //    int renderPercent = (int)Math.Clamp(DateTime.UtcNow.Ticks - now.Ticks, 0, tickUnit);

        //    var pio = ImGui.GetPlatformIO();
        //    for (int i = 0; i < pio.Viewports.Size; i++)
        //    {
        //        var vp = pio.Viewports[i];
        //        var vmin = vp.Pos;
        //        var vmax = new Vector2(vp.Pos.X + vp.Size.X, vp.Pos.Y + vp.Size.Y);

        //        float width = vmax.X - vmin.X;
        //        float height = vmax.Y - vmin.Y;

        //        // Pointy-top (윗/아랫면이 꼭짓점)
        //        float r = size;                                // center-to-vertex
        //        float stepX = r * 2;
        //        float stepY = r * 2;
        //        int cols = (int)(width / stepX) + 3;
        //        int rows = (int)(height / stepY) + 3;

        //        var dl = ImGui.GetForegroundDrawList(vp);

        //        for (int cx = 0; cx < cols; cx++)
        //        {
        //            // 홀수 열은 세로로 반 칸 내려 배치 (벌집 오프셋)
        //            float colYOffset = ((cx & 1) == 1) ? r : 0f;

        //            for (int cy = 0; cy < rows; cy++)
        //            {
        //                string identifier = $"{i}_{cx}_{cy}";
        //                uint hash = Identicon.Fnv1aHash(identifier);

        //                if (hash % tickUnit > renderPercent)
        //                {
        //                    continue;
        //                }

        //                var center = new Vector2(
        //                    vmin.X + cx * stepX,
        //                    vmin.Y + cy * stepY + colYOffset
        //                );

        //                // 꼭짓점 계산: -90°에서 시작해 60°씩 (12시 방향이 꼭짓점)
        //                Span<Vector2> pts = stackalloc Vector2[6];
        //                for (int k = 0; k < 6; k++)
        //                {
        //                    float ang = (60f * k) * (float)Math.PI / 180f;
        //                    pts[k] = new Vector2(
        //                        center.X + r * (float)Math.Cos(ang) * 1.3f,
        //                        center.Y + r * (float)Math.Sin(ang) * 1.115f
        //                    );
        //                }

        //                dl.AddConvexPolyFilled(ref pts[0], 6, red50);
        //                dl.AddPolyline(ref pts[0], 6, boarderColor, ImDrawFlags.Closed, 4.0f);
        //                dl.AddPolyline(ref pts[0], 6, red50, ImDrawFlags.Closed, 1.0f);
        //                ImGui.SetWindowFontScale(2.0f); // 2배 확대
        //                dl.AddText(center - new Vector2(ImGui.GetFontSize() / 2 * 2.5f, ImGui.GetFontSize() / 2), boarderColor, "ERROR");
        //            }
        //        }
        //    }
        //};

        ////////////////////////////
        ///Log

        SampleWindow sample = new();
        visualizer.UiWindows.TryAdd(sample.WindowName, sample);
        Random random = new Random();
        int logIndex = 0;
        while (visualizer.IsWindowShouldClose == false)
        {
            dataTable.PushData(new PlayerRow { Name = $"{logIndex}AAAAAAAA😀AAAAAAAAAAAAAAA사나A", Level = logIndex, Class = "EEEE", DPS = 10 });

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