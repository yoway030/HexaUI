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

        LogViewer logviewer = new LogViewer();

        hexaImGuiManager.RegisterDrawCallback(() =>
        {
            logviewer.Draw();
        });

        int logIndex = 0;
        while (hexaImGuiManager.IsWindowShouldClose == false)
        {
            logviewer.AddMessage(new LogMessage { DateTime = DateTime.UtcNow, Level = "DEBUG", Message = $"asdafasdasdas fads asdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fadsasdafasdasdas fads{logIndex}" });
            logviewer.AddMessage(new LogMessage { DateTime = DateTime.UtcNow, Level = "ERROR", Message = $"asdafasdasdas fads {logIndex}" });
            Thread.Sleep(100);
            logIndex++;
        }

        thread.Join();
    }
}
