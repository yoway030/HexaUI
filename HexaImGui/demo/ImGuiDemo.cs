using Hexa.NET.ImGui;
using ELImGui.Utils;
using System.Runtime.CompilerServices;

namespace ELImGui.demo;

class ImGuiDemo
{
    private void HelpMarker(string desc)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort) && ImGui.BeginTooltip())
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    private void IMGUI_DEMO_MARKER(string v)
    {
    }

    public void Draw()
    {
        if (!ImGui.Begin("Demo ImGuiCSharp"))
        {
            ImGui.End();
            return;
        }

        if (ImGui.CollapsingHeader("Help"))
        {
            ImGui.SeparatorText("ABOUT THIS DEMO:");
            ImGui.BulletText("Sections below are demonstrating many aspects of the library.");
            ImGui.BulletText("The \"Examples\" menu above leads to more demo contents.");
            ImGui.BulletText("The \"Tools\" menu above gives access to: About Box, Style Editor\n" +
                "and Metrics/Debugger (general purpose Dear ImGui debugging tool).");

            ImGui.SeparatorText("PROGRAMMER GUIDE:");
            ImGui.BulletText("See the ShowDemoWindow() code in imgui_demo.cpp. <- you are here!");
            ImGui.BulletText("See comments in imgui.cpp.");
            ImGui.BulletText("See example applications in the examples/ folder.");
            ImGui.BulletText("Read the FAQ at ");
            ImGui.SameLine(0, 0);
            ImGui.TextLinkOpenURL("https://www.dearimgui.com/faq/");
            ImGui.BulletText("Set 'io.ConfigFlags |= NavEnableKeyboard' for keyboard controls.");
            ImGui.BulletText("Set 'io.ConfigFlags |= NavEnableGamepad' for gamepad controls.");

            ImGui.SeparatorText("USER GUIDE:");
            ImGui.ShowUserGuide();
        }

        if (ImGui.CollapsingHeader("Configuration"))
        {
            var io = ImGui.GetIO();

            if (ImGui.TreeNode("Configuration##2"))
            {
                ImGui.SeparatorText("General");
                ImGui.CheckboxFlags("io.ConfigFlags: NavEnableKeyboard", ref io.ConfigFlags<int>(), (int)ImGuiConfigFlags.NavEnableKeyboard);
                ImGui.SameLine(); HelpMarker("Enable keyboard controls.");
                ImGui.CheckboxFlags("io.ConfigFlags: NavEnableGamepad", ref io.ConfigFlags<int>(), (int)ImGuiConfigFlags.NavEnableGamepad);
                ImGui.SameLine(); HelpMarker("Enable gamepad controls. Require backend to set io.BackendFlags |= ImGuiBackendFlags_HasGamepad.\n\nRead instructions in imgui.cpp for details.");
                ImGui.CheckboxFlags("io.ConfigFlags: NoMouse", ref io.ConfigFlags<int>(), (int)ImGuiConfigFlags.NoMouse);
                ImGui.SameLine(); HelpMarker("Instruct dear imgui to disable mouse inputs and interactions.");

                // The "NoMouse" option can get us stuck with a disabled mouse! Let's provide an alternative way to fix it:
                if ((io.ConfigFlags & ImGuiConfigFlags.NoMouse) != 0)
                {
                    if ((float)ImGui.GetTime() % 0.40f < 0.20f)
                    {
                        ImGui.SameLine();
                        ImGui.Text("<<PRESS SPACE TO DISABLE>>");
                    }
                    // Prevent both being checked
                    if (ImGui.IsKeyPressed(ImGuiKey.Space) || (io.ConfigFlags & ImGuiConfigFlags.NoKeyboard) != 0)
                        io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;
                }

                ImGui.CheckboxFlags("io.ConfigFlags: NoMouseCursorChange", ref io.ConfigFlags<int>(), (int)ImGuiConfigFlags.NoMouseCursorChange);
                ImGui.SameLine(); HelpMarker("Instruct backend to not alter mouse cursor shape and visibility.");
                ImGui.CheckboxFlags("io.ConfigFlags: NoKeyboard", ref io.ConfigFlags<int>(), (int)ImGuiConfigFlags.NoKeyboard);
                ImGui.SameLine(); HelpMarker("Instruct dear imgui to disable keyboard inputs and interactions.");

                ImGui.Checkbox("io.ConfigInputTrickleEventQueue", ref io.ConfigInputTrickleEventQueue);
                ImGui.SameLine(); HelpMarker("Enable input queue trickling: some types of events submitted during the same frame (e.g. button down + up) will be spread over multiple frames, improving interactions with low framerates.");
                ImGui.Checkbox("io.MouseDrawCursor", ref io.MouseDrawCursor);
                ImGui.SameLine(); HelpMarker("Instruct Dear ImGui to render a mouse cursor itself. Note that a mouse cursor rendered via your application GPU rendering path will feel more laggy than hardware cursor, but will be more in sync with your other visuals.\n\nSome desktop applications may use both kinds of cursors (e.g. enable software cursor only when resizing/dragging something).");

                ImGui.SeparatorText("Keyboard/Gamepad Navigation");
                ImGui.Checkbox("io.ConfigNavSwapGamepadButtons", ref io.ConfigNavSwapGamepadButtons);
                ImGui.Checkbox("io.ConfigNavMoveSetMousePos", ref io.ConfigNavMoveSetMousePos);
                ImGui.SameLine(); HelpMarker("Directional/tabbing navigation teleports the mouse cursor. May be useful on TV/console systems where moving a virtual mouse is difficult");
                ImGui.Checkbox("io.ConfigNavCaptureKeyboard", ref io.ConfigNavCaptureKeyboard);
                ImGui.Checkbox("io.ConfigNavEscapeClearFocusItem", ref io.ConfigNavEscapeClearFocusItem);
                ImGui.SameLine(); HelpMarker("Pressing Escape clears focused item.");
                ImGui.Checkbox("io.ConfigNavEscapeClearFocusWindow", ref io.ConfigNavEscapeClearFocusWindow);
                ImGui.SameLine(); HelpMarker("Pressing Escape clears focused window.");
                ImGui.Checkbox("io.ConfigNavCursorVisibleAuto", ref io.ConfigNavCursorVisibleAuto);
                ImGui.SameLine(); HelpMarker("Using directional navigation key makes the cursor visible. Mouse click hides the cursor.");
                ImGui.Checkbox("io.ConfigNavCursorVisibleAlways", ref io.ConfigNavCursorVisibleAlways);
                ImGui.SameLine(); HelpMarker("Navigation cursor is always visible.");

                ImGui.SeparatorText("Windows");
                ImGui.Checkbox("io.ConfigWindowsResizeFromEdges", ref io.ConfigWindowsResizeFromEdges);
                ImGui.SameLine(); HelpMarker("Enable resizing of windows from their edges and from the lower-left corner.\nThis requires ImGuiBackendFlags_HasMouseCursors for better mouse cursor feedback.");
                ImGui.Checkbox("io.ConfigWindowsMoveFromTitleBarOnly", ref io.ConfigWindowsMoveFromTitleBarOnly);
                ImGui.Checkbox("io.ConfigWindowsCopyContentsWithCtrlC", ref io.ConfigWindowsCopyContentsWithCtrlC); // [EXPERIMENTAL]
                ImGui.SameLine(); HelpMarker("*EXPERIMENTAL* CTRL+C copy the contents of focused window into the clipboard.\n\nExperimental because:\n- (1) has known issues with nested Begin/End pairs.\n- (2) text output quality varies.\n- (3) text output is in submission order rather than spatial order.");
                ImGui.Checkbox("io.ConfigScrollbarScrollByPage", ref io.ConfigScrollbarScrollByPage);
                ImGui.SameLine(); HelpMarker("Enable scrolling page by page when clicking outside the scrollbar grab.\nWhen disabled, always scroll to clicked location.\nWhen enabled, Shift+Click scrolls to clicked location.");

                ImGui.SeparatorText("Widgets");
                ImGui.Checkbox("io.ConfigInputTextCursorBlink", ref io.ConfigInputTextCursorBlink);
                ImGui.SameLine(); HelpMarker("Enable blinking cursor (optional as some users consider it to be distracting).");
                ImGui.Checkbox("io.ConfigInputTextEnterKeepActive", ref io.ConfigInputTextEnterKeepActive);
                ImGui.SameLine(); HelpMarker("Pressing Enter will keep item active and select contents (single-line only).");
                ImGui.Checkbox("io.ConfigDragClickToInputText", ref io.ConfigDragClickToInputText);
                ImGui.SameLine(); HelpMarker("Enable turning DragXXX widgets into text input with a simple mouse click-release (without moving).");
                ImGui.Checkbox("io.ConfigMacOSXBehaviors", ref io.ConfigMacOSXBehaviors);
                ImGui.SameLine(); HelpMarker("Swap Cmd<>Ctrl keys, enable various MacOS style behaviors.");
                ImGui.Text("Also see Style->Rendering for rendering options.");

                // Also read: https://github.com/ocornut/imgui/wiki/Error-Handling
                ImGui.SeparatorText("Error Handling");

                ImGui.Checkbox("io.ConfigErrorRecovery", ref io.ConfigErrorRecovery);
                ImGui.SameLine(); HelpMarker(
                    "Options to configure how we handle recoverable errors.\n" +
                    "- Error recovery is not perfect nor guaranteed! It is a feature to ease development.\n" +
                    "- You not are not supposed to rely on it in the course of a normal application run.\n" +
                    "- Possible usage: facilitate recovery from errors triggered from a scripting language or after specific exceptions handlers.\n" +
                    "- Always ensure that on programmers seat you have at minimum Asserts or Tooltips enabled when making direct imgui API call! " +
                    "Otherwise it would severely hinder your ability to catch and correct mistakes!");
                ImGui.Checkbox("io.ConfigErrorRecoveryEnableAssert", ref io.ConfigErrorRecoveryEnableAssert);
                ImGui.Checkbox("io.ConfigErrorRecoveryEnableDebugLog", ref io.ConfigErrorRecoveryEnableDebugLog);
                ImGui.Checkbox("io.ConfigErrorRecoveryEnableTooltip", ref io.ConfigErrorRecoveryEnableTooltip);
                if (!io.ConfigErrorRecoveryEnableAssert && !io.ConfigErrorRecoveryEnableDebugLog && !io.ConfigErrorRecoveryEnableTooltip)
                    io.ConfigErrorRecoveryEnableAssert = io.ConfigErrorRecoveryEnableDebugLog = io.ConfigErrorRecoveryEnableTooltip = true;

                // Also read: https://github.com/ocornut/imgui/wiki/Debug-Tools
                ImGui.SeparatorText("Debug");
                ImGui.Checkbox("io.ConfigDebugIsDebuggerPresent", ref io.ConfigDebugIsDebuggerPresent);
                ImGui.SameLine(); HelpMarker("Enable various tools calling IM_DEBUG_BREAK().\n\nRequires a debugger being attached, otherwise IM_DEBUG_BREAK() options will appear to crash your application.");
                ImGui.Checkbox("io.ConfigDebugHighlightIdConflicts", ref io.ConfigDebugHighlightIdConflicts);
                ImGui.SameLine(); HelpMarker("Highlight and show an error message when multiple items have conflicting identifiers.");
                ImGui.BeginDisabled();
                ImGui.Checkbox("io.ConfigDebugBeginReturnValueOnce", ref io.ConfigDebugBeginReturnValueOnce);
                ImGui.EndDisabled();
                ImGui.SameLine(); HelpMarker("First calls to Begin()/BeginChild() will return false.\n\nTHIS OPTION IS DISABLED because it needs to be set at application boot-time to make sense. Showing the disabled option is a way to make this feature easier to discover.");
                ImGui.Checkbox("io.ConfigDebugBeginReturnValueLoop", ref io.ConfigDebugBeginReturnValueLoop);
                ImGui.SameLine(); HelpMarker("Some calls to Begin()/BeginChild() will return false.\n\nWill cycle through window depths then repeat. Windows should be flickering while running.");
                ImGui.Checkbox("io.ConfigDebugIgnoreFocusLoss", ref io.ConfigDebugIgnoreFocusLoss);
                ImGui.SameLine(); HelpMarker("Option to deactivate io.AddFocusEvent(false) handling. May facilitate interactions with a debugger when focus loss leads to clearing inputs data.");
                ImGui.Checkbox("io.ConfigDebugIniSettings", ref io.ConfigDebugIniSettings);
                ImGui.SameLine(); HelpMarker("Option to save .ini data with extra comments (particularly helpful for Docking, but makes saving slower).");

                ImGui.TreePop();
                ImGui.Spacing();
            }

            IMGUI_DEMO_MARKER("Configuration/Backend Flags");
            if (ImGui.TreeNode("Backend Flags"))
            {
                HelpMarker(
                    "Those flags are set by the backends (imgui_impl_xxx files) to specify their capabilities.\n" +
                    "Here we expose them as read-only fields to avoid breaking interactions with your backend.");

                // FIXME: Maybe we need a BeginReadonly() equivalent to keep label bright?
                ImGui.BeginDisabled();
                ref int flagsAsInt = ref Unsafe.As<ImGuiBackendFlags, int>(ref io.BackendFlags);
                ImGui.CheckboxFlags("io.BackendFlags: HasGamepad", ref flagsAsInt, (int)ImGuiBackendFlags.HasGamepad);
                ImGui.CheckboxFlags("io.BackendFlags: HasMouseCursors", ref flagsAsInt, (int)ImGuiBackendFlags.HasMouseCursors);
                ImGui.CheckboxFlags("io.BackendFlags: HasSetMousePos", ref flagsAsInt, (int)ImGuiBackendFlags.HasSetMousePos);
                ImGui.CheckboxFlags("io.BackendFlags: RendererHasVtxOffset", ref flagsAsInt, (int)ImGuiBackendFlags.RendererHasVtxOffset);
                ImGui.EndDisabled();

                ImGui.TreePop();
                ImGui.Spacing();
            }

            IMGUI_DEMO_MARKER("Configuration/Capture, Logging");
            if (ImGui.TreeNode("Capture/Logging"))
            {
                HelpMarker(
                    "The logging API redirects all text output so you can easily capture the content of " +
                    "a window or a block. Tree nodes can be automatically expanded.\n" +
                    "Try opening any of the contents below in this window and then click one of the \"Log To\" button.");
                ImGui.LogButtons();

                HelpMarker("You can also call ImGui.LogText() to output directly to the log without a visual output.");
                if (ImGui.Button("Copy \"Hello, world!\" to clipboard"))
                {
                    ImGui.LogToClipboard();
                    ImGui.LogText("Hello, world!");
                    ImGui.LogFinish();
                }
                ImGui.TreePop();
            }
        }

        ImGui.End();
    }
}
