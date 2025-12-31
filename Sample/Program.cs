using ELImGui;
using ELImGui.Effect;
using ELImGui.Utils;
using ELImGui.Window;

using Hexa.NET.ImGui;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        ImRenderer.CreateInstance();
        ImRenderer renderer = ImRenderer.Instance;

        ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////
//        ProcessMonitor processMonitor = new("ProcessMonitor");

//        string jsonString =
//"""
//{           
//    "name": "John \"Johnny\" Smith",
//    "age": 32,
//    "email": null,
//    "isActive": true,
//    "roles": ["admin", "editor", "user"],
//    "profile": {
//    "address": {
//        "street": "123 Main St",
//        "city": "New York",
//        "zipcode": "10001"
//    },
//    "phone": "+1-800-555-0199"
//    },
//    "loginHistory": [
//    { "date": "2023-12-01T10:00:00Z", "ip": "192.168.1.1" },
//    { "date": "2023-12-05T14:22:13Z", "ip": "192.168.1.23" }
//    ]
//}
//""";

        var tableRole = new DataTableRuleBuilder<TextRow>()
            .AddColumn(name: "Time", getter: (in TextRow p) => p.Timestamp.ToString())
            .AddColumn(name: "Index", width: 60, getter: (in TextRow p) => p.Index.ToString())
            .AddColumn(name: "Text", width: 300, getter: (in TextRow p) => p.Text.ToString())
            .Build(
                tooltipRender: (in TextRow row) =>
                {
                    ImGui.TextUnformatted($"Time: {row.Timestamp}");
                    ImGui.TextUnformatted($"Index: {row.Index}");
                    ImGui.TextUnformatted($"Text: {row.Text}");
                },
                getRowToString: (in TextRow row) =>
                {
                    return $"{row.Timestamp} {row.Index} {row.Text}";
                });

        IndexedTableWindow<TextRow> indexedTable = new(tableRole, $"{"\U0001F3C4"}IndexedTableWindow");
        indexedTable.Widget.AutoScroll = true;

        DataTableWindow<TextRow> dataTable = new(tableRole, "DataTableWindow");

        //SingleWidgetWindow<JsonWidget> jsonWidgetWindow = new("JsonWidgetWindow");
        //jsonWidgetWindow.InitializeWidget(new JsonWidget("JsonWidget", jsonWidgetWindow.WindowName));
        //jsonWidgetWindow.Widget.JsonText = jsonString;

        //SingleWidgetWindow<TextViewWidget> textViewerWindow = new("TextViewerWindow");
        //textViewerWindow.InitializeWidget(new TextViewWidget("TextViewWidget", textViewerWindow.WindowName));
        //textViewerWindow.Widget.Initialize(jsonString, false);

        //SingleWidgetWindow<SimpleTableWidget> simpleTableWindow = new("SimpleTableWindow");
        //simpleTableWindow.InitializeWidget(new SimpleTableWidget("SimpleTableWidget", simpleTableWindow.WindowName));
        //simpleTableWindow.Widget.Headers = new string[] { "asd", "asd1", "asd2" };

        //SingleWidgetWindow<CommandConsoleWidget> ConsoleWindow = new("ConsoleWindow");
        //ConsoleWindow.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", ConsoleWindow.WindowName));

        //CommandConsole console = new CommandConsole("CommandConsole");
        //console.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", console.WindowName));
        //console.IsVisibleImObject = false;
        //console.Widget.InitSampleCommands();

        //VerticalMultiWidgetWindow multiWidgetWindow = new("MultiWidgetWindow");
        //var mmjson = new JsonWidget("JsonWidget1", multiWidgetWindow.WindowName);
        //mmjson.JsonText = jsonString;
        //multiWidgetWindow.AddWidget(mmjson, 0.5f);

        //var mmtext = new TextViewWidget("TextViewWidget", multiWidgetWindow.WindowName);
        //mmtext.Initialize(jsonString, false);
        //multiWidgetWindow.AddWidget(mmtext, 0.5f);

        //NodeViewer nodeView = new NodeViewer("NodeViewer");

        renderer.RenderActionQueue.Post((context) =>
        {
            var windows = new BaseWindow[]
            {
                indexedTable,
                dataTable,
                //processMonitor,
                //console,
                //nodeView,
                //jsonWidgetWindow,
                //textViewerWindow,
                //simpleTableWindow,
                //ConsoleWindow,
                //multiWidgetWindow,
            };

            foreach (var window in windows)
            {
                context.MainWindows.Add(window.WindowName, window);
            }
        });

        renderer.RenderActionQueue.Post((context) =>
        {
            context.ForegroundEffects.Add(
                new HexagonOverlayEffect(
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddSeconds(10),
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(5500000),
                    new string[] { "On", "Your", "Mark" },
                    ImGuiColorHelper.White, ImGuiColorHelper.BrightenClamped(ImGuiColorHelper.Blue, 0.3f)));
        });

        ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////
        ///

        //ImRenderListActor<string> renderListActor = new();
        //var list = renderListActor.GetPostAdapter();

        //ImRenderDictionaryActor<int, string> renderDictActor = new();
        //var dict = renderDictActor.GetPostAdapter();

        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            dataTable.Widget.InitializeActor(Environment.CurrentManagedThreadId);

            renderer.Initialize("Sample");

            while (renderer.IsWindowShouldClose == false)
            {
                renderer.Loop();
            }

            renderer.Cleanup();
        });

        thread.Start();

        ////////////////////////////

        //var result0 = Task.Run(async () =>
        //{
        //    int logIndex = 0;

        //    while (visualizer.IsWindowShouldClose == false)
        //    {
        //        list.AddPost($"{logIndex}AA");
        //        dict.AddPost(logIndex, $"Value_{logIndex}");

        //        if (logIndex % 5 == 0)
        //        {
        //            var snapshot = await list.SnapshotAsk();
        //            var snapshotDict = await dict.SnapshotAsk();

        //            var name = String.Join(", ", snapshot.Take(5));
        //            name += string.Join(", ", snapshotDict.Values.Take(5));

        //            dataTable.PushData(new PlayerRow
        //            {
        //                Name = name,
        //                Level = logIndex,
        //                Class = "SNAP",
        //                DPS = 20,
        //            });
        //        }

        //        Thread.Sleep(100);
        //        logIndex++;
        //    }
        //});

        //////////////////////////
        ///

        var taskDataTable = Task.Run(async () =>
        {
            while (renderer.IsWindowShouldClose == false)
            {
                try
                {
                    var lines = File.ReadAllLines("data.Text");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        indexedTable.PushData(new TextRow
                        {
                            Timestamp = DateTime.UtcNow,
                            Index = i,
                            Text = lines[i]
                        });

                        await Task.Delay(100);
                    }
                }
                catch
                {
                    indexedTable.PushData(new TextRow
                    {
                        Timestamp = DateTime.UtcNow,
                        Index = 0,
                        Text = "failed file read"
                    });
                }
            }
        });

        


        //var taskAddData = Task.Run(async () =>
        //{

        //    Random random = new Random();
        //    int logIndex = 0;
        //    while (renderer.IsWindowShouldClose == false)
        //    {
        //        dataTable.PushData(new SampleDataType { Name = $"{logIndex}AAAAAAAA😀AAAAAAAAAAAAAAA사나A", Level = logIndex, Class = "EEEE", DPS = 10 });

        //        if (logIndex % 5 == 0)
        //        {
        //            ediTable.AddDataPost(new SampleDataType
        //            {
        //                Name = $"Player_{logIndex}",
        //                Level = random.Next(1, 100),
        //                Class = "Warrior",
        //                DPS = (float)(random.NextDouble() * 1000),
        //            });
        //        }
        //        else if (logIndex % 7 == 0)
        //        {
        //            ediTable.UpdateDataAtPost((logIndex / 10), new SampleDataType
        //            {
        //                Name = $"UpdatedPlayer_{logIndex}",
        //                Level = random.Next(1, 100),
        //                Class = "Mage",
        //                DPS = (float)(random.NextDouble() * 1000),
        //            });
        //        }
        //        else if (logIndex % 11 == 0)
        //        {
        //            ediTable.RemoveDataAtPost(0);
        //        }

        //        await Task.Delay(100);
        //        logIndex++;
        //    }
        //});


        thread.Join();
    }
}