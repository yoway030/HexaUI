namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;

public class CommandConsole : SingleWidgetWindow<CommandConsoleWidget>
{
    public CommandConsole(string windowName = $"{nameof(CommandConsole)}")
        : base(windowName)
    {
        // 타이틀등 창 속성 설정
        WindowFlags = ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse;
    }

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        // '`' 입력시 창 오픈
        if (ImGui.IsKeyPressed(ImGuiKey.GraveAccent))
        {
            IsVisibleImObject = !IsVisibleImObject;
            if (IsVisibleImObject)
            {
                var viewport = ImGui.GetMainViewport();
                var size = viewport.Size;
                size.Y = Math.Clamp(size.Y, 0, 400);

                WindowPosition = viewport.Pos;
                WindowSize = size;
            }
        }
    }
}
