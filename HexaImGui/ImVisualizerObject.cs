namespace ELImGui;

using System;

public interface ImVisualizerObject
{
    void RenderVisualizer(DateTime utcNow, double deltaSec);
    void UpdateVisualizer(DateTime utcNow, double deltaSec);
}

public interface ImVisualizerWindow : ImVisualizerObject
{
    public string WindowName { get; init; }
    public int WindowDepth { get; init; }
    public string WindowId { get; init; }
    public bool IsVisible { get; set; }
}
