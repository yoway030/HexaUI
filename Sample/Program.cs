using HexaUI;
using Sample.ImVisualize;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            ImVisualizer.Init(Backend.DirectX);
            ImVisualizer.Instance.Run(new ImWindow());
        });
        thread.Start();

        Console.ReadKey();
        ImVisualizer.Instance.Exiting = true;

        thread.Join();

    }
}
