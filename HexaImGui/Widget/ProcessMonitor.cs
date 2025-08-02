using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;

namespace HexaImGui.Widget;

public class ProcessMonitor
{
    private readonly int _historySize = 600;

    private readonly List<float> _cpuValues = new(600);
    private readonly List<float> _memoryValues = new(600);

    private readonly Process _process = Process.GetCurrentProcess();
    private TimeSpan _lastTotalProcessorTime;
    private DateTime _lastSampleTime;
    private readonly int _processorCount = Environment.ProcessorCount;

    private float[] _xAxis = Array.Empty<float>();

    public void Draw()
    {
        UpdateMetrics();

        ImGui.Begin("Process Monitor - ImPlot");

        ImPlot.SetNextAxesToFit();
        if (ImPlot.BeginPlot("Process Stats", new Vector2(-1, 0), ImPlotFlags.NoInputs))
        {
            ImPlot.SetupAxes("Time", "Metric");

            // X축 시간
            EnsureXAxis();

            // Plot CPU
            if (_cpuValues.Any())
            {
                Span<float> span = CollectionsMarshal.AsSpan(_cpuValues);
                ImPlot.PlotLine("CPU (%)", ref MemoryMarshal.GetReference(span), span.Length);
            }

            // Plot Memory
            if (_memoryValues.Count > 0)
            {
                Span<float> span = CollectionsMarshal.AsSpan(_memoryValues);
                ImPlot.PlotLine("Memory (MB)", ref MemoryMarshal.GetReference(span), span.Length);
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
            AddSample(_cpuValues, cpuPercent);
        }

        // 메모리
        float memMB = _process.WorkingSet64 / (1024f * 1024f);
        AddSample(_memoryValues, memMB);

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
        int count = _cpuValues.Count;
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