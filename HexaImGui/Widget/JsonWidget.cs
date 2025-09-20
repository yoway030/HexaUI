namespace ELImGui.Widget;

using System.Numerics;
using Hexa.NET.ImGui;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

public class JsonWidget : BaseWidget
{
    public static readonly Vector4 HighLightColor = new(0.0f, 1.0f, 0.0f, 0.5f);

    public JsonWidget(string widgetName, string parentWindowName) : base(widgetName, parentWindowName)
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

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        RenderImpl();
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        UpdateImpl();
    }

    public void RenderImpl()
    {
        if (_exception != null)
        {
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), _exception);
            ImGui.Separator();
            ImGui.TextUnformatted(JsonText);
            return;
        }
        else if (ParsedJson == null)
        {
            ImGui.Text("No JSON data.");
            return;
        }

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

                    ImGui.PushID(childPath);
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
                    ImGui.PopID();
                }

                break;

            case JTokenType.Array:
                var array = (JArray)token;
                for (int i = 0; i < array.Count; i++)
                {
                    var color = GetColorForToken(token.Type);
                    string childPath = $"{path}[{i}]";

                    ImGui.PushID(childPath);
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
                    ImGui.PopID();
                }

                break;

            default:
            {
                string childPath = $"{path}.value";
                string display = GetValueString(token);
                var color = GetColorForToken(token.Type);

                ImGui.PushID(childPath);
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
                ImGui.PopID();
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
        JTokenType.String => new Vector4(0.4f, 0.4f, 1f, 1f),
        JTokenType.Integer or JTokenType.Float => new Vector4(0.4f, 1f, 0.4f, 1f),
        JTokenType.Boolean => new Vector4(0.4f, 1f, 0.4f, 1f),
        JTokenType.Null => new Vector4(1f, 0f, 0f, 1f),
        JTokenType.Date => new Vector4(1f, 0.7f, 0.2f, 1f),
        JTokenType.Array => new Vector4(1f, 1f, 1f, 0.5f),
        _ => new Vector4(1f, 1f, 1f, 1f)
    };
}
