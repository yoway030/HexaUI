namespace ELImGui.NodeEditor;

using System;

public class LinkDot
{
    public string Message { get; set; } = string.Empty;
    
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public TimeSpan FlowDuration { get; set; }

    public uint Color { get; set; } = 0xFFFFFFFF;

    public float DotRadius { get; set; } = 4.0f;
}
