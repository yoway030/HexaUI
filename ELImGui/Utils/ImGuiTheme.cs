namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.Numerics;

public readonly struct ImGuiThemeValues
{
    public ImGuiThemeValues(Vector4 primary, Vector4 secondary, Vector4 focus, Vector4 background)
    {
        Primary = primary;
        Secondary = secondary;
        Focus = focus;
        Background = background;
    }

    public readonly Vector4 Primary;
    public readonly Vector4 Secondary;
    public readonly Vector4 Focus;
    public readonly Vector4 Background;
}

/// <summary>
/// 테마 정보 및 테마 정보 관리 싱글톤
/// </summary>
public sealed class ImGuiTheme
{
    private static ImGuiTheme? _instance;

    public static ImGuiTheme Instance => _instance ?? throw new InvalidOperationException($"{nameof(ImGuiTheme)} not initialized");
    public static ImGuiThemeValues Values => Instance._values;

    public static void Initialize(ImGuiThemeValues? values)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException($"{nameof(ImGuiTheme)} already initialized");
        }

        _instance = values == null
            ? new ImGuiTheme(new ImGuiThemeValues(
                primary: ImGuiColorHelper.DefaultPrimary,
                secondary: ImGuiColorHelper.DefaultSecondary,
                focus: ImGuiColorHelper.DefaultFocus,
                background: ImGuiColorHelper.DefaultBackground
            ))
            : new ImGuiTheme(values.Value);

        _instance.Apply();
    }

    public void Apply()
    {
        var style = ImGui.GetStyle();

        style.Colors[(int)ImGuiCol.Button] = Values.Primary;
        style.Colors[(int)ImGuiCol.ButtonHovered] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.Button], 0.2f);
        style.Colors[(int)ImGuiCol.ButtonActive] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.Button], 0.4f);

        style.Colors[(int)ImGuiCol.TitleBgActive] = Values.Primary;
        style.Colors[(int)ImGuiCol.TitleBg] = ImGuiColorHelper.DarkenClamped(style.Colors[(int)ImGuiCol.TitleBgActive], 0.9f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = ImGuiColorHelper.BlueClamped(style.Colors[(int)ImGuiCol.TitleBg], 0.1f);

        style.Colors[(int)ImGuiCol.TabSelected] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.TitleBgActive], 0.2f);
        style.Colors[(int)ImGuiCol.TabSelectedOverline] = Values.Secondary;
        style.Colors[(int)ImGuiCol.Tab] = ImGuiColorHelper.DarkenClamped(style.Colors[(int)ImGuiCol.TabSelected], 0.2f);
        style.Colors[(int)ImGuiCol.TabHovered] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.TabSelected], 0.4f);

        style.Colors[(int)ImGuiCol.TabDimmed] = ImGuiColorHelper.DarkenClamped(style.Colors[(int)ImGuiCol.Tab], 0.5f);
        style.Colors[(int)ImGuiCol.TabDimmedSelected] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.TabDimmed], 0.15f);
        style.Colors[(int)ImGuiCol.TabDimmedSelectedOverline] = ImGuiColorHelper.DarkenClamped(style.Colors[(int)ImGuiCol.TabSelectedOverline], 0.5f);

        style.Colors[(int)ImGuiCol.CheckMark] = Values.Secondary;

        style.Colors[(int)ImGuiCol.Header] = ImGuiColorHelper.AlphaBlendClamped(Values.Secondary, 0.8f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = ImGuiColorHelper.AlphaBlendClamped(Values.Secondary, 0.7f);
        style.Colors[(int)ImGuiCol.HeaderActive] = ImGuiColorHelper.AlphaBlendClamped(Values.Secondary, 0.6f);

        style.Colors[(int)ImGuiCol.FrameBg] = ImGuiColorHelper.BrightenClamped(Values.Background, 0.05f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.FrameBg], 0.2f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.FrameBg], 0.1f);

        style.Colors[(int)ImGuiCol.WindowBg] = Values.Background;
        style.Colors[(int)ImGuiCol.ChildBg] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.WindowBg], 0.02f);
        style.Colors[(int)ImGuiCol.PopupBg] = ImGuiColorHelper.BrightenClamped(style.Colors[(int)ImGuiCol.WindowBg], 0.02f);
    }

    private ImGuiTheme(ImGuiThemeValues values)
    {
        _values = values;
    }

    private ImGuiThemeValues _values;
}