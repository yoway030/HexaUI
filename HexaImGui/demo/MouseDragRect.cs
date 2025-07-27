using Hexa.NET.ImGui;
using System.Numerics;

namespace HexaImGui.demo;

public class MouseDragRect
{
    public MouseDragRect()
    {
    }

    private Vector2? _dragStartPos = null;
    private Vector2? _dragEndPos = null;

    public void Draw()
    {
        ImGui.BeginChild($"MouseDragRectDemo", ImGuiWindowFlags.NoMove);

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

        for (int i = 0; i < 50; i++)
        {
            string line = $"Sample text line {i + 1}";
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

        ImGui.EndChild();
    }

}
