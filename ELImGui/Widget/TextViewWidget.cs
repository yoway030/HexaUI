namespace ELImGui.Widget;

using ELImGui.Utils;
using ELImGui.Window;
using Hexa.NET.ImGui;
using System.Data;
using System.IO;
using System.Numerics;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

public class TextViewWidget : BaseWidget
{
    public TextViewWidget(string widgetName, string ownerWindowName, bool useHeader = false, bool useLineNumber = false)
        : base(widgetName, ownerWindowName)
    {
        if (useHeader == true)
        {
            _findWidget = new("Find", OwnerWindowName);
            _findWidget.FindingTargetChangedFunc += OnFindingTargetChanged;
            _findWidget.FoundedFocusMovedFunc += OnFoundedFocusMoved;
        }

        _useLineNumber = useLineNumber;
    }

    private ImGuiSelectionBasicStorage _selection = new();

    private FindTextWidget<IndexedRow<string>>? _findWidget;
    private IndexedRow<string>? _focusedRow = null;
    private bool _focusMove = false;

    private bool _useLineNumber = false;

    public string? ErrorText { get; private set; } = null;
    public string Text { get; private set; } = String.Empty;
    public List<string> Lines { get; private set; } = null!;
    public string? Path { get; private set; } = null;

    public void Initialize(string value, bool isPath)
    {
        if (isPath == true)
        {
            Path = value;

            if (String.IsNullOrWhiteSpace(Path))
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

                if (String.IsNullOrWhiteSpace(Text))
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
            Text = value;
        }

        Lines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        int lineCount = Lines.Count;

        // header
        if (_findWidget != null)
        {
            ImGui.Text($"FromFile: {Path ?? "null"}");
            ImGuiHelper.SpacingSameLine();
            ImGui.Text($"Lines: {lineCount}");

            _findWidget.RenderImObject(utcNow, deltaSec, imInternalContext);
        }

        // body
        if (ImGui.BeginChild("Body") == true)
        {
            if (ErrorText != null)
            {
                ImGui.TextColored(ImGuiColorHelper.TextError, $"Error : {ErrorText}");
            }
            else if (lineCount == 0)
            {
                ImGui.Text("No text to display.");
            }
            else
            {
                var ms_io = ImGui.BeginMultiSelect(
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
                    var colorEffect = Vector4.Zero;
                    string line = Lines[i];
                    bool isLineSelected = _selection.Contains((uint)i);
                    colorEffect = (_findWidget?.IsMachted(line) ?? false)
                        ? ImGuiColorHelper.AlphaBlendClamped(ImGuiTheme.Values.Focus, 0.8f)
                        : Vector4.Zero;
                    colorEffect = _focusedRow?.Index == i ? ImGuiTheme.Values.Focus : colorEffect;

                    // 색이 설정된 경우 배경색상 출력
                    if (colorEffect != Vector4.Zero)
                    {
                        Vector2 size = ImGui.GetWindowSize();
                        var drawList = ImGui.GetWindowDrawList();

                        Vector2 pos = ImGui.GetCursorScreenPos();
                        size.Y = ImGui.GetTextLineHeight();
                        uint bgColor = ImGui.ColorConvertFloat4ToU32(colorEffect);

                        drawList.AddRectFilled(pos, pos + size, bgColor, 2.0f);
                    }

                    ImGui.SetNextItemSelectionUserData(i);
                    ImGui.Selectable($"##{i}", isLineSelected);
                    ImGui.SameLine();

                    if (_useLineNumber)
                    {
                        ImGui.TextColored(ImGuiColorHelper.StyleColor(ImGuiCol.TextDisabled), $"{i + 1,3}: ");
                        ImGui.SameLine();
                    }

                    ImGui.TextUnformatted(line);
                }

                // 포커스된 행이 있으면 해당 행이 보이도록 스크롤 조정
                if (_focusMove == true)
                {
                    int index = (int)(_focusedRow?.Index ?? 0);
                    float posY = index * ImGui.GetTextLineHeightWithSpacing();
                    ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + posY, 0.5f);

                    _focusMove = false;
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
    }

    public override void OnWindowFocused(BaseWindow baseWindow)
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            var sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                var data = _selection.Storage.Data[i];
                sb.AppendLine(Lines[(int)data.Key]);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }

    private void OnFindingTargetChanged()
    {
        _focusedRow = null;

        for (int i = 0; i < Lines.Count; i++)
        {
            string line = Lines[i];
            if (_findWidget?.IsMachted(line) == true)
            {
                _findWidget?.FoundedList.Add(new IndexedRow<string>((uint)i, line));
            }
        }
    }

    private void OnFoundedFocusMoved()
    {
        if (_findWidget?.IsFinding == false)
        {
            return;
        }

        _selection.Clear();

        if (_findWidget!.TryGetFocusedData(out var focusedData) == false)
        {
            return;
        }

        _focusedRow = focusedData;
        _focusMove = true;
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
    }
}
