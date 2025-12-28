namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.Numerics;

public static class Identicon
{
    public static void RenderIdenticonRect(string input)
    {
        var dl = ImGui.GetWindowDrawList();
        var oldFlags = dl.Flags;
        dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;

        var topLeft = ImGui.GetCursorScreenPos();
        float size = ImGui.GetFontSize();

        // 해시 & 색상
        uint h32 = Fnv1aHash(input);
        RenderIdenticonRect(dl, h32, size, topLeft.X, topLeft.Y);

        dl.Flags = oldFlags;

        ImGui.Dummy(new(size, size)); // 커서 이동
    }

    public static void RenderIdenticonRect(uint h32)
    {
        var dl = ImGui.GetWindowDrawList();
        var oldFlags = dl.Flags;
        dl.Flags &= ~ImDrawListFlags.AntiAliasedFill;

        var topLeft = ImGui.GetCursorScreenPos();
        float size = ImGui.GetFontSize();

        // 해시 & 색상
        RenderIdenticonRect(dl, h32, size, topLeft.X, topLeft.Y);

        dl.Flags = oldFlags;

        ImGui.Dummy(new(size, size)); // 커서 이동
    }

    public static void RenderIdenticonRect(ImDrawListPtr dl, uint h32, float size, float left, float top)
    {
        var rgb = ImGuiColorHelper.HslToRgb(h32, 0.65f, 0.55f);
        uint fg = ImGui.ColorConvertFloat4ToU32(new(rgb.X, rgb.Y, rgb.Z, 1f));

        // 패딩/셀
        float pad = size * 0.00f;
        float inner = size - (pad * 2f);
        float cell = inner / 5f;

        // 패턴. 5x5 그리드, 좌우대칭. 5*3 = 15비트 * 색상 개수 만큼 표현
        uint bits = h32;
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                bool v = (bits & 1) == 1;
                bits >>= 1;

                if (!v)
                {
                    continue;
                }

                void FillCell(int gx)
                {
                    var p0 = new Vector2(left, top) + new Vector2(pad + (gx * cell), pad + (y * cell));
                    var p1 = p0 + new Vector2(cell, cell);
                    dl.AddRectFilled(p0, p1, fg);
                }

                FillCell(x);
                FillCell(4 - x); // 대칭
            }
        }
    }

    public static uint Fnv1aHash(string input)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;
        foreach (char c in input)
        {
            hash ^= (byte)(c & 0xFF);   // 하위 바이트
            hash *= prime;

            hash ^= (byte)(c >> 8);     // 상위 바이트
            hash *= prime;
        }

        return hash;
    }
}