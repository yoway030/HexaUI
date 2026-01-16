namespace ELImGui.Window;

using ELImGui.Base;
using System;
using System.Diagnostics;

public interface IMonitorValue
{
    string Name { get; }

    double[] PlotValues { get; }

    void UpdatePlotValues(int count);

    void OnUpdate(DateTime utcNow);
}

public abstract class MonitorValue<T> : IMonitorValue
    where T : struct
{
    public MonitorValue(string name, double intervalSec, int storeCount)
    {
        _name = name;
        _intervalSec = intervalSec;
        _storeCount = storeCount;
        _plotBuffer = new T[storeCount];
        _ringBuffer = new RingBuffer<T>(storeCount);
        _ringBuffer.Fill(default);
    }

    protected readonly string _name;
    protected readonly double _intervalSec;
    protected readonly int _storeCount;
    protected readonly RingBuffer<T> _ringBuffer;
    protected T[] _plotBuffer;

    public string Name => _name;
    public double IntervalSec => _intervalSec;
    public int StoreCount => _storeCount;
    public T[] PlotBuffer => _plotBuffer;

    public void Add(T value)
    {
        _ringBuffer.Add(value);
    }

    public void UpdatePlotValues(int count)
    {
        _ringBuffer.CopyRecentTo(_plotBuffer.AsSpan(), count);
    }

    public abstract double[] PlotValues { get; }

    public abstract void OnUpdate(DateTime utcNow);
}

public class ProcessMonitorTimestamp : MonitorValue<DateTime>
{
    public ProcessMonitorTimestamp(double intervalSec, int storeCount)
        : base("Time", intervalSec, storeCount)
    {
        var utcNow = DateTime.UtcNow;

        for (int i = 0; i < StoreCount; i++)
        {
            double oldTimeSpan = (StoreCount - i) * IntervalSec;
            Add(utcNow.AddSeconds(-oldTimeSpan));
        }
    }

    public override double[] PlotValues => _plotBuffer.Select(dt => (dt.ToLocalTime() - DateTime.UnixEpoch).TotalSeconds).ToArray();

    public override void OnUpdate(DateTime utcNow)
    {
        Add(utcNow);
    }
}

public class ProcessMonitorCpuUsage : MonitorValue<double>
{
    public ProcessMonitorCpuUsage(double intervalSec, int storeCount)
        : base("CPU(%)", intervalSec, storeCount)
    {
    }

    private readonly int _processorCount = Environment.ProcessorCount;
    private DateTime _lastUpdateDatetime = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime;

    public override double[] PlotValues => _plotBuffer;

    public override void OnUpdate(DateTime utcNow)
    {
        var timeSpan = utcNow - _lastUpdateDatetime;

        // CPU 사용률
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var currentTotalProcessorTime = process.TotalProcessorTime;
        double cpuUsed = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalSeconds;
        double cpuPercent = Math.Max(0, cpuUsed / timeSpan.TotalSeconds * 100 / _processorCount);
        Add(cpuPercent);

        _lastTotalProcessorTime = currentTotalProcessorTime;
        _lastUpdateDatetime = utcNow;
    }
}

public class ProcessMonitorMemUsage : MonitorValue<double>
{
    public ProcessMonitorMemUsage(double intervalSec, int storeCount)
        : base("Mem(MB)", intervalSec, storeCount)
    {
    }

    public override double[] PlotValues => _plotBuffer;

    public override void OnUpdate(DateTime utcNow)
    {
        // 메모리
        // Process를 Refresh하지 않음. CPU 사용률을 구하는 쪽에서 이미 Refresh했기 때문
        var process = Process.GetCurrentProcess();
        float memMB = process.WorkingSet64 / (1024f * 1024f);
        Add(memMB);
    }
}