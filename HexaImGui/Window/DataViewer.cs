using Hexa.NET.ImGui;
using HexaGen.Runtime;
using HexaImGui.Utils;
using System.Collections.Concurrent;
using System.Text;

namespace HexaImGui.Window;

public class DataViewer<TData> : BaseWindow
    where TData : ViewableData, new()
{
    public DataViewer(string windowName = $"{nameof(DataViewer<TData>)}")
        :base(windowName, 0)
    {
        _dataIdx = -1;
    }

    public int MaxLocalStorage { get; init; }
    public ConcurrentDictionary<int, TData> DataQueue = new();
    private int _dataIdx;
    private ImGuiSelectionBasicStorage _selection = new();

    public string FilterText = string.Empty;

    public override void OnRender()
    {
        int dataCount = DataQueue.Count;

        // Selection info
        ImGui.Text($"Select:{_selection.Size}/{DataQueue.Count}");
        ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");
        ImGuiHelper.SpacingSameLine();

        // filter input
        ImGui.Text("Filter:");
        ImGuiHelper.SpacingSameLine();

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20.0f);
        ImGui.InputText($"##Filter{WindowId}", ref FilterText, 100, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiHelper.SpacingSameLine();

        ImGuiHelper.HelpMarkerSameLine("엔터키로 필터링 적용");

        if (dataCount == 0)
        {
            ImGui.Text("No data available.");
            ImGui.End();
            return;
        }

        var initData = DataQueue.First().Value;

        if (ImGui.BeginTable("Datas", initData.GetColumnSetupActions().Count() + 1, ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            // 선택기능을 위한 첫번째 컬럼
            ImGui.TableSetupColumn($"##Idx{WindowId}", ImGuiTableColumnFlags.WidthFixed, 0);

            // 데이터 출력하는 컬럼
            foreach (var action in initData.GetColumnSetupActions())
            {
                action();
            }

            ImGui.TableHeadersRow();

            // 멀티셀렉트 처리
            ImGuiMultiSelectIOPtr ms_io = ImGui.BeginMultiSelect(
                ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                _selection.Size,
                DataQueue.Count);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= dataCount)
                    {
                        return unchecked((uint)-1);
                    }
                    return (uint)index;
                });
            _selection.ApplyRequests(ms_io);

            // 대량 데이터를 위한 클리퍼
            ImGuiListClipper clipper = new();
            clipper.Begin(dataCount);
            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            while (clipper.Step())
            {
                // 클리핑 처리
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    TData data = DataQueue[displayIndex];
                    var fieldsToString = data.FieldsToString;

                    // row시작
                    ImGui.TableNextRow();

                    // 필터링된 데이터 백그라운드 강조 표시
                    if (string.IsNullOrWhiteSpace(FilterText) == false &&
                        fieldsToString.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // ImGui.TableSetBgColor 는 ImGui.TableNextRow 이후 호출 필요
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 0.5f)));
                    }

                    ImGui.TableNextColumn();
                    {
                        // 선택기능을 위한 첫번째 컬럼
                        bool item_is_selected = _selection.Contains((uint)displayIndex);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable($"##{displayIndex}#{WindowId}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    }

                    // 데이터 필드 출력
                    foreach (var drawAction in data.GetFieldDrawActions())
                    {
                        ImGui.TableNextColumn();
                        drawAction();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        data.TooltipDraw();
                    }
                }
            }

            ms_io = ImGui.EndMultiSelect();
            _selection.ApplyRequests(ms_io);

            ImGui.EndTable();
        }
    }

    public override void OnUpdate()
    {
    }

    public void PushData(TData data)
    {
        int idx = Interlocked.Increment(ref _dataIdx);
        DataQueue.TryAdd(idx, data);
    }

    public override void OnWindowFocused()
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                var dataIndex = _selection.Storage.Data[i].Key;

                sb.AppendLine(DataQueue[(int)dataIndex].FieldsToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}