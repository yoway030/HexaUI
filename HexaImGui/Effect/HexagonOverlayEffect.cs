namespace ELImGui.Effect;

using ELImGui.Utils;
using Hexa.NET.ImGui;
using System;
using System.Numerics;

public class HexagonOverlayEffect : ForegroundEffect
{
    // 화면을 덮기까지 걸리는 시간. 0이면 덮힌 상태로 시작
    private long _overlayingTick;
    // 덮힌 화면으로 대기하는 시간
    private long _overlayHoldTick;
    // 덮힌 화면을 비우는 시간
    private long _overlayClearTick;

    public HexagonOverlayEffect(
        DateTime startDateTime,
        DateTime endDateTime,
        TimeSpan OverlayingSpan,
        TimeSpan OverlayHoldSpan,
        string[] words,
        Vector4 innerColor,
        Vector4 boarderColor,
        float hexagonSize = 40f)
        : base(startDateTime, endDateTime)
    {
        Words = words;
        InnerColor = ImGui.ColorConvertFloat4ToU32(innerColor);
        BoarderColor = ImGui.ColorConvertFloat4ToU32(boarderColor);
        Size = hexagonSize;

        _overlayingTick = OverlayingSpan.Ticks;
        _overlayHoldTick = OverlayHoldSpan.Ticks;
        _overlayClearTick = (EndDateTime - (StartDateTime + OverlayingSpan + OverlayHoldSpan)).Ticks;
    }

    public float Size { get; init; }
    public string[] Words { get; init; }
    public uint InnerColor { get; init; }
    public uint BoarderColor { get; init; }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        long epochTick = utcNow.Ticks - StartDateTime.Ticks;
        long progressTick = Math.Clamp(epochTick, 0, _durationTicks);

        var pio = ImGui.GetPlatformIO();
        for (int i = 0; i < pio.Viewports.Size; i++)
        {
            var vp = pio.Viewports[i];
            var vmin = vp.Pos;
            var vmax = new Vector2(vp.Pos.X + vp.Size.X, vp.Pos.Y + vp.Size.Y);

            float width = vmax.X - vmin.X;
            float height = vmax.Y - vmin.Y;

            // Pointy-top (윗/아랫면이 꼭짓점)
            float r = Size;                                // center-to-vertex
            float stepX = r * 2;
            float stepY = r * 2;
            int cols = (int)(width / stepX) + 3;
            int rows = (int)(height / stepY) + 3;

            var dl = ImGui.GetForegroundDrawList(vp);

            for (int cx = 0; cx < cols; cx++)
            {
                // 홀수 열은 세로로 반 칸 내려 배치 (벌집 오프셋)
                float colYOffset = ((cx & 1) == 1) ? r : 0f;

                for (int cy = 0; cy < rows; cy++)
                {
                    string identifier = $"{i}_{cx}_{cy}";
                    uint hash = Identicon.Fnv1aHash(identifier);

                    // start ~ _overlaying 까지 화면을 가리고
                    // _overlayHold 동안 대기
                    // (_overlaying + _overlayHold) ~ end 까지 다시 열림.
                    if (_overlayingTick > 0 && progressTick < _overlayingTick && hash % _overlayingTick > progressTick)
                    {
                        continue;
                    }
                    else if (_overlayingTick + _overlayHoldTick > progressTick)
                    {
                    }
                    else
                    {
                        if ((hash % _overlayClearTick) + _overlayingTick + _overlayHoldTick < progressTick)
                        {
                            continue;
                        }
                    }

                    var center = new Vector2(
                        vmin.X + (cx * stepX),
                        vmin.Y + (cy * stepY) + colYOffset
                    );

                    // 꼭짓점 계산: -90°에서 시작해 60°씩 (12시 방향이 꼭짓점)
#pragma warning disable CA2014 // 루프에서 stackalloc을 사용하면 안 됨
                    Span<Vector2> pts = stackalloc Vector2[6];
#pragma warning restore CA2014 // 루프에서 stackalloc을 사용하면 안 됨
                    for (int k = 0; k < 6; k++)
                    {
                        float ang = 60f * k * (float)Math.PI / 180f;
                        pts[k] = new Vector2(
                            center.X + (r * (float)Math.Cos(ang) * 1.3f),
                            center.Y + (r * (float)Math.Sin(ang) * 1.115f)
                        );
                    }

                    dl.AddConvexPolyFilled(ref pts[0], 6, InnerColor);
                    dl.AddPolyline(ref pts[0], 6, BoarderColor, ImDrawFlags.Closed, 4.0f);
                    dl.AddPolyline(ref pts[0], 6, InnerColor, ImDrawFlags.Closed, 1.0f);

                    ImGui.SetWindowFontScale(2.0f); // 2배 확대
                    string word = Words[hash % Words.Length];
                    var wordLen = ImGui.CalcTextSize(word);

                    dl.AddText(center - new Vector2(wordLen.X / 2, wordLen.Y / 2), BoarderColor, word);
                }
            }
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
    }
}
