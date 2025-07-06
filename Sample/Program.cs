using HexaUI;
using Sample.ImVisualizer;

namespace Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        // 스레드 생성 및 시작
        Thread thread = new Thread(() =>
        {
            ImVisualizer.ImVisualizer.Init(Backend.DirectX);
            ImVisualizer.ImVisualizer.Instance.Run(new ImWindow());
        });
        thread.Start();

        Console.ReadKey();
        ImVisualizer.ImVisualizer.Instance.Exiting = true;

        thread.Join();

    }
}
