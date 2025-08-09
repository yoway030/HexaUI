using Hexa.NET.ImGui;
using System.Numerics;
using System.Text;
using HexaImGui.Utils;
using System.Net.Http.Headers;

namespace HexaImGui.Window;

public class TextViewer : BaseWindow
{
    public TextViewer(string windowName, string textOrPath, bool isPath)
        : base(windowName, 0)
    {
        WindowName = windowName;

        if (isPath == true)
        {
            Path = textOrPath;

            if (string.IsNullOrWhiteSpace(Path))
            {
                ErrorText = "파일 경로가 null이거나 비어 있습니다.";
            }

            try
            {
                if (!File.Exists(Path))
                {
                    ErrorText = "지정된 파일을 찾을 수 없습니다.";
                }

                Text = File.ReadAllText(Path);

                if (string.IsNullOrWhiteSpace(Text))
                {
                    ErrorText = "파일 내용이 비어 있습니다.";
                }

            }
            catch (IOException ex)
            {
                ErrorText = $"파일 입출력 오류: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorText = $"알 수 없는 오류: {ex.Message}";
            }
        }
        else
        {
            Text = textOrPath;
        }

        Lines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
    }

    public string? ErrorText { get; private set; } = null;
    public string Text { get; private set; } = string.Empty;
    public List<string> Lines { get; private set; } = null!;
    public string? Path { get; private set; } = null;
    private ImGuiSelectionBasicStorage _selection = new();

    public string HighlightText = string.Empty;
    private HashSet<int>? _highlightedLines = null;

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        int lineCount = Lines.Count;

        if (ImGui.BeginChild($"{WindowName}Panel#{WindowDepth}", ImGuiChildFlags.AutoResizeY) == false)
        {
            ImGui.EndChild();
            return;
        }

        ImGui.Text($"FromFile: {Path ?? "null"}");
        ImGuiHelper.SpacingSameLine();
        ImGui.Text($"Lines: {lineCount}");

        // filter input
        ImGui.Text("Highlight:");
        ImGuiHelper.SpacingSameLine();

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20.0f);
        if (ImGui.InputText($"##{WindowName}Highlight{WindowDepth}", ref HighlightText, 100, ImGuiInputTextFlags.EnterReturnsTrue) == true)
        {
            OnHighlightChange();
        }

        ImGui.SeparatorText("Text");
        ImGui.EndChild();

        if (ImGui.BeginChild($"{WindowName}Text#{WindowDepth}") == false)
        {
            ImGui.EndChild();
            return;
        }

        if (ErrorText != null)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error : {ErrorText}");
        }
        else if (lineCount == 0)
        {
            ImGui.Text("No text to display.");
        }
        else
        {
            ImGuiMultiSelectIOPtr ms_io = ImGui.BeginMultiSelect(
                ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                _selection.Size,
                lineCount);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    return (uint)index;
                });
            _selection.ApplyRequests(ms_io);

            for (int i = 0; i < lineCount; i++)
            {
                string line = Lines[i];
                bool item_is_selected = _selection.Contains((uint)i);

                ImGui.SetNextItemSelectionUserData(i);

                ImGui.Selectable($"##{i}", item_is_selected);
                ImGui.SameLine();

                ImGui.TextColored(
                    _highlightedLines?.Contains(i) == true
                        ? ColorTextHighLight
                        : ImGui.GetStyle().Colors[(int)ImGuiCol.Text], // Default text color
                    line);
            }

            ms_io = ImGui.EndMultiSelect();

            try
            {
                _selection.ApplyRequests(ms_io);
            }
            catch (Exception)
            {
                _selection.Clear();
            }
        }

        ImGui.EndChild();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public  override void OnWindowFocused()
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                var data = _selection.Storage.Data[i];
                sb.AppendLine(Lines[(int)data.Key]);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }

    private void OnHighlightChange()
    {
        if (string.IsNullOrWhiteSpace(HighlightText))
        {
            _highlightedLines = null;
        }
        else
        {
            _highlightedLines = new HashSet<int>();
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Contains(HighlightText, StringComparison.OrdinalIgnoreCase))
                {
                    _highlightedLines.Add(i);
                }
            }
        }
    }
}
