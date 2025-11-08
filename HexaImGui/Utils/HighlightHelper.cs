using System.Numerics;

namespace ELImGui.Utils;

public class HighlightHelper
{
    public static readonly Vector4 DefaultHighLightColor = new(0.0f, 1.0f, 0.0f, 0.2f);

    public Vector4 HighLightColor = DefaultHighLightColor;
}
