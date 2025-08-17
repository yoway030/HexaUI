using Hexa.NET.ImGui;
using System.Numerics;
using System.Text;

namespace ELImGui.Window;

public class CommandConsole : BaseWindow
{
    public static readonly Vector4 DefaultColor = new Vector4(1, 1, 1, 1);
    public static readonly Vector4 HelpTextColor = new Vector4(0.8f, 0.9f, 1f, 1f);
    public static readonly Vector4 EchoTextColor = new Vector4(0.7f, 0.9f, 0.7f, 1f);
    public static readonly Vector4 ErrorTextColor = new Vector4(1, 0.4f, 0.4f, 1f);

    public CommandConsole(string windowName = $"{nameof(CommandConsole)}")
        : base(windowName, 0)
    {
        // 기본 명령 등록
        Register("help", _ =>
        {
            AddLog("Available commands:", HelpTextColor);
            foreach (var k in _commands.Keys)
            {
                AddLog($"  {k}");
            }
        });

        Register("clear", _ => Clear());

        Register("echo", args =>
        {
            AddLog(string.Join(' ', args));
        });

        Register("time", _ =>
        {
            AddLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        });
    }

    private readonly Dictionary<string, Action<string[]>> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string text, Vector4 color)> _log = new();
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    private string _commandInput = string.Empty;
    private bool _scrollToBottom = false;

    public void Register(string name, Action<string[]> handler)
    {
        _commands[name] = handler;
    }

    public void AddLog(string text, Vector4? color = null)
    {
        _log.Add((text, color ?? DefaultColor));
        _scrollToBottom = true;
    }

    public void Clear()
    {
        _log.Clear();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        // '`' 입력시 창 오픈
        if (ImGui.IsKeyPressed(ImGuiKey.GraveAccent))
        {
            IsVisible = !IsVisible;
            if (IsVisible)
            {
                _scrollToBottom = true; // 토글 시 스크롤을 맨 아래로 이동

                var viewport = ImGui.GetMainViewport();
                var size = viewport.Size;
                size.Y = Math.Clamp(size.Y, 0, 400);

                SetWindowPosSize(viewport.Pos, size);
            }
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        // 로그 영역
        string childId = $"ConsoleLog##{WindowId}";
        byte[] bytes = Encoding.UTF8.GetBytes(childId);

        // Mark the unsafe block to allow pointer usage
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                ImGui.BeginChild(ptr, new Vector2(0, -ImGui.GetFrameHeightWithSpacing() - 6), ImGuiWindowFlags.HorizontalScrollbar);
            }
        }

        foreach (var (t, c) in _log)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, c);
            ImGui.TextUnformatted(t);
            ImGui.PopStyleColor();
        }

        if (_scrollToBottom)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottom = false;
        }

        ImGui.EndChild();

        // 입력창
        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere(); // 다음 그려질 위젯에 포커스
        }

        ImGui.PushItemWidth(-1);
        string consoleInputLabel = $"##ConsoleInput#{WindowId}";
        ImGui.SetItemDefaultFocus();

        bool pressedEnter = false;
        unsafe
        {
            pressedEnter = ImGui.InputText(consoleInputLabel, ref _commandInput, 1024,
            ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackHistory, Callback);
        }
        
        ImGui.PopItemWidth();

        if (pressedEnter)
        {
            ExecuteCurrentInput();

            // 입력 후 포커스 유지
            ImGui.SetKeyboardFocusHere(-1);
        }
    }

    public unsafe int Callback(ImGuiInputTextCallbackData* data)
    {
        string foundHistory = null!;

        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
        {
            if (_history.Count > 0)
            {
                _historyIndex = (_historyIndex < 0) ? _history.Count - 1 : Math.Max(0, _historyIndex - 1);
                foundHistory = _history[_historyIndex];
            }
        }
        
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
        {
            if (_history.Count > 0)
            {
                if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
                {
                    _historyIndex++;
                    foundHistory = _history[_historyIndex];
                }
                else
                {
                    _historyIndex = -1;
                    foundHistory = string.Empty;
                }
            }
        }

        if (foundHistory != null)
        {
            // 예: 텍스트 전체를 원하는 값으로 교체
            ReadOnlySpan<char> replacement = foundHistory;

            // With the following corrected line:
            byte[] replacementBytes = Encoding.UTF8.GetBytes(replacement.ToArray());
            // 전체 삭제
            data->DeleteChars(0, data->BufTextLen);

            // 삽입 (BufSize를 넘으면 잘립니다)
            fixed (byte* p = replacementBytes)
                data->InsertChars(0, p, p + replacement.Length);

            // 커서 맨 끝으로
            data->CursorPos = data->BufTextLen;

            return 1; // 이벤트 전파 차단
        }

        return 0;
    }

    private void ExecuteCurrentInput()
    {
        var line = _commandInput.Trim();
        if (line.Length == 0)
        {
            return;
        }

        // 로그에 커맨드 에코
        AddLog($"> {line}", EchoTextColor);

        // 히스토리
        if (_history.Count == 0 || _history[^1] != line)
        {
            _history.Add(line);
        }

        _historyIndex = -1;

        // 파싱
        var (cmd, args) = Parse(line);

        // 실행
        if (_commands.TryGetValue(cmd, out var handler))
        {
            try
            {
                handler(args);
            }
            catch (Exception ex)
            {
                AddLog($"[error] {ex.Message}", ErrorTextColor);
            }
        }
        else
        {
            AddLog($"Unknown command: {cmd}", ErrorTextColor);
        }

        _commandInput = string.Empty;
        _scrollToBottom = true;
    }

    private static (string cmd, string[] args) Parse(string line)
    {
        // 간단 파서: 공백 분리, 큰따옴표로 묶인 토큰 지원
        var tokens = new List<string>();
        bool inQuotes = false;
        var cur = new StringBuilder();

        foreach (char ch in line)
        {
            if (ch == '"') 
            {
                inQuotes = !inQuotes; continue;
            }

            if (!inQuotes && char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0) { tokens.Add(cur.ToString()); cur.Clear(); }
            }
            else
            {
                cur.Append(ch);
            }
        }

        if (cur.Length > 0)
        {
            tokens.Add(cur.ToString());
        }

        if (tokens.Count == 0)
        {
            return (string.Empty, Array.Empty<string>());
        }

        var cmd = tokens[0];
        tokens.RemoveAt(0);
        return (cmd, tokens.ToArray());
    }
}
