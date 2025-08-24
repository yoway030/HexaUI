namespace ELImGui.NodeEditor;

using System;

public class LinkFlowPoint
{
    public string Message { get; set; } = string.Empty;
    
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public TimeSpan FlowDuration { get; set; }

    public uint Color { get; set; } = 0xFFFFFFFF;
}
