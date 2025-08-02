using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;

namespace HexaImGui.Window;

public class ProcessMonitor
{
    public const int SampleIntervalMilliseconds = 2000;

    public ProcessMonitor(string windowName, int storageTimeSec = 3600)
    {
        WindowName = windowName;
        StorageTimeSec = storageTimeSec;

        // 초기화
        _lastSampleTime = DateTime.Now;
        _lastTotalProcessorTime = _process.TotalProcessorTime;
        // CPU, 메모리 값 초기화
        _cpuUsage.Capacity = _historySize;
        _memoryUsage.Capacity = _historySize;
    }

    public string WindowName { get; init; }
    public int StorageTimeSec { get; init; }

    private readonly int _historySize = 600;

    private DateTime _lastSampleTime;

    // cpu usage
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly int _processorCount = Environment.ProcessorCount;
    private TimeSpan _lastTotalProcessorTime;

    private readonly List<float> _cpuUsage = new(600);

    // memory usage
    private readonly List<float> _memoryUsage = new(600);

    
    
    

    private float[] _xAxis = Array.Empty<float>();

    public void Draw()
    {
        UpdateMetrics();

        ImGui.Begin(WindowName);

        ImPlot.SetNextAxesToFit();
        if (ImPlot.BeginPlot($"{WindowName}Plot", new Vector2(-1, 0), ImPlotFlags.NoInputs))
        {
            ImPlot.SetupAxes("Time", "Metric");

            // X축 시간
            EnsureXAxis();

            // Plot CPU
            if (_cpuUsage.Any())
            {
                Span<float> span = CollectionsMarshal.AsSpan(_cpuUsage);
                ImPlot.PlotLine("CPU(%)", ref MemoryMarshal.GetReference(span), span.Length);
            }

            // Plot Memory
            if (_memoryUsage.Count > 0)
            {
                Span<float> span = CollectionsMarshal.AsSpan(_memoryUsage);
                ImPlot.PlotLine("Memory(MB)", ref MemoryMarshal.GetReference(span), span.Length);
            }

            ImPlot.EndPlot();
        }

        ImGui.End();
    }


    private void UpdateMetrics()
    {
        _process.Refresh();

        // CPU 사용률
        var currentTime = DateTime.Now;
        var currentTotalProcessorTime = _process.TotalProcessorTime;

        if (_lastSampleTime != default)
        {
            var elapsed = (currentTime - _lastSampleTime).TotalSeconds;
            var cpuUsed = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalSeconds;
            float cpuPercent = (float)((cpuUsed / elapsed) * 100 / _processorCount);
            AddSample(_cpuUsage, cpuPercent);
        }

        // 메모리
        float memMB = _process.WorkingSet64 / (1024f * 1024f);
        AddSample(_memoryUsage, memMB);

        _lastSampleTime = currentTime;
        _lastTotalProcessorTime = currentTotalProcessorTime;
    }

    private void AddSample(List<float> list, float value)
    {
        list.Add(value);
        if (list.Count > _historySize)
        {
            list.RemoveAt(0);
        }
    }

    private void EnsureXAxis()
    {
        int count = _cpuUsage.Count;
        if (_xAxis.Length != count)
        {
            _xAxis = new float[count];
            for (int i = 0; i < count; i++)
            {
                _xAxis[i] = i;
            }
        }
    }
}