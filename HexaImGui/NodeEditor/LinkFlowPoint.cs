namespace HexaImGui.NodeEditor;

using System;

public class LinkFlowPoint
{
    public string Message { get; set; } = string.Empty;
    
    public TimeSpan FlowMilliSec { get; set; }

    public uint Color { get; set; } = 0xFFFFFFFF;
}
