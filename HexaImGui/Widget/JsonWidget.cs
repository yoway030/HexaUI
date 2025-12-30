namespace ELImGui.Widget;

using ELImGui.Utils;
using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Numerics;

public class JsonWidget : BaseWidget
{
    public JsonWidget(string widgetName, string ownerWindowName) : base(widgetName, ownerWindowName)
    {
    }

    private string? _exception = null;
    private string _jsonText = String.Empty;
    private bool _jsonChanged = false;

    public string JsonText
    {
        get => _jsonText;
        set
        {
            if (_jsonText != value)
            {
                _jsonText = value;
                _jsonChanged = true;
            }
        }
    }

    public JToken? ParsedJson { get; private set; } = null;

    public void Initialize(string value, bool isPath)
    {
        JsonText = String.Empty;

        if (isPath == true)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                _exception = "파일 경로가 null이거나 비어 있습니다.";
            }

            try
            {
                if (!File.Exists(value))
                {
                    _exception = "지정된 파일을 찾을 수 없습니다.";
                }

                JsonText = File.ReadAllText(value);

                if (String.IsNullOrWhiteSpace(JsonText))
                {
                    _exception = "파일 내용이 비어 있습니다.";
                }
            }

            catch (IOException ex)
            {
                _exception = $"파일 입출력 오류: {ex.Message}";
            }
            catch (Exception ex)
            {
                _exception = $"알 수 없는 오류: {ex.Message}";
            }
        }
        else
        {
            JsonText = value;
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        RenderImpl();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        UpdateImpl();
    }

    public void RenderImpl()
    {
        if (_exception != null)
        {
            ImGui.TextColored(ImGuiColorHelper.TextError, _exception);
            ImGui.Separator();
            ImGui.TextUnformatted(JsonText);
            return;
        }
        else if (ParsedJson == null)
        {
            ImGui.Text("No JSON data.");
            return;
        }

        using var child = new ImGuiScopedId(WidgetName);
        DrawJsonTokenWithPath(ParsedJson, "$");
    }

    public void UpdateImpl()
    {
        if (_jsonChanged == true)
        {
            try
            {
                ParsedJson = JToken.Parse(_jsonText);
                _exception = null;
            }
            catch (Exception e)
            {
                ParsedJson = null;
                _exception = "Invalid JSON format : " + e.Message;
            }

            _jsonChanged = false;
        }
    }

    void DrawJsonTokenWithPath(JToken token, string path)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var prop in (JObject)token)
                {
                    string childPath = path + "." + prop.Key;
                    using var child = new ImGuiScopedId(childPath);

                    if (ImGui.TreeNodeEx(prop.Key, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        var valueType = prop.Value!.Type;
                        if (valueType is not JTokenType.Object and
                            not JTokenType.Array)
                        {
                            ImGui.SameLine();
                            ImGui.TextUnformatted(":");
                            ImGui.SameLine();
                        }

                        DrawJsonTokenWithPath(prop.Value!, childPath);
                        ImGui.TreePop();
                    }
                }

                break;

            case JTokenType.Array:
                var array = (JArray)token;
                for (int i = 0; i < array.Count; i++)
                {
                    var color = GetColorForToken(token.Type);
                    string childPath = $"{path}[{i}]";

                    using var child = new ImGuiScopedId(childPath);
                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    ImGui.TextUnformatted($"[{i}]");
                    ImGui.PopStyleColor();

                    var valueType = array[i].Type;
                    if (valueType is not JTokenType.Object and
                        not JTokenType.Array)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted(":");
                        ImGui.SameLine();
                    }

                    DrawJsonTokenWithPath(array[i], childPath);
                }

                break;

            default:
            {
                string childPath = $"{path}.value";
                string display = GetValueString(token);
                var color = GetColorForToken(token.Type);

                using var child = new ImGuiScopedId(childPath);
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                if (ImGui.Selectable(display, false))
                {
                    // Ctrl+C 눌렸으면 복사
                    if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyDown(ImGuiKey.C))
                    {
                        ImGui.SetClipboardText(display);
                    }
                }

                ImGui.PopStyleColor();
                //ImGui.SameLine();
                //ImGui.TextUnformatted($"({token.Type})");
            }

            break;
        }
    }

    string GetValueString(JToken token)
    {
        if (token.Type == JTokenType.Null)
        {
            return "(null)";
        }
        else if (token.Type == JTokenType.String)
        {
            return JsonConvert.ToString(token.ToString());
        }

        return token.ToString() ?? String.Empty;
    }

    Vector4 GetColorForToken(JTokenType type) => type switch
    {
        JTokenType.String => ImGuiColorHelper.TextString,
        JTokenType.Integer or JTokenType.Float => ImGuiColorHelper.TextNumber,
        JTokenType.Boolean => ImGuiColorHelper.TextBool,
        JTokenType.Null => ImGuiColorHelper.TextNull,
        JTokenType.Date => ImGuiColorHelper.TextDate,
        JTokenType.Array => ImGuiColorHelper.TextGray,
        _ => ImGuiColorHelper.TextNoraml
    };
}
