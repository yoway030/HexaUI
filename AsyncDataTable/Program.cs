using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace AsyncDataTable;

internal class Program
{
    static void Main(string[] args)
    {
        //TestDictionaryActor();
        TestListActor();
    }

    static void TestDictionaryActor()
    {
        DictionaryActor<int, string> actor = new(useTask: false);

        var thread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(1000);
                actor.TryWork();

                var input = Console.ReadLine();
                if (input == "exit")
                    break;

                Console.WriteLine(actor.DebugString());
            }

        });
        thread.Start();

        Task.Run(async () =>
        {
            _ = actor.Add(1, "111111111");
            _ = actor.Add(2, "222222222");
            _ = actor.Add(3, "333333333");

            var result1 = await actor.GetAsync(1);
            Console.WriteLine("Snapshot: " + string.Join(", ", result1));

            _ = actor.AddAsync(4, "444444444");

            var result2 = await actor.SnapshotAsync();
            Console.WriteLine("Snapshot: " + string.Join(", ", result2));


            _ = actor.Remove(3);

            var result3 = await actor.SnapshotAsync();
            Console.WriteLine("Snapshot: " + string.Join(", ", result3));

            _ = actor.Clear();

            var result4 = await actor.SnapshotAsync();
            Console.WriteLine("Snapshot: " + string.Join(", ", result4));
        }).Wait();

        thread.Join();
    }

    static void TestListActor()
    {
        ListActor<int> actor = new(useTask:false);

        var thread = new Thread(() =>
        {
            while (true)
            {
                actor.TryWork();

                var input = Console.ReadLine();
                if (input == "exit")
                    break;

                Console.WriteLine(actor.DebugString());
            }

        });
        thread.Start();

        Task.Run(async () =>
        {
            _ = actor.Add(1);
            _ = actor.Add(2);
            _ = actor.Add(3);

            var result1 = await actor.GetAsync(1);
            Console.WriteLine("Snapshot: " + string.Join(", ", result1));

            _ = actor.AddAsync(4);

            var result2 = await actor.SnapshotAsync();
            Console.WriteLine("Snapshot: " + string.Join(", ", result2));


            _ = actor.Remove(1);
            _ = actor.RemoveAt(1);

            var result3 = await actor.SnapshotAsync();
            Console.WriteLine("Snapshot: " + string.Join(", ", result3));

            _ = actor.Clear();

            var result4 = await actor.SnapshotAsync();
            Console.WriteLine("Snapshot: " + string.Join(", ", result4));
        }).Wait();

        thread.Join();
    }
}

public class SingleThreadSynchronizationContext : SynchronizationContext
{
    // 워커 스레드와 공유되며, 모든 스레드에서 접근할 수 있는 작업 큐
    private readonly ConcurrentQueue<Action> _workItems = new();

    // 루프가 계속 돌지 않고, 작업이 들어왔을 때만 스레드가 깨어나도록 알림
    private readonly ManualResetEventSlim _workSignal = new(false);

    // 워커 스레드가 이 플래그를 확인하여 루프를 종료합니다.
    private volatile bool _done;

    public void Complete()
    {
        _done = true;
        _workSignal.Set(); // 워커 스레드를 깨워서 종료 플래그를 확인하도록 합니다.
    }

    // 외부 스레드에서 비동기 작업이 완료된 후 호출되는 메서드
    public override void Post(SendOrPostCallback d, object? state)
    {
        if (_done) return;

        // Action으로 감싸서 큐에 넣습니다.
        _workItems.Enqueue(() => d(state));

        // 워커 스레드에게 새로운 작업이 도착했음을 알립니다.
        _workSignal.Set();
    }

    // 워커 스레드의 메인 루프 (Dispatch Loop)
    public void RunLoop()
    {
        while (!_done)
        {
            // 작업이 들어올 때까지 효율적으로 대기합니다.
            _workSignal.Wait();

            // 시그널을 리셋하여 다음 대기를 준비합니다.
            _workSignal.Reset();

            // 큐에 있는 모든 작업을 처리합니다.
            while (_workItems.TryDequeue(out var workItem))
            {
                workItem();
            }
        }
    }
}

public class SingleWorkerThread
{
    private readonly SingleThreadSynchronizationContext _context = new();
    private readonly Thread _workerThread;

    public SingleWorkerThread()
    {
        _workerThread = new Thread(WorkerEntry)
        {
            IsBackground = true,
            Name = "SingleWorker"
        };
        _workerThread.Start();
    }

    private void WorkerEntry()
    {
        // 1. 이 스레드에 커스텀 SynchronizationContext를 설치합니다.
        // 이 시점부터, 이 스레드 내에서 시작되는 모든 await는 이 컨텍스트로 돌아옵니다.
        SynchronizationContext.SetSynchronizationContext(_context);

        Console.WriteLine($"Worker Thread Started (ID: {Thread.CurrentThread.ManagedThreadId})");

        // 2. 메시지 루프를 시작합니다.
        // 이 스레드는 여기서 대기하며, 큐에 들어온 작업을 처리합니다.
        _context.RunLoop();

        Console.WriteLine("Worker Thread Stopped.");
    }

    public Task PostWorkAsync(int delayMs)
    {
        // 워커 스레드 (_context)에 작업을 Post할 새로운 TCS를 만듭니다.
        var tcs = new TaskCompletionSource();

        // 워커 스레드로 전달할 작업 (Action) 정의
        Action workOnWorker = async () =>
        {
            try
            {
                Console.WriteLine($"Work start on Thread ID: {Thread.CurrentThread.ManagedThreadId}");

                // 이 람다 함수는 이제 워커 스레드 (ID 6)에서 실행됩니다.
                await Task.Delay(delayMs).ConfigureAwait(true);

                // await 후속 작업은 캡처된 컨텍스트(ID 6)로 돌아오거나, 
                // configureAwait(false) 때문에 스레드 풀에서 실행될 수 있습니다. 
                // *이 경우 Thread ID 6을 보장하려면 추가적인 Post가 필요합니다.*

                Console.WriteLine($"Work Finished on Thread ID: {Thread.CurrentThread.ManagedThreadId}");

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        };

        // _context.Post를 통해 워커 스레드의 큐에 작업을 넣습니다.
        _context.Post(_ => workOnWorker(), null);

        return tcs.Task;
    }

    public void Stop()
    {
        _context.Complete();
        _workerThread.Join();
    }
}