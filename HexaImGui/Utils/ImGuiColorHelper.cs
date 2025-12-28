namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.Numerics;
using System.Runtime.CompilerServices;

public static class ImGuiColorHelper
{
    /// <summary>
    /// 색상을 어둡게 만듭니다. factor는 0~1 범위로, 0은 원래 색상, 1은 검정색이 됩니다.
    /// </summary>
    public static Vector4 DarkenClamped(Vector4 c, float factor)
    {
        return new Vector4(
            Clamp01(c.X - (c.X * factor)),
            Clamp01(c.Y - (c.Y * factor)),
            Clamp01(c.Z - (c.Z * factor)),
            c.W
        );
    }

    public static uint DarkenU32(uint color, float factor)
    {
        Vector4 v = DarkenClamped(ImGui.ColorConvertU32ToFloat4(color), factor);
        return ImGui.ColorConvertFloat4ToU32(v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector4 TintClamped(Vector4 c, Vector4 target, float factor)
    {
        return new Vector4(
            Clamp01(c.X + (target.X - c.X) * factor),
            Clamp01(c.Y + (target.Y - c.Y) * factor),
            Clamp01(c.Z + (target.Z - c.Z) * factor),
            c.W
        );
    }

    /// <summary>
    /// 색상을 밝게 만듭니다. factor는 0~1 범위로, 0은 원래 색상, 1은 흰색이 됩니다.
    /// </summary>
    public static Vector4 BrightenClamped(Vector4 c, float factor)
    {
        return TintClamped(c, new Vector4(1f, 1f, 1f, c.W), factor);
    }

    public static uint BrightenU32(uint color, float factor)
    {
        Vector4 v = BrightenClamped(ImGui.ColorConvertU32ToFloat4(color), factor);
        return ImGui.ColorConvertFloat4ToU32(v);
    }

    public static Vector4 BlueClamped(Vector4 c, float factor)
    {
        return TintClamped(c, new Vector4(0f, 0f, 1f, c.W), factor);
    }

    public static Vector4 GreenClamped(Vector4 c, float factor)
    {
        return TintClamped(c, new Vector4(0f, 1f, 0f, c.W), factor);
    }

    public static Vector4 RedClamped(Vector4 c, float factor)
    {
        return TintClamped(c, new Vector4(1f, 0f, 0f, c.W), factor);
    }

    public static Vector4 YellowClamped(Vector4 c, float factor)
    {
        return TintClamped(c, new Vector4(1f, 1f, 0f, c.W), factor);
    }

    // hue: 0~360 (도), sat: 0~1, light: 0~1
    // 반환: R,G,B 각각 0~1 범위 (Vector3)
    public static Vector3 HslToRgb(float hue, float sat, float light)
    {
        // 1) 입력 정규화
        hue = Mod(hue, 360f);           // 음수/초과 방지
        sat = Clamp01(sat);
        light = Clamp01(light);

        // 2) 무채색(채도 0) 처리: R=G=B=light
        if (sat <= 0f)
        {
            return new Vector3(light, light, light);
        }

        // 3) 보조값 p/q 계산 (표준 HSL 변환식)
        float q = (light < 0.5f) ? (light * (1f + sat)) : (light + sat - (light * sat));
        float p = (2f * light) - q;

        // 4) H(도) → 0..1 로 변환 후 3원색 위치에서 보간
        float h = hue / 360f;
        float r = Hue2Rgb(p, q, h + (1f / 3f));
        float g = Hue2Rgb(p, q, h);
        float b = Hue2Rgb(p, q, h - (1f / 3f));

        return new Vector3(r, g, b);
    }

    // 보조 함수들
    private static float Hue2Rgb(float p, float q, float t)
    {
        // t를 0..1 범위에 래핑
        if (t < 0f)
        {
            t += 1f;
        }

        if (t > 1f)
        {
            t -= 1f;
        }

        if (t < 1f / 6f)
        {
            return p + ((q - p) * 6f * t);
        }

        if (t < 1f / 2f)
        {
            return q;
        }

        if (t < 2f / 3f)
        {
            return p + ((q - p) * ((2f / 3f) - t) * 6f);
        }

        return p;
    }

    private static float Clamp01(float v) => (v < 0f) ? 0f : (v > 1f ? 1f : v);

    private static float Mod(float x, float m)
    {
        float r = x % m;
        return (r < 0f) ? r + m : r;
    }
}