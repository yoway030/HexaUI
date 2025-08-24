namespace ELImGui.NodeEditor;

using System;

public class LinkDot
{
    public string Message { get; init; } = string.Empty;
    
    public DateTime CreatedTime { get; init; } = DateTime.UtcNow;

    public int DurationMSec { get; init; } = 1000;

    public PinKind Destination { get; init; } = PinKind.Input;

    public uint Color { get; set; } = 0xFFFFFFFF;

    public float DotRadius { get; set; } = 4.0f;
}
