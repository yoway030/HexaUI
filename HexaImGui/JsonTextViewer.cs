using Hexa.NET.ImGui;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HexaImGui;

public class JsonTextViewer
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
        ImGui.Begin("JSON Text Viewer");
        //DrawJsonTextEditor(jsonText);
        DrawJsonTextEditorWithLineNumber(jsonText);
        ImGui.End();
    }

    public void DrawJsonTextEditor(string jsonText)
    {
        try
        {
            var token = JToken.Parse(jsonText);
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
            token.WriteTo(jsonWriter);

            var lines = sb.ToString().Split('\n');
            ImGui.BeginChild("##JsonViewer");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd(); // 개행 제거
                Vector4 color = GetHighlightColor1(line);
                bool selected = false;

                // 선택 + 복사
                ImGui.TextColored(color, ""); // 색상만 적용 (텍스트는 Selectable에서 이미 표시됨)
                ImGui.SameLine();

                if (ImGui.Selectable($"{line}##{i}", ref selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyDown(ImGuiKey.C))
                        ImGui.SetClipboardText(line);
                }

            }

            ImGui.EndChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"JSON 파싱 오류: {ex.Message}");
        }
    }

    private Vector4 GetHighlightColor1(string line)
    {
        if (line.Contains(":"))
        {
            if (line.Contains("\"") && line.Contains(": \""))
                return new Vector4(1f, 0.7f, 0.3f, 1f); // 문자열
            if (line.Contains("true") || line.Contains("false"))
                return new Vector4(0.3f, 1f, 0.3f, 1f); // bool
            if (line.Contains("null"))
                return new Vector4(1f, 0.3f, 1f, 1f); // null
            if (line.Any(char.IsDigit))
                return new Vector4(0.4f, 0.8f, 1f, 1f); // 숫자 추정
        }

        return new Vector4(1, 1, 1, 1); // 기본 흰색
    }

    public void DrawJsonTextEditorWithLineNumber(string jsonText)
    {
        try
        {
            var token = JToken.Parse(jsonText);
            var linesWithPath = FlattenJsonLines(token, "$");
            var widthDigits = linesWithPath.Count.ToString().Length;

            ImGui.BeginChild("JsonEditor", new Vector2(0, 0), ImGuiWindowFlags.HorizontalScrollbar);

            for (int i = 0; i < linesWithPath.Count; i++)
            {
                var (line, path) = linesWithPath[i];

                // 라인 번호 (왼쪽 고정 폭)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                ImGui.TextUnformatted($"{(i + 1).ToString().PadLeft(widthDigits)}");
                ImGui.PopStyleColor();
                ImGui.SameLine();

                // 라인 내용
                Vector4 color = GetHighlightColor(line);
                bool selected = false;

                if (ImGui.Selectable($"{line}##{i}", ref selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C))
                        ImGui.SetClipboardText(line);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Path: {path}");
            }

            ImGui.EndChild();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"JSON 파싱 오류: {ex.Message}");
        }
    }

    List<(string line, string path)> FlattenJsonLines(JToken token, string path)
    {
        var result = new List<(string, string)>();
        var sb = new StringBuilder();
        var writer = new JsonTextWriter(new StringWriter(sb)) { Formatting = Formatting.Indented };
        token.WriteTo(writer);

        var lines = sb.ToString().Split('\n');

        // JSON 구조를 탐색하며 줄 번호에 경로 대응
        int lineIndex = 0;
        TraverseWithLineTracking(token, path, result, ref lineIndex);
        return result;
    }

    void TraverseWithLineTracking(JToken token, string path, List<(string, string)> result, ref int line)
    {
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                result.Add(($"\"{prop.Name}\":", $"{path}.{prop.Name}"));
                line++;
                TraverseWithLineTracking(prop.Value, $"{path}.{prop.Name}", result, ref line);
            }
        }
        else if (token is JArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                result.Add(($"[{i}]:", $"{path}[{i}]"));
                line++;
                TraverseWithLineTracking(arr[i], $"{path}[{i}]", result, ref line);
            }
        }
        else
        {
            // 단일 값
            string value = token.Type == JTokenType.String
                ? JsonConvert.ToString(token.ToString())
                : token.ToString();
            result.Add((value, path));
            line++;
        }
    }

    Vector4 GetHighlightColor(string line)
    {
        if (line.Contains(":"))
        {
            if (line.Contains("\"") && line.Contains(": \""))
                return new Vector4(1f, 0.7f, 0.3f, 1f); // 문자열
            if (line.Contains("true") || line.Contains("false"))
                return new Vector4(0.3f, 1f, 0.3f, 1f); // bool
            if (line.Contains("null"))
                return new Vector4(1f, 0f, 1f, 1f); // null
            if (line.Any(char.IsDigit))
                return new Vector4(0.4f, 0.8f, 1f, 1f); // 숫자 추정
        }

        return new Vector4(1, 1, 1, 1); // 기본 흰색
    }
}
