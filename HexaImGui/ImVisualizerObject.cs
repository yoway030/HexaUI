using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexaImGui;

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
