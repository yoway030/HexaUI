using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Numerics;

namespace HexaImGui.Window;

public class ProcessMonitor : BaseWindow
{
    public ProcessMonitor(string windowName, double intervalSec = 0.1f, double simpleShowSec = 10f, double maxStorageSec = 3600f)
        : base(windowName, 0)
    {
        IntervalSec = intervalSec;
        SimpleShowSec = simpleShowSec;
        MaxStorageSec = maxStorageSec;

        SimpleShowCount = (int)(SimpleShowSec / IntervalSec);
        StorageCount = (int)(MaxStorageSec / IntervalSec);

        _cpuUsage = Enumerable.Repeat(0d, StorageCount).ToList();
        _memoryUsage = Enumerable.Repeat(0f, StorageCount).ToList();
        _xAxis = new(StorageCount);

        _lastSampleTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _process.TotalProcessorTime;
    }

    public int SimpleShowCount { get; init; }
    public int StorageCount { get; init; }
    public double IntervalSec { get; init; }
    public double SimpleShowSec { get; init; }
    public double MaxStorageSec { get; init; }

    private DateTime _lastSampleTime;
    private readonly List<int> _xAxis;

    // cpu usage
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly int _processorCount = Environment.ProcessorCount;
    private TimeSpan _lastTotalProcessorTime;
    private readonly List<double> _cpuUsage;
    private double _cpuUsageMax = 100.0;

    // memory usage
    private readonly List<float> _memoryUsage;
    private double _memoryUsageMax = 200.0;

    // style
    private Vector2 _oldWindowPadding = new(0, 0);

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        var windowStyle = ImGui.GetStyle();
        _oldWindowPadding = windowStyle.WindowPadding;
        windowStyle.WindowPadding = new Vector2(1, 1); // 창 내부 여백 제거
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec)
    {
        var windowStyle = ImGui.GetStyle();
        windowStyle.WindowPadding = _oldWindowPadding;
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        Vector2 windowSize = ImGui.GetContentRegionAvail();

        var plotStyle = ImPlot.GetStyle();
        var oldPlotPadding = plotStyle.PlotPadding;
        var oldFitPadding = plotStyle.FitPadding;
        var oldLabelPadding = plotStyle.LabelPadding;
        var oldLegendPadding = plotStyle.LegendPadding;

        // 최소 여백 설정
        plotStyle.PlotPadding = new Vector2(0, 0);      // plot 내부 여백
        plotStyle.FitPadding = new Vector2(0, 0);       // 플롯 외부 여백?
        plotStyle.LabelPadding = new Vector2(2, 2);     // 축 레이블 간격
        plotStyle.LegendPadding = new Vector2(2, 2);    // 범례 간격

        ImPlot.SetNextAxesToFit();
        if (ImPlot.BeginPlot($"##{WindowName}CpuPlot", windowSize, ImPlotFlags.NoInputs))
        {
            ImPlot.SetupAxis(ImAxis.Y1);
            ImPlot.SetupAxis(ImAxis.Y2, ImPlotAxisFlags.Opposite);

            ImPlot.SetupAxisLimits(ImAxis.Y1, 0, _cpuUsageMax, ImPlotCond.Always);
            ImPlot.SetupAxisLimits(ImAxis.Y2, 0, _memoryUsageMax, ImPlotCond.Always);

            {
                // Line Memory
                int startIndex = Math.Max(0, _memoryUsage.Count - SimpleShowCount);
                var span = CollectionsMarshal.AsSpan(_memoryUsage).Slice(startIndex);

                ImPlot.SetAxis(ImAxis.Y2);
                ImPlot.PlotBars("Memory(MB)", ref MemoryMarshal.GetReference(span), SimpleShowCount);
            }

            {
                // Line CPU
                ImPlot.SetAxis(ImAxis.Y1);
                int startIndex = Math.Max(0, _cpuUsage.Count - SimpleShowCount);
                var span = CollectionsMarshal.AsSpan(_cpuUsage).Slice(startIndex);
                ImPlot.PlotLine("CPU(%)", ref MemoryMarshal.GetReference(span), SimpleShowCount);
            }

            ImPlot.EndPlot();
        }

        plotStyle.PlotPadding = oldPlotPadding;
        plotStyle.FitPadding = oldFitPadding;
        plotStyle.LabelPadding = oldLabelPadding;
        plotStyle.LegendPadding = oldLegendPadding;
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        var timeSpan = utcNow - _lastSampleTime;
        if (timeSpan.TotalSeconds < IntervalSec)
        {
            return;
        }

        // CPU 사용률
        _process.Refresh();
        var currentTotalProcessorTime = _process.TotalProcessorTime;
        var cpuUsed = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalSeconds;
        var cpuPercent = ((cpuUsed / timeSpan.TotalSeconds) * 100 / _processorCount);
        _cpuUsage.Add(cpuPercent);
        _cpuUsage.RemoveAt(0);

        // 메모리
        float memMB = _process.WorkingSet64 / (1024f * 1024f);
        _memoryUsage.Add(memMB);
        _memoryUsage.RemoveAt(0);
        _memoryUsageMax = Math.Max(_memoryUsageMax, memMB);

        _lastSampleTime = utcNow;
        _lastTotalProcessorTime = currentTotalProcessorTime;
    }
}