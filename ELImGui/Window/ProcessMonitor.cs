namespace ELImGui.Window;

using ELImGui.Base;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Diagnostics;
using System.Numerics;
using System.Text;

public class ProcessMonitor : BaseWindow
{
    public enum MonitorKind
    {
        Timestamp = 0,
        CPUUsage = 1,
        MemoryUsage = 2,
    }

    public ProcessMonitor(string windowName, double intervalSec = 0.1f, int displayCount = 600)
        : base(windowName)
    {
        var utcNow = DateTime.UtcNow;

        IntervalSec = intervalSec;
        DisplayCount = displayCount;

        var timestampValue = new ProcessMonitorTimestamp(intervalSec, displayCount);
        var cpuUsageValue = new ProcessMonitorCpuUsage(intervalSec, displayCount);
        var memoryUsageValue = new ProcessMonitorMemUsage(intervalSec, displayCount, cpuUsageValue.Process);
        
        _monitorValues = new List<IMonitorValue>
        {
            timestampValue, // Timestamp는 항상 첫번째
            cpuUsageValue,
            memoryUsageValue,
        };
    }

    private DateTime _lastSampleTime;
    private Vector2 _oldWindowPadding = new(0, 0);

    private List<IMonitorValue> _monitorValues;

    public double IntervalSec { get; init; }
    public int DisplayCount { get; init; }

    public override void OnPrevRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        var windowStyle = ImGui.GetStyle();
        _oldWindowPadding = windowStyle.WindowPadding;
        windowStyle.WindowPadding = new Vector2(1, 1); // 창 내부 여백 제거
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        var windowStyle = ImGui.GetStyle();
        windowStyle.WindowPadding = _oldWindowPadding;
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        var windowSize = ImGui.GetContentRegionAvail();

        var plotStyle = ImPlot.GetStyle();
        var oldPlotPadding = plotStyle.PlotPadding;
        var oldFitPadding = plotStyle.FitPadding;
        var oldLabelPadding = plotStyle.LabelPadding;
        var oldLegendPadding = plotStyle.LegendPadding;

        // 최소 여백 설정
        plotStyle.PlotPadding = new Vector2(0, 10);      // plot 내부 여백
        plotStyle.FitPadding = new Vector2(0, 0);       // 플롯 외부 여백?
        plotStyle.LabelPadding = new Vector2(2, 2);     // 축 레이블 간격
        plotStyle.LegendPadding = new Vector2(2, 2);    // 범례 간격

        // 시간 축 데이터 준비
        _monitorValues[0].UpdatePlotValues(DisplayCount);
        var timeValues = _monitorValues[0].PlotValues;

        int plotCount = _monitorValues.Count - 1;

        if (ImPlot.BeginSubplots($"##{WindowName}SubPlot", plotCount, 1, windowSize, ImPlotSubplotFlags.LinkRows))
        {
            for (int i = 1; i < _monitorValues.Count; i++)
            {
                var monitorValue = _monitorValues[i];
                monitorValue.UpdatePlotValues(DisplayCount);

                ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.1f);
                RenderPlot(monitorValue.Name, windowSize, monitorValue.PlotValues, timeValues, showXAxis: i == plotCount);
                ImPlot.PopStyleVar();
            }

            ImPlot.EndSubplots();
        }

        plotStyle.PlotPadding = oldPlotPadding;
        plotStyle.FitPadding = oldFitPadding;
        plotStyle.LabelPadding = oldLabelPadding;
        plotStyle.LegendPadding = oldLegendPadding;
    }

    private void RenderPlot(string plotName, Vector2 windowSize, in double[] values, in double[]? TimesXAxis = default, bool showXAxis = false)
    {
        if (TimesXAxis == null)
        {
            return;
        }

        ImPlot.SetNextAxesToFit();
        if (ImPlot.BeginPlot($"##{WindowName}#{plotName}", windowSize))
        {
            if (showXAxis == false)
            {
                ImPlot.SetupAxis(ImAxis.X1, ImPlotAxisFlags.NoTickLabels);
            }

            unsafe
            {
                ImPlot.SetupAxisFormat(ImAxis.X1, new ImPlotFormatter(TimeFormatter), null);
            }

            ImPlot.PlotShaded(plotName,
                ref TimesXAxis[0],
                ref values[0],
                DisplayCount);

            ImPlot.PlotLine(plotName,
                ref TimesXAxis[0],
                ref values[0],
                DisplayCount);

            ImPlot.EndPlot();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        var timeSpan = utcNow - _lastSampleTime;
        if (timeSpan.TotalSeconds < IntervalSec)
        {
            return;
        }

        _lastSampleTime = utcNow;

        foreach (var monitorValue in _monitorValues)
        {
            monitorValue.OnUpdate(utcNow);
        }
    }

    private static unsafe int TimeFormatter(double value, byte* buff, int size, void* userData)
    {
        try
        {
            if (buff == null || size <= 0)
            {
                return 0;
            }

            DateTime dt = DateTime.UnixEpoch.AddSeconds(value);
            string formatted = dt.ToString("HH:mm:ss");

            // UTF8 바이트로 변환
            int maxBytes = Math.Min(size - 1, formatted.Length * 3); // UTF8은 최대 3바이트/문자
            Span<byte> span = new Span<byte>(buff, maxBytes);
            int bytesWritten = Encoding.UTF8.GetBytes(formatted, span);

            // Null terminator 추가
            if (bytesWritten < size)
            {
                buff[bytesWritten] = 0;
            }

            return bytesWritten;
        }
        catch
        {
            return 0;
        }
    }
}