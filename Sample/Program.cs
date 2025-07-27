using HexaImGui;
using Hexa.NET.ImGui;
using System.Numerics;
using HexaImGui.Window;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        ImVisualizer imGuiManager = new ImVisualizer();

        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            imGuiManager.Initialize();

            while (imGuiManager.IsWindowShouldClose == false)
            {
                imGuiManager.Loop();
            }

            imGuiManager.Cleanup();
        });
        thread.Start();

        DataSurfer<LogMessage> logsurfer = new("LogSurfer");

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
        TextViewer jsonViewer = new TextViewer("ASDF", jsonString, false);

        imGuiManager.RegisterDrawCallback(() =>
        {
            logsurfer.DrawDataSurf();
            jsonViewer.Draw();
        });

        int logIndex = 0;
        while (imGuiManager.IsWindowShouldClose == false)
        {
            logsurfer.PushData(new LogMessage { DateTime = DateTime.UtcNow, Level = "DEBUG", Message = $"asdafasdasdas fads asdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fads{logIndex}" });
            logsurfer.PushData(new LogMessage { DateTime = DateTime.UtcNow, Level = "ERROR", Message = $"asdafasdasdas fads {logIndex}" });
            Thread.Sleep(100);
            logIndex++;
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

    public override int DrawableFieldCount => 3;    // DateTime, Level, Message

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
        yield return () => ImGui.TextColored(GetLevelColor(Level), Level);
        yield return () => ImGui.TextUnformatted(Message);
        yield break;
    }

    public override void TooltipDraw()
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

