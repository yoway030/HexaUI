namespace ELImGui.Widget;

using System.Text.RegularExpressions;
using Hexa.NET.ImGui;
using ELImGui.Utils;

class FindTextWidget<TData> : BaseWidget
{
    public FindTextWidget(string widgetName, string ownerWindowName) : base(widgetName, ownerWindowName)
    {
    }

    private string _target = String.Empty;
    private bool _onlyFiltered = false;
    private bool _useRegex = false;

    public string Target => _target;
    public bool IsFinding => String.IsNullOrWhiteSpace(_target) == false;
    public bool IsOnlyFiltered => IsFinding && _onlyFiltered == true;
    public Action? FindingTargetChangedFunc = null;

    public List<TData>? FoundedList = null;
    public int FoundedFocusIndex = 0;   // 0은 포커스 없음. 1부터 FoundedList.Count까지
    public Action? FoundedFocusMovedFunc = null;

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        string prevTarget = _target;

        ImGui.Text($"{WidgetName}:");

        ImGuiHelper.SpacingSameLine();
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 15.0f);
        if (ImGui.InputText($"##{WidgetName}#{OwnerWindowName}", ref _target, 100, ImGuiInputTextFlags.EnterReturnsTrue) == true)
        {
            if (_target != prevTarget)
            {
                FindingTargetChange();
            }
        }

        ImGuiHelper.SpacingSameLine();
        if (ImGui.Checkbox($"Regex##{WidgetName}#{OwnerWindowName}", ref _useRegex) == true)
        {
            if (IsFinding == true)
            {
                FindingTargetChange();
            }
        }

        ImGuiHelper.HelpMarkerSameLine("정규표현식 사용");

        if (IsFinding == false)
        {
            // 찾는 중이 아니라면 추가적인 컨트롤은 출력하지 않음.
            return;
        }

        ImGuiHelper.SpacingSameLine();
        if (ImGui.Button("Clear") == true)
        {
            _target = String.Empty;
            FindingTargetChange();
            return;
        }

        ImGuiHelper.SpacingSameLine();
        ImGui.Checkbox($"Filter##{WidgetName}#{OwnerWindowName}", ref _onlyFiltered);
        ImGuiHelper.HelpMarkerSameLine("찾는 문자열과 매칭되것 만 보기");

        ImGui.SameLine();
        if (ImGui.Button($"<##{WidgetName}#{OwnerWindowName}") == true)
        {
            FoundedRowFocusMove(-1);
        }

        ImGui.SameLine(0, 0);
        if (ImGui.Button($">##{WidgetName}#{OwnerWindowName}") == true)
        {
            FoundedRowFocusMove(1);
        }

        ImGuiHelper.HelpMarkerSameLine("매칭되는 문자열간 포커스 이동");

        ImGui.SameLine();
        ImGui.Text($"{FoundedFocusIndex}/{FoundedList?.Count}");
        ImGuiHelper.HelpMarkerSameLine("현재 포커스 / 찾은 수");
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public void FindingTargetChange()
    {
        FoundedList?.Clear();
        FoundedList = null;
        FoundedFocusIndex = 0;
        FindingTargetChangedFunc?.Invoke();
    }

    public void FoundedRowFocusMove(int moveFocusIndex)
    {
        if (FoundedList == null || FoundedList.Count == 0)
        {
            FoundedFocusIndex = 0;
            return;
        }

        int newIndex = FoundedFocusIndex + moveFocusIndex;
        if (newIndex < 1)
        {
            newIndex = FoundedList.Count;
        }
        else if (newIndex > FoundedList.Count)
        {
            newIndex = 1;
        }

        FoundedFocusIndex = newIndex;
        FoundedFocusMovedFunc?.Invoke();
    }

    public bool IsMachted(string text)
    {
        if (!IsFinding)
        {
            return false;
        }

        if (_useRegex)
        {
            try
            {
                return Regex.IsMatch(text, Target, RegexOptions.IgnoreCase);
            }
            catch
            {
                // 정규표현식이 잘못된 경우 매칭 실패로 처리
                return false;
            }
        }
        else
        {
            return text.Contains(Target, StringComparison.OrdinalIgnoreCase);
        }
    }
}

