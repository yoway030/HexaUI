using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace HexaImGui;

public class JsonViewer
{
    public string jsonText = 
"""
{           
  "name": "John \"Johnny\" Smith",
  "age": 32,
  "email": null,
  "isActive": true,
  "roles": ["admin", "editor", "user"],
  "profile": {
    "address": {
      "street": "123 Main St",
      "city": "New York",
      "zipcode": "10001"
    },
    "phone": "+1-800-555-0199"
  },
  "loginHistory": [
    { "date": "2023-12-01T10:00:00Z", "ip": "192.168.1.1" },
    { "date": "2023-12-05T14:22:13Z", "ip": "192.168.1.23" }
  ]
}
""";

    public void Draw()
    {
        ImGui.Begin("JSON Viewer");
        
        if (ImGui.CollapsingHeader("JSON Viewer"))
        {
            DrawJsonPretty(jsonText);
        }

        ImGui.End();
    }

    private void DrawJsonPretty(string jsonText)
    {
        try
        {
            var token = JToken.Parse(jsonText);
            DrawJsonTokenWithPath(token, "$");
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), $"Invalid JSON: {ex.Message}");
        }
    }

    private void DrawJToken(JToken token, int indent)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var prop in (JObject)token)
                {
                    ImGui.PushID(prop.Key);
                    if (ImGui.TreeNode(prop.Key))
                    {
                        DrawJToken(prop.Value!, indent + 1);
                        ImGui.TreePop();
                    }
                    ImGui.PopID();
                }
                break;

            case JTokenType.Array:
                int i = 0;
                foreach (var item in (JArray)token)
                {
                    ImGui.PushID(i);
                    if (ImGui.TreeNode($"[{i}]"))
                    {
                        DrawJToken(item, indent + 1);
                        ImGui.TreePop();
                    }
                    ImGui.PopID();
                    i++;
                }
                break;

            case JTokenType.String:
                ImGui.TextColored(new Vector4(0.9f, 0.6f, 0.2f, 1f), $"\"{token}\"");
                break;

            case JTokenType.Integer:
            case JTokenType.Float:
                ImGui.TextColored(new Vector4(0.6f, 0.8f, 1f, 1f), token.ToString());
                break;

            case JTokenType.Boolean:
                ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.4f, 1f), token.ToString().ToLower());
                break;

            case JTokenType.Null:
                ImGui.TextColored(new Vector4(1f, 0f, 1f, 1f), "null");
                break;

            default:
                ImGui.Text(token.ToString());
                break;
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
                        ImGui.SetTooltip($"Path: ${childPath}");

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
                        ImGui.SetTooltip($"Path: ${childPath}");

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
                        ImGui.SetClipboardText(display);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Path: ${path}");

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
