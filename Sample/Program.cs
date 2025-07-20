using HexaImGui;
using Hexa.NET.ImGui;
using HexaImGui.demo;

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

        DataSurfer<LogMessage> logsurfer = new();

        imGuiManager.RegisterDrawCallback(() =>
        {
            logsurfer.Draw();
        });

        int logIndex = 0;
        while (imGuiManager.IsWindowShouldClose == false)
        {
            logsurfer.AddMessage(new LogMessage { DateTime = DateTime.UtcNow, Level = "DEBUG", Message = $"asdafasdasdas fads asdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fads{logIndex}" });
            logsurfer.AddMessage(new LogMessage { DateTime = DateTime.UtcNow, Level = "ERROR", Message = $"asdafasdasdas fads {logIndex}" });
            Thread.Sleep(100);
            logIndex++;
        }

        thread.Join();
    }
}


public record LogMessage : ISurfableData
{
    public DateTime DateTime { get; set; } = DateTime.MinValue;

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string ToClipboard => $"{DateTime.ToString("yyyy-MM-ddTHH-mm-ss.fff")} {Level} {Message}";
}

