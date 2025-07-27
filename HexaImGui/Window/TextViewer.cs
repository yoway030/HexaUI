using Hexa.NET.ImGui;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;
using HexaImGui.Utils;
using Hexa.NET.ImGui.Widgets;

namespace HexaImGui.Window;

public class TextViewer
{
    public TextViewer(string widgetName, string textOrPath, bool isPath)
    {
        WidgetName = widgetName;

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

    public string WidgetName { get; init; } = "TextViewer";
    public int WidgetDepth { get; init; } = 0;
    public string? ErrorText { get; private set; } = null;
    public string Text { get; private set; } = string.Empty;
    public List<string> Lines { get; private set; } = null!;
    public string? Path { get; private set; } = null;
    private ImGuiSelectionBasicStorage _selection = new();


    public void Draw()
    {
        ImGui.Begin($"{WidgetName}#{WidgetDepth}");
        DrawChild();
        ImGui.End();
    }

    private int? _selectStartLine = null;
    private int? _selectEndLine = null;
    private int? _mouseDragStartLine = null;

    private int? _mouseSelectStartLine = null;
    private int? _mouseSelectEndLine = null;
    private Vector2? _dragStartPos = null;
    private Vector2? _dragEndPos = null;

    private void DrawChild()
    {
        ImGui.BeginChild($"{WidgetName}Child#{WidgetDepth}",ImGuiWindowFlags.NoMove);

        if (ErrorText != null)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error : {ErrorText}");
            ImGui.EndChild();
            return;
        }


        var drawList = ImGui.GetWindowDrawList();
        var io = ImGui.GetIO();
        bool isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);

        // 마우스 드래그 시작 감지
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            _dragStartPos = null;
        }
        else if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            _dragStartPos = io.MousePos;
        }
        else if (isDragging == true)
        {
            _dragEndPos = io.MousePos;
        }

        if (Lines.Any())
        {
            int lineCount = Lines.Count;
            ImGui.Text($"Path: {Path ?? "null"}");
            ImGuiHelper.SpacingSameLine();
            ImGui.Text($"Lines: {lineCount}");
            ImGui.SeparatorText("Text");

            for (int i = 0; i < Lines.Count; i++)
            {
                string line = Lines[i];

                // 라인 위치 정보 저장
                Vector2 lineStart = ImGui.GetCursorScreenPos();
                ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight())); // 공간 확보
                Vector2 lineEnd = ImGui.GetCursorScreenPos();
                drawList.AddRectFilled(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.Button));

                // 선택 범위 → 배경 박스 그리기
                if (IsLineInMouseSelection(i))
                {
                    drawList.AddRectFilled(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.FrameBgActive));
                }

                // 라인 출력 (번호 + 텍스트)
                ImGui.SetCursorScreenPos(lineStart);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                ImGui.TextUnformatted($"{i + 1,3}");
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.TextUnformatted(line);
            }

            ImGui.TextUnformatted($"{isDragging}, {_dragStartPos}, {_dragEndPos}");
            if (isDragging && _dragStartPos.HasValue && _dragEndPos.HasValue)
            {
                // 드래그 중인 경우 → 드래그 박스 그리기
                Vector2 dragMin = Vector2.Min(_dragStartPos.Value, _dragEndPos.Value);
                Vector2 dragMax = Vector2.Max(_dragStartPos.Value, _dragEndPos.Value);
                drawList.AddRectFilled(dragMin, dragMax, ImGui.GetColorU32(ImGuiCol.Border));
            }

            // Ctrl+C 복사
            if (_selectStartLine.HasValue && _selectEndLine.HasValue &&
                io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C))
            {
                int from = Math.Min(_selectStartLine.Value, _selectEndLine.Value);
                int to = Math.Max(_selectStartLine.Value, _selectEndLine.Value);
                var selectedLines = Lines.Skip(from).Take(to - from + 1);
                ImGui.SetClipboardText(string.Join("\n", selectedLines));
            }
        }
        else
        {
            ImGui.Text("No text to display.");
        }

        ImGui.EndChild();

        if (ImGui.TreeNode("Multi-Select"))
        {
            ImGui.Text("Supported features:");
            ImGui.BulletText("Keyboard navigation (arrows, page up/down, home/end, space).");
            ImGui.BulletText("Ctrl modifier to preserve and toggle selection.");
            ImGui.BulletText("Shift modifier for range selection.");
            ImGui.BulletText("CTRL+A to select all.");
            ImGui.BulletText("Escape to clear selection.");
            ImGui.BulletText("Click and drag to box-select.");
            ImGui.Text("Tip: Use 'Demo->Tools->Debug Log->Selection' to see selection requests as they happen.");

            // Use default selection.Adapter: Pass index to SetNextItemSelectionUserData(), store index in Selection
            const int ITEMS_COUNT = 50;
            ImGui.Text($"Selection: {_selection.Size}/{ITEMS_COUNT}");

            // The BeginChild() has no purpose for selection logic, other that offering a scrolling region.
            if (ImGui.BeginChild("##Basket", new Vector2(-float.Epsilon, ImGui.GetFontSize() * 20), ImGuiChildFlags.ResizeY))
            {
                // "-float.Epsilon" 코드 설명.
                // C++ 코드에서는 -FLT_MIN 을 사용했지만, C#에서는 -float.Epsilon을 사용하여 최소 크기를 설정합니다.
                // 왜 - 가 붙는지는 의문. 안붙이면 정상동작하지 않음.
                // 0을 써도 동작엔 문제가 없지만 기존 C++ 코드와 일관성을 유지하기 위해 -float.Epsilon 사용

                ImGuiMultiSelectIOPtr ms_io = ImGui.BeginMultiSelect(
                    ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                    _selection.Size,
                    ITEMS_COUNT);

                ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                    (storage, index) =>
                    {
                        return (uint)index;
                    });
                _selection.ApplyRequests(ms_io);

                for (int n = 0; n < ITEMS_COUNT; n++)
                {
                    string label = $"##{n}";
                    bool item_is_selected = _selection.Contains((uint)n);
                    ImGui.SetNextItemSelectionUserData(n);
                    ImGui.Selectable(label, item_is_selected);
                    ImGui.SameLine();
                    ImGui.TextUnformatted("asdasfasdasda");
                }

                ms_io = ImGui.EndMultiSelect();
                _selection.ApplyRequests(ms_io);
            }
            ImGui.EndChild();
            ImGui.TreePop();
        }
    }

    private bool IsLineSelected(int i)
    {
        if (_selectStartLine.HasValue && _selectEndLine.HasValue)
        {
            int from = Math.Min(_selectStartLine.Value, _selectEndLine.Value);
            int to = Math.Max(_selectStartLine.Value, _selectEndLine.Value);
            return i >= from && i <= to;
        }
        return false;
    }

    private bool IsLineInMouseSelection(int lineIndex)
    {
        if (_mouseSelectStartLine.HasValue && _mouseSelectEndLine.HasValue)
        {
            int min = Math.Min(_mouseSelectStartLine.Value, _mouseSelectEndLine.Value);
            int max = Math.Max(_mouseSelectStartLine.Value, _mouseSelectEndLine.Value);
            return lineIndex >= min && lineIndex <= max;
        }
        return false;
    }

    private bool IsRectOverlapping(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
    {
        return !(aMax.Y < bMin.Y || aMin.Y > bMax.Y); // 세로 방향만 체크 (줄 기준)
    }
}
