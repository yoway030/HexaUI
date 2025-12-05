namespace ELImGui.Widget;

using ELImGui.Window;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.Mathematics;
using System;
using System.Numerics;

public unsafe class Space3dWidget : BaseWidget
{
    private record struct CubeInfo(
        Vector3 Position,
        float Scale);

    private record struct LineInfo(
        Vector3 Start,
        Vector3 End,
        uint Color);

    private record struct TextInfo(
        Vector3 Position,
        string Text,
        uint Color);

    private CameraTransform _camera = new();
    private Vector3 _cameraOffset = Vector3.Zero;
    private Vector3 _sc = new(10, 0.5f, 0.5f);

    private const float _speed = 2;

    private Viewport _sourceViewport = new(1920, 1080);
    private Viewport _viewport;
    private List<CubeInfo> _cubeInfos = [];
    private List<LineInfo> _lineInfos = [];
    private List<TextInfo> _textInfos = [];

    private float _gridSize = 1.0f;
    private Action? _onRender3dWidget = null;

    public Space3dWidget(string widgetName, string ownerWindowName, float gridSize = 1.0f, Action? onRender3dWidget = null)
        : base(widgetName, ownerWindowName)
    {
        _gridSize = gridSize;
        _onRender3dWidget = onRender3dWidget;
        UpdateCameraOrbit(Vector2.Zero, 0);
    }

    public void ResetCamera()
    {
        _cameraOffset = Vector3.Zero;
        _sc = new(10, 0.5f, 0.5f);
        UpdateCameraOrbit(Vector2.Zero, 0);
    }

    public void ClearData()
    {
        lock (_cubeInfos)
        {
            _cubeInfos.Clear();
        }

        lock (_lineInfos)
        {
            _lineInfos.Clear();
        }

        lock (_textInfos)
        {
            _textInfos.Clear();
        }
    }

    private Vector3 ConvertPos(Vector3 pos)
    {
        var convertedPos = pos / _gridSize;
        convertedPos.X = -convertedPos.X;
        float z = convertedPos.Z;
        float y = convertedPos.Y;
        convertedPos.Z = y;
        convertedPos.Y = z;
        return convertedPos;
    }

    public void AddCubeInfo(Vector3 pos, float scale)
    {
        var convertedPos = ConvertPos(pos);
        lock (_cubeInfos)
        {
            _cubeInfos.Add(new CubeInfo(convertedPos, scale));
        }
    }

    public void AddWorldPosText(Vector3 pos, string text, uint color)
    {
        var convertedPos = ConvertPos(pos);
        lock (_textInfos)
        {
            _textInfos.Add(new TextInfo(convertedPos, text, color));
        }
    }

    public void AddLine(Vector3 start, Vector3 end, uint color)
    {
        lock (_lineInfos)
        {
            _lineInfos.Add(new LineInfo(ConvertPos(start), ConvertPos(end), color));
        }
    }

    public override unsafe void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        _onRender3dWidget?.Invoke();

        HandleInput();

        var widgetPos = ImGui.GetCursorScreenPos(); // 현재 위젯의 시작 위치 (화면 절대좌표)
        var widgetSize = ImGui.GetContentRegionAvail(); // 위젯 내부 사용 가능한 영역

        float ratioX = widgetSize.X / _sourceViewport.Width;
        float ratioY = widgetSize.Y / _sourceViewport.Height;
        float s = Math.Min(ratioX, ratioY);
        float w = _sourceViewport.Width * s;
        float h = _sourceViewport.Height * s;
        float x = widgetPos.X + ((widgetSize.X - w) / 2);
        float y = widgetPos.Y + ((widgetSize.Y - h) / 2);

        _viewport = new Viewport(x, y, w, h);

        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(true);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetRect(x, y, w, h);

        var view = _camera.View;
        var proj = _camera.Projection;

        var matrix = Matrix4x4.Identity;
        ImGuizmo.DrawGrid(ref view, ref proj, ref matrix, 50);

        DrawCubeInfos();
        DrawLineInfos();
        DrawTextInfos();
    }

    private void DrawCubeInfos()
    {
        CubeInfo[] cubeInfos;
        lock (_cubeInfos)
        {
            cubeInfos = _cubeInfos.ToArray();
        }

        if (cubeInfos.Length == 0)
        {
            return;
        }

        var rotation = Quaternion.CreateFromYawPitchRoll(0, 0.0f, 0);

        var transforms = new Matrix4x4[cubeInfos.Length];
        for (int i = 0; i < cubeInfos.Length; i++)
        {
            var cubeInfo = cubeInfos[i];
            var scale = new Vector3(cubeInfo.Scale, cubeInfo.Scale, cubeInfo.Scale);

            var transform =
                Matrix4x4.CreateScale(scale) *
                Matrix4x4.CreateFromQuaternion(rotation) *
                Matrix4x4.CreateTranslation(cubeInfo.Position);

            transforms[i] = transform;
        }

        var view = _camera.View;
        var proj = _camera.Projection;

        ImGuizmo.DrawCubes(ref view, ref proj, transforms, transforms.Length);
    }

    private void DrawLineInfos()
    {
        LineInfo[] lineInfos;
        lock (_lineInfos)
        {
            lineInfos = _lineInfos.ToArray();
        }

        if (lineInfos.Length == 0)
        {
            return;
        }

        var view = _camera.View;
        var proj = _camera.Projection;

        foreach (var lineInfo in lineInfos)
        {
            Draw3DLine(lineInfo.Start, lineInfo.End, view, proj, _viewport, lineInfo.Color);
        }
    }

    private void Draw3DLine(Vector3 start, Vector3 end, Matrix4x4 view, Matrix4x4 proj, Viewport viewport, uint color, float thickness = 1.0f)
    {
        if (!WorldToScreen(start, view, proj, viewport, out var p1))
        {
            return;
        }

        if (!WorldToScreen(end, view, proj, viewport, out var p2))
        {
            return;
        }

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddLine(p1, p2, color, thickness);
    }

    private void DrawTextInfos()
    {
        TextInfo[] textInfos;
        lock (_lineInfos)
        {
            textInfos = _textInfos.ToArray();
        }

        if (textInfos.Length == 0)
        {
            return;
        }

        var view = _camera.View;
        var proj = _camera.Projection;
        var drawList = ImGui.GetWindowDrawList();

        foreach (var textInfo in textInfos)
        {
            if (WorldToScreen(textInfo.Position, view, proj, _viewport, out var screenPos))
            {
                var size = ImGui.CalcTextSize(textInfo.Text);
                var pos = new Vector2(screenPos.X - (size.X * 0.5f), screenPos.Y - (size.Y * 0.5f) - 15.0f);
                drawList.AddText(pos, textInfo.Color, textInfo.Text);
            }
        }
    }

    private bool WorldToScreen(Vector3 worldPos, Matrix4x4 view, Matrix4x4 proj, Viewport viewport, out Vector2 screenPos)
    {
        screenPos = Vector2.Zero;

        var viewProj = Matrix4x4.Multiply(view, proj);
        var clip = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProj);
        if (clip.W <= 0.0f)
        {
            return false;
        }

        var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;

        float x = viewport.X + ((1 + ndc.X) * 0.5f * viewport.Width);
        float y = viewport.Y + ((1 - ndc.Y) * 0.5f * viewport.Height);
        screenPos = new Vector2(x, y);

        return true;
    }

    private void HandleInput()
    {
        if (ImGui.IsWindowHovered())
        {
            bool mouseRightPressed = ImGui.IsMouseDown(ImGuiMouseButton.Right);
            bool mouseMiddlePressed = ImGui.IsMouseDown(ImGuiMouseButton.Middle);

            var delta = Vector2.Zero;
            var positionDelta = Vector2.Zero;
            if (mouseRightPressed)
            {
                delta = ImGui.GetIO().MouseDelta;
            }

            if (mouseMiddlePressed)
            {
                positionDelta = ImGui.GetIO().MouseDelta;
            }

            if (ImGui.IsKeyDown(ImGuiKey.W))
            {
                positionDelta.Y = 1.0f;
            }

            if (ImGui.IsKeyDown(ImGuiKey.S))
            {
                positionDelta.Y = -1.0f;
            }

            if (ImGui.IsKeyDown(ImGuiKey.A))
            {
                positionDelta.X = 1.0f;
            }

            if (ImGui.IsKeyDown(ImGuiKey.D))
            {
                positionDelta.X = -1.0f;
            }

            float wheel = ImGui.GetIO().MouseWheel;

            if (delta.X != 0f || delta.Y != 0f || wheel != 0f)
            {
                UpdateCameraOrbit(delta, wheel);
            }

            if (positionDelta.X != 0.0f || positionDelta.Y != 0.0f)
            {
                UpdateCameraPan(positionDelta);
            }
        }
    }

    private void UpdateCameraOrbit(Vector2 delta, float wheel)
    {
        _sc.X += -wheel;
        _sc.X = MathF.Max(1.0f, _sc.X);
        _sc.Y += -delta.X * 0.004f * _speed;
        _sc.Z = Math.Clamp(_sc.Z + (delta.Y * 0.004f * _speed), -MathF.PI / 2, MathF.PI / 2);

        var pos = SphereHelper.GetCartesianCoordinates(_sc);
        var orientation = Quaternion.CreateFromYawPitchRoll(-_sc.Y, _sc.Z, 0);
        _camera.PositionRotation = (pos, orientation);
        _camera.Position += _cameraOffset;
        _camera.Recalculate();
    }

    private void UpdateCameraPan(Vector2 delta)
    {
        // 현재 카메라 방향 벡터 계산
        var view = _camera.View;
        Matrix4x4.Invert(view, out var invView);

        // 카메라 기준 벡터 추출
        Vector3 right = new(invView.M11, invView.M12, invView.M13);
        Vector3 forward = new(invView.M31, invView.M32, invView.M33)
        {
            // Y축(상하) 성분 제거 — 즉, 수평면(XZ 평면) 이동만 남기기
            Y = 0
        };
        forward = Vector3.Normalize(forward);
        right.Y = 0;
        right = Vector3.Normalize(right);

        float panSpeed = 0.05f * _sc.X; // 거리 비례 이동 속도

        // delta.Y는 Z방향(앞/뒤), delta.X는 X방향(좌/우)
        var offset = ((-right * delta.X) + (forward * delta.Y)) * panSpeed;

        _cameraOffset += offset;
        UpdateCameraOrbit(Vector2.Zero, 0.0f);
    }

    public override void OnWindowFocused(BaseWindow baseWindow) { }

    public override void OnUpdate(DateTime utcNow, double deltaSec) { }
}