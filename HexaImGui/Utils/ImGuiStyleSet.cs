namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.ComponentModel;
using System.Numerics;

public readonly struct ImGuiStyleSetValues
{
    public ImGuiStyleSetValues(Vector4 primary, Vector4 secondary, Vector4 focus, Vector4 background)
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

public sealed class ImGuiStyleSet
{
    private static ImGuiStyleSet? _instance;

    public static ImGuiStyleSet Instance => _instance ?? throw new InvalidOperationException($"{nameof(ImGuiStyleSet)} not initialized");
    public static ImGuiStyleSetValues Values => Instance._values;

    public static void Initialize(ImGuiStyleSetValues? values)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException($"{nameof(ImGuiStyleSet)} already initialized");
        }

        if (values == null)
        {
            _instance = new ImGuiStyleSet(new ImGuiStyleSetValues(
                primary: ImGuiStyleSet.Gray,
                secondary: ImGuiStyleSet.Mint,
                focus: ImGuiStyleSet.LightGray,
                background: ImGuiStyleSet.ABlack
            ));
        }
        else
        {
            _instance = new ImGuiStyleSet(values.Value);
        }

        //Apply();
    }

    public static void Apply()
    {
        var style = ImGui.GetStyle();
        style.Colors[(int)ImGuiCol.TitleBgActive] = Values.Primary;
        style.Colors[(int)ImGuiCol.TitleBg] = ImGuiColorHelper.DarkenClamped(Values.Primary, 0.5f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = ImGuiColorHelper.BlueClamped(style.Colors[(int)ImGuiCol.TitleBg], 0.05f);

        style.Colors[(int)ImGuiCol.TabSelected] = ImGuiColorHelper.BrightenClamped(Values.Primary, 0.4f);
        style.Colors[(int)ImGuiCol.Tab] = ImGuiColorHelper.BrightenClamped(Values.Primary, 0.2f);
        style.Colors[(int)ImGuiCol.TabHovered] = ImGuiColorHelper.DarkenClamped(Values.Primary, 0.3f);
        style.Colors[(int)ImGuiCol.TabSelectedOverline] = Values.Primary;
        style.Colors[(int)ImGuiCol.TabDimmed] = ImGuiColorHelper.DarkenClamped(Values.Primary, 0.8f);
        style.Colors[(int)ImGuiCol.TabDimmedSelected] = ImGuiColorHelper.DarkenClamped(Values.Primary, 0.8f);
        style.Colors[(int)ImGuiCol.TabDimmedSelectedOverline] = Values.Primary;

        style.Colors[(int)ImGuiCol.Header] = Values.Primary;
        style.Colors[(int)ImGuiCol.HeaderHovered] = Values.Focus;
        style.Colors[(int)ImGuiCol.HeaderActive] = Values.Focus;
        style.Colors[(int)ImGuiCol.Button] = Values.Secondary;
        style.Colors[(int)ImGuiCol.ButtonHovered] = Values.Focus;
        style.Colors[(int)ImGuiCol.ButtonActive] = Values.Focus;
        style.Colors[(int)ImGuiCol.FrameBg] = Values.Background;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = Values.Focus;
        style.Colors[(int)ImGuiCol.FrameBgActive] = Values.Focus;
        style.Colors[(int)ImGuiCol.WindowBg] = Values.Background;
    }

    public static Vector4 White => new(1f, 1f, 1f, 1f);
    public static Vector4 Black => new(0f, 0f, 0f, 1f);
    public static Vector4 Gray => new(0.5f, 0.5f, 0.5f, 1f);
    public static Vector4 LightGray => new(0.8f, 0.8f, 0.8f, 1f);
    public static Vector4 BlackGray => new(0.2f, 0.2f, 0.2f, 1f);
    public static Vector4 ABlack => new(0.1f, 0.1f, 0.1f, 1f);
    public static Vector4 Red => new(1f, 0f, 0f, 1f);
    public static Vector4 Green => new(0f, 1f, 0f, 1f);
    public static Vector4 Blue => new(0f, 0f, 1f, 1f);
    public static Vector4 Yellow => new(1f, 1f, 0f, 1f);
    public static Vector4 Magenta => new(1f, 0f, 1f, 1f);
    public static Vector4 Cyan => new(0f, 1f, 1f, 1f);
    public static Vector4 Mint => new(0.4f, 1f, 0.8f, 1f);

    //////////////////////////////////////////

    public static Vector4 TextError => new(1f, 0.2f, 0.2f, 1f);
    public static Vector4 TextNoraml => new(0.8f, 0.8f, 0.8f, 1f);
    public static Vector4 TextWhite => White;
    public static Vector4 TextBlue => Blue;
    public static Vector4 TextGray => Gray;
    public static Vector4 TextString => new(1f, 0.6f, 0.4f, 1f);
    public static Vector4 TextNumber => new(0.4f, 1f, 0.4f, 1f);
    public static Vector4 TextNull => TextError;
    public static Vector4 TextDate => new(1f, 0.7f, 0.2f, 1f);

    private ImGuiStyleSet(ImGuiStyleSetValues values)
    {
        _values = values;
    }

    private ImGuiStyleSetValues _values;
}