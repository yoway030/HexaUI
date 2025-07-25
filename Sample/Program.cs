using HexaImGui;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        ImGuiManager imGuiManager = new ImGuiManager();

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

        imGuiManager.RegisterDrawCallback(() =>
        {
            logsurfer.DrawDataSurf();
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

    public override void FieldSetupColumn(int field)
    {
        switch (field)
        {
            case 0:
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 180);
                break;
            case 1:
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 60);
                break;
            case 2:
                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthFixed, 1000);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), "Invalid field index");
        }
    }

    public override void FieldDraw(int field)
    {
        switch (field)
        {
            case 0:
                ImGui.TextUnformatted(DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                break;
            case 1:
                ImGui.TextColored(GetLevelColor(Level), Level);
                break;
            case 2:
                ImGui.TextUnformatted(Message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), "Invalid field index");
        }
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

