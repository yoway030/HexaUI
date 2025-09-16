namespace ELImGui.Widget;

using System.Numerics;
using Hexa.NET.ImGui;
using ELImGui.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class JsonWidget : BaseWidget
{
    public static readonly Vector4 HighLightColor = new(0.0f, 1.0f, 0.0f, 0.5f);

    public JsonWidget()
    {
        InitializeName(widgetName: $"{nameof(JsonWidget)}", parentWindowName: String.Empty);
    }

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
        if (_exception != null)
        {
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), _exception);
            return;
        }
        else if (ParsedJson == null)
        {
            ImGui.Text("No JSON data.");
            return;
        }

        DrawJsonTokenWithPath(ParsedJson, "$");
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
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
                _jsonText = String.Empty;
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

                    var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed;
                    bool open = ImGui.TreeNodeEx(prop.Key, flags);

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Path: ${childPath}");
                    }

                    if (open)
                    {
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
                    string childPath = $"{path}[{i}]";
                    ImGui.PushID(childPath);

                    bool open = ImGui.TreeNodeEx($"[{i}]", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed);

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Path: ${childPath}");
                    }

                    if (open)
                    {
                        DrawJsonTokenWithPath(array[i], childPath);
                        ImGui.TreePop();
                    }

                    ImGui.PopID();
                }

                break;

            default:
                string display = token.Type == JTokenType.String
                    ? JsonConvert.ToString(token.ToString()) // escape 포함 문자열
                    : token.ToString();

                var color = GetColorForToken(token.Type);

                if (ImGui.Selectable(display, false))
                {
                    // Ctrl+C 눌렸으면 복사
                    if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyDown(ImGuiKey.C))
                    {
                        ImGui.SetClipboardText(display);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Path: ${path}");
                }

                ImGui.SameLine();
                ImGui.TextColored(color, $" ({token.Type})");

                break;
        }
    }

    Vector4 GetColorForToken(JTokenType type) => type switch
    {
        JTokenType.String => new Vector4(1f, 0.7f, 0.2f, 1f),
        JTokenType.Integer or JTokenType.Float => new Vector4(0.4f, 0.8f, 1f, 1f),
        JTokenType.Boolean => new Vector4(0.3f, 1f, 0.3f, 1f),
        JTokenType.Null => new Vector4(1f, 0f, 1f, 1f),
        _ => new Vector4(1f, 1f, 1f, 1f)
    };
}
