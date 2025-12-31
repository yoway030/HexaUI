namespace ELImGui.Effect;

using ELImGui.Utils;
using Hexa.NET.ImGui;
using System;
using System.Numerics;

public class SnowfallEffect : ForegroundEffect
{
    public float SnowSize { get; init; } = 3.0f;
    public float SnowAlpha { get; init; } = 0.8f;   // 0 ~ 1
    public float FallSpeed { get; init; } = 60.0f;  // pixel / sec
    public uint SnowColor { get; init; }

    public SnowfallEffect(
        DateTime startDateTime,
        DateTime endDateTime,
        float snowSize,
        float snowAlpha,
        float fallSpeed,
        Vector4 snowColor)
        : base(startDateTime, endDateTime)
    {
        SnowSize = snowSize;
        SnowAlpha = Math.Clamp(snowAlpha, 0f, 1f);
        FallSpeed = fallSpeed;

        var c = snowColor;
        c.W *= SnowAlpha;
        SnowColor = ImGui.ColorConvertFloat4ToU32(c);
    }

    public override void OnRender(
        DateTime utcNow,
        double deltaSec,
        ImInternalContext imInternalContext)
    {
        long epochTick = utcNow.Ticks - StartDateTime.Ticks;
        if (epochTick < 0)
        {
            return;
        }

        float timeSec = epochTick / (float)TimeSpan.TicksPerSecond;

        var pio = ImGui.GetPlatformIO();
        for (int i = 0; i < pio.Viewports.Size; i++)
        {
            var vp = pio.Viewports[i];
            var vmin = vp.Pos;
            var vmax = vmin + vp.Size;

            float width = vp.Size.X;
            float height = vp.Size.Y;

            var dl = ImGui.GetForegroundDrawList(vp);

            // 눈 밀도 (간격)
            float step = SnowSize * 20.0f;
            int cols = (int)(width / step) + 2;
            int rows = (int)(height / step) + 2;

            for (int cx = 0; cx < cols; cx++)
            {
                for (int cy = 0; cy < rows; cy++)
                {
                    string id = $"{i}_{cx}_{cy}";
                    uint hash = Identicon.Fnv1aHash(id);

                    // X 위치는 고정
                    float x = vmin.X + (cx * step) + (hash % 1000 / 1000f * step);

                    // Y 위치는 시간에 따라 아래로 이동
                    float baseY = (cy * step) + (hash % 500 / 500f * step);
                    float y = (baseY + (timeSec * FallSpeed)) % (height + step);

                    var pos = new Vector2(x, vmin.Y + y);

                    // 살짝 흔들리는 느낌 (좌우)
                    float sway =
                        MathF.Sin((timeSec * 1.5f) + (hash & 0xFF)) * SnowSize;
                    pos.X += sway;

                    dl.AddCircleFilled(pos, SnowSize, SnowColor);
                }
            }
        }
    }

    public override void OnUpdate(
        DateTime utcNow,
        double deltaSec,
        ImInternalContext imInternalContext)
    {
    }
}