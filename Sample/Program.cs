using ELImGui;
using Hexa.NET.ImGui;
using System.Numerics;
using ELImGui.Window;
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

        DataTableWindow<LogMessage> dataTable = new("LogSurfer");
        dataTable.TableWidget.DefineInfo.Columns.Add(new DataTableColumn("Data1", 60, ImGuiTableColumnFlags.WidthFixed));
        dataTable.TableWidget.DefineInfo.Columns.Add(new DataTableColumn("Data2", 100, ImGuiTableColumnFlags.WidthFixed));
        dataTable.TableWidget.DefineInfo.Columns.Add(new DataTableColumn("Data3", 1024, ImGuiTableColumnFlags.WidthFixed));

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
        TextViewer textViewer = new TextViewer("TextViewer", jsonString, false);
        RecentDataViewer recentDataViewer = new RecentDataViewer("RecentDataViewer");
        CommandConsole console = new CommandConsole("CommandConsole");
        console.IsVisibleImObject = false;
        NodeViewer nodeView = new NodeViewer("NodeViewer");

        visualizer.UiWindows.TryAdd(dataTable.WindowName, dataTable);
        visualizer.UiWindows.TryAdd(processMonitor.WindowName, processMonitor);
        visualizer.UiWindows.TryAdd(recentDataViewer.WindowName, recentDataViewer);
        visualizer.UiWindows.TryAdd(console.WindowName, console);
        visualizer.UiWindows.TryAdd(nodeView.WindowName, nodeView);
        
        SampleWindow sample = new();
        visualizer.UiWindows.TryAdd(sample.WindowName, sample);
        Random random = new Random();
        int logIndex = 0;
        while (visualizer.IsWindowShouldClose == false)
        {
            dataTable.PushData(new LogMessage { DateTime = DateTime.UtcNow, Level = "DEBUG", Message = $"asdafasdasdas fads asdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fads{logIndex}" });
            dataTable.PushData(new LogMessage { DateTime = DateTime.UtcNow, Level = "ERROR", Message = $"asdafasdasdas fads {logIndex}" });
            
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


public class LogMessage : SurfableIndexingData
{
    public DateTime DateTime { get; set; } = DateTime.MinValue;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public Vector4 GetLevelColor(string level) => level switch
    {
        "ERROR" => new Vector4(1, 0.2f, 0.2f, 1),
        "WARN" => new Vector4(1, 0.7f, 0.2f, 1),
        "DEBUG" => new Vector4(0.5f, 0.7f, 1f, 1),
        _ => new Vector4(1, 1, 1, 1),
    };

    public override string FieldsToString => $"{DateTime.ToString("yyyy-MM-ddTHH-mm-ss.fff")} {Level} {Message}";

    public override IEnumerable<Action> GetColumnSetupActions()
    {
        yield return () => ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 180);
        yield return () => ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 60);
        yield return () => ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthFixed, 1000);
        yield break;
    }

    public override IEnumerable<Action> GetFieldDrawActions()
    {
        yield return () => ImGui.TextUnformatted(DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        yield return () =>
        {
            Identicon.DrawIdenticonRect(Level);
            ImGui.SameLine();
            ImGui.TextColored(GetLevelColor(Level), Level);
        };
        yield return () =>
        {
            Identicon.DrawIdenticonRect(Message);
            ImGui.SameLine();
            ImGui.TextUnformatted(Message);
        };
        yield break;
    }

    public override void RenderTooltip()
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"{DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")}");
        ImGui.TextColored(GetLevelColor(Level), Level);

        // Message wrapping
        ImGui.Spacing();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 30); // Adjust wrap position based on font size
        ImGui.TextUnformatted(Message);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}

