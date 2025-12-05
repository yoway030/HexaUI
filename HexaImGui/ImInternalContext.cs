namespace ELImGui;

using ELImGui.Effect;
using ELImGui.Window;

/// <summary>
/// Initialize 이후 ImGui Renderer 스레드에서 만 동기적으로 접근할 데이터를 모아둠
/// </summary>
public class ImInternalContext
{
    public readonly Dictionary<string /*windowName*/, BaseWindow> MainWindows = new();
    public readonly Dictionary<string /*windowName*/, BaseWindow> SubWindows = new();

    public readonly List<IImMenu> MainMenus = new();
    public readonly List<ForegroundEffect> ForegroundEffects = new();
}