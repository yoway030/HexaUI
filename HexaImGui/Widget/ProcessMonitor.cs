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

    private readonly List<float> _cpu = new(600);

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
            if (_cpu.Any())
            {
                Span<float> cpuSpan = CollectionsMarshal.AsSpan(_cpu);
                ImPlot.PlotLine("CPU (%)", ref MemoryMarshal.GetReference(cpuSpan), cpuSpan.Length);
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
            AddSample(_cpu, cpuPercent);
        }

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
        int count = _cpu.Count;
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