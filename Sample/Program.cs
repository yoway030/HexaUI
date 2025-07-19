using HexaImGui;
using Hexa.NET.ImGui;
using HexaImGui.demo;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        HexaImGuiManager hexaImGuiManager = new HexaImGuiManager();

        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            hexaImGuiManager.Initialize();

            while (hexaImGuiManager.IsWindowShouldClose == false)
            {
                hexaImGuiManager.Loop();
            }

            hexaImGuiManager.Cleanup();
        });
        thread.Start();

        LogSurfer<LogMessage> logsurfer = new();

        hexaImGuiManager.RegisterDrawCallback(() =>
        {
            logsurfer.Draw();
        });

        int logIndex = 0;
        while (hexaImGuiManager.IsWindowShouldClose == false)
        {
            logsurfer.AddMessage(new LogMessage { DateTime = DateTime.UtcNow, Level = "DEBUG", Message = $"asdafasdasdas fads asdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fads{logIndex}" });
            logsurfer.AddMessage(new LogMessage { DateTime = DateTime.UtcNow, Level = "ERROR", Message = $"asdafasdasdas fads {logIndex}" });
            Thread.Sleep(100);
            logIndex++;
        }

        thread.Join();
    }
}


public record LogMessage : ILogWave
{
    public DateTime DateTime { get; set; } = DateTime.MinValue;

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

