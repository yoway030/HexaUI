using ELImGui;
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
        // 랜더러 인스턴스 생성
        ImRenderer.CreateInstance();
        ImRenderer renderer = ImRenderer.Instance;

        // 랜더러 스레드 생성 및 시작
        // ImGui는 별도의 랜더링 스레드에서 초기화와 루프가 실행되어야 함
        Thread thread = new Thread(() =>
        {
            renderer.Initialize("Sample");

            while (renderer.IsWindowShouldClose == false)
            {
                renderer.Loop();
            }

            renderer.Cleanup();
        });
        thread.Start();

        ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////

        // 변경이 적은 간단한 테이블 윈도우
        // ImGui는 별도의 렌더링 스레드에서 초기화와 루프가 실행되어야 하기 때문에 외부 스레드에서 데이터를 추가하려면 동기화가 필요함
        // 동기화가 필요한 경우에는 DataTableWindow<T> 또는 IndexedTableWindow<T>를 사용하는 것을 권장
        SingleWidgetWindow<SimpleTableWidget> simpleTableWindow = new("SimpleTableWindow");
        simpleTableWindow.InitializeWidget(new SimpleTableWidget("SimpleTableWidget", simpleTableWindow.WindowName));
        simpleTableWindow.Widget.Headers = new string[] { "Index", "Text" };
        simpleTableWindow.Widget.Rows.Add(new SimpleTableWidget.ColumnInfo[]
        {
            new SimpleTableWidget.ColumnInfo(SimpleTableWidget.ColumnType.Text, "0", null),
            new SimpleTableWidget.ColumnInfo(SimpleTableWidget.ColumnType.Text, "Hello, World!", null)
        });
        simpleTableWindow.Widget.Rows.Add(new SimpleTableWidget.ColumnInfo[]
        {
            new SimpleTableWidget.ColumnInfo(SimpleTableWidget.ColumnType.Text, "1", null),
            new SimpleTableWidget.ColumnInfo(SimpleTableWidget.ColumnType.Text, "Hello, World!", null)
        });

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

        // 데이터를 추가, 수정, 삭제 가능한 테이블 윈도우
        DataTableWindow<TextRow> dataTable = new(tableRole, "DataTableWindow");

        // 계속 데이터를 삽입하는 테이블 윈도우
        IndexedTableWindow<TextRow> indexedTable = new(tableRole, $"{"\U0001F3C4"}IndexedTableWindow");
        indexedTable.Widget.AutoScroll = true;

        // 명령어 콘솔 윈도우. `를 눌러서 열 수 있음
        CommandConsole console = new CommandConsole("CommandConsole");
        console.InitializeWidget(new CommandConsoleWidget("CommandConsoleWidget", console.WindowName));
        console.IsVisibleImObject = false;
        console.Widget.InitSampleCommands();

        // 프로세스 정보를 모니터링하는 윈도우
        ProcessMonitor processMonitor = new("ProcessMonitor");

        SingleWidgetWindow<JsonWidget> jsonWidgetWindow = new("JsonWidgetWindow");
        jsonWidgetWindow.InitializeWidget(new JsonWidget("JsonWidget", jsonWidgetWindow.WindowName));
        jsonWidgetWindow.Widget.Initialize("data.Json.json", isPath: true);

        SingleWidgetWindow<TextViewWidget> textViewerWindow = new("TextViewerWindow");
        textViewerWindow.InitializeWidget(new TextViewWidget("TextViewWidget", textViewerWindow.WindowName, true, true));
        textViewerWindow.Widget.Initialize("data.Text.txt", true);

        VerticalMultiWidgetWindow multiWidgetWindow = new("MultiWidgetWindow");
        multiWidgetWindow.AddWidget(new RenderActionWidget<string>("M1", multiWidgetWindow.WindowName, "Multi", (in string s) => ImGui.Text(s)), 1f);
        multiWidgetWindow.AddWidget(new RenderActionWidget<string>("M2", multiWidgetWindow.WindowName, "Widget", (in string s) => ImGui.Text(s)), 1f);

        NodeViewer nodeView = new NodeViewer("NodeViewer");
        nodeView.InitSample();

        // 생성한 윈도우들을 렌더러에 등록
        renderer.RenderActionQueue.Post((context) =>
        {
            var windows = new BaseWindow[]
            {
                simpleTableWindow,
                indexedTable,
                dataTable,
                processMonitor,
                console,
                nodeView,
                jsonWidgetWindow,
                textViewerWindow,
                multiWidgetWindow,
            };

            foreach (var window in windows)
            {
                context.MainWindows.Add(window.WindowName, window);
            }
        });

        // 프로세스 시작 세레모니 UI 이펙트 추가
        renderer.RenderActionQueue.Post((context) =>
        {
            context.ForegroundEffects.Add(
                new HexagonOverlayEffect(
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddSeconds(1.5),
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(5500000),
                    new string[] { "On", "Your", "Mark" },
                    ImGuiColorHelper.White, ImGuiColorHelper.BrightenClamped(ImGuiColorHelper.Blue, 0.3f)));
        });

        ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////

        // 외부 스레드에서 테이블에 데이터 삽입 작업을 수행하는 예제
        var taskIndexedTable = Task.Run(async () =>
        {
            while (renderer.IsWindowShouldClose == false)
            {
                try
                {
                    var lines = File.ReadAllLines("data.Text.txt");
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

        // 외부 스레드에서 테이블에 데이터 삽입/수정 작업을 수행하는 예제
        var taskDataTable = Task.Run(async () =>
        {
            Random random = new Random();
            int logIndex = 0;

            while (renderer.IsWindowShouldClose == false)
            {
                if (logIndex % 5 == 0)
                {
                    dataTable.AddDataPost(
                        new TextRow
                        {
                            Timestamp = DateTime.UtcNow,
                            Index = logIndex,
                            Text = random.NextInt64().ToString()
                        });
                }
                else if (logIndex % 7 == 0)
                {
                    dataTable.UpdateDataAtPost((logIndex % 20),
                        new TextRow
                        {
                            Timestamp = DateTime.UtcNow,
                            Index = logIndex,
                            Text = random.NextInt64().ToString()
                        });
                }
                else if (logIndex % 9 == 0)
                {
                    dataTable.RemoveDataAtPost(0);
                }

                await Task.Delay(100);
                logIndex++;
            }
        });

        thread.Join();
    }
}