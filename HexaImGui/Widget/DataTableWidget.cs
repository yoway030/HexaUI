namespace ELImGui.Window;

using ELImGui.Utils;
using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Numerics;
using System.Text;

public class DataTableWidget<TData> : BaseWidget
{
    public DataTableWidget(DataTableRule<TData> rule, string widgetName)
        : this(rule, widgetName, String.Empty)
    {
    }

    public DataTableWidget(DataTableRule<TData> rule, string widgetName, string ownerWindowName, int maxLocalStorage = 10_000, int windowDepth = 0)
        : base(widgetName, ownerWindowName)
    {
        Rule = rule;
        MaxLocalStorage = maxLocalStorage;
        DataIdx = 1;
        ShouldScrollToEnd = ShouldScrollToEndInternal;

        _findWidget = new("Find", OwnerWindowName);
        _findWidget.FindingTargetChangedFunc += OnFindingTargetChanged;
        _findWidget.FoundedFocusMovedFunc += OnFoundedFocusMoved;
    }

    private List<IndexedRow<TData>> _localStorage = new();
    private List<IndexedRow<TData>> _showStorage = null!;

    private ImGuiSelectionBasicStorage _selection = new();
    private FindTextWidget<IndexedRow<TData>> _findWidget;
    private IndexedRow<TData>? _focusedRow = null;
    private bool _focusMove = false;

    public DataTableRule<TData> Rule;

    public bool AutoScroll = false;
    public bool UseHeader { get; set; } = true;
    public Func<bool> ShouldScrollToEnd { get; set; }

    public int MaxLocalStorage { get; init; }
    public ConcurrentQueue<TData> DataQueue = new();
    public uint DataIdx { get; private set; }
    public float RowHeightWithSpacing { get; private set; }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        // header
        if (UseHeader)
        {
            ImGui.Checkbox($"AutoScroll##{OwnerWindowName}", ref AutoScroll);

            // Selection info
            ImGuiHelper.SpacingSameLine();
            ImGui.Text($"Select:{_selection.Size}/{_localStorage.Count}");
            ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");

            // Filter
            ImGuiHelper.SpacingSameLine();
            _findWidget.RenderImObject(utcNow, deltaSec);
        }

        // body data
        if (_showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = _showStorage[0];

        if (ImGui.BeginTable("Datas", Rule.Columns.Length + 1, Rule.TableFlags))
        {
            // 헤더 고정
            ImGui.TableSetupScrollFreeze(0, 1);

            Rule.SetupColumns();

            // 선택기능을 위한 컬럼 설정
            ImGui.TableSetupColumn($"##Idx#{OwnerWindowName}", ImGuiTableColumnFlags.WidthFixed, 0);
            ImGui.TableHeadersRow();

            // 멀티셀렉트 처리
            var ms_io = ImGui.BeginMultiSelect(
                ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.BoxSelect1D,
                _selection.Size,
                _showStorage.Count);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= _showStorage.Count)
                    {
                        return unchecked((uint)-1);
                    }

                    return _showStorage[index].Index;
                });
            _selection.ApplyRequests(ms_io);

            // 대량 데이터를 위한 클리퍼
            ImGuiListClipper clipper = new();
            clipper.Begin(_showStorage.Count);
            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            int displayRowCount = 0;
            float beforeDrawPosY = 0;
            float afterDrawPosY = 0;

            while (clipper.Step())
            {
                // 클리핑 처리
                for (int displayIndex = clipper.DisplayStart; displayIndex < clipper.DisplayEnd; displayIndex++)
                {
                    displayRowCount++;

                    var colorEffect = Vector4.Zero;
                    var indexedRow = _showStorage[displayIndex];
                    string fieldsToString = Rule.RowToString(indexedRow.RowData);
                    bool isHighlighted = _findWidget.IsMachted(fieldsToString);
                    bool isRowHovered = false;
                    beforeDrawPosY = ImGui.GetCursorPosY();

                    // color 조정
                    colorEffect = _findWidget.IsMachted(fieldsToString) ? HighlightHelper.HighLightColor : Vector4.Zero;
                    colorEffect = _focusedRow?.Index == indexedRow.Index ? HighlightHelper.FoucusColor : colorEffect;

                    // row시작
                    ImGui.TableNextRow();

                    if (colorEffect != Vector4.Zero)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(colorEffect));
                    }

                    Rule.RenderRowHead(indexedRow.RowData);

                    // 데이터 필드 출력
                    Rule.RenderRow(indexedRow.RowData);

                    ImGui.TableNextColumn();
                    {
                        // 선택기능 컬럼
                        bool item_is_selected = _selection.Contains(indexedRow.Index);
                        ImGui.SetNextItemSelectionUserData(displayIndex);
                        ImGui.Selectable($"##{indexedRow.Index}#{OwnerWindowName}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

                        if (ImGui.IsItemHovered())
                        {
                            isRowHovered = true;
                        }
                    }

                    Rule.RenderRowFoot(indexedRow.RowData);

                    afterDrawPosY = ImGui.GetCursorPosY();

                    if (Rule.TooltipRender != null && ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.Button("floating Tooltip"))
                        {
                            string windowName = $"{WidgetName}:{indexedRow.Index}";

                            if (ImVisualizer.Instance.FindSubWindow(windowName) != null)
                            {
                                ImGui.SetWindowFocus(windowName);
                            }
                            else
                            {
                                var widget = new RenderActionWidget<TData>(indexedRow.RowData, Rule.TooltipRender);
                                var window = new SingleWidgetWindow<RenderActionWidget<TData>>(windowName);
                                window.InitializeWidget(widget);
                                window.IsVisibleImObject = true;

                                ImVisualizer.Instance.AddSubWindow(window);
                            }

                            ImGui.CloseCurrentPopup();
                        }

                        Rule.RenderTooltip(indexedRow.RowData);
                        ImGui.EndPopup();
                    }

                    if (isRowHovered)
                    {
                        if (Rule.TooltipRender != null && ImGui.BeginTooltip())
                        {
                            Rule.RenderTooltip(indexedRow.RowData);
                            ImGui.EndTooltip();
                        }
                    }
                }
            }

            RowHeightWithSpacing = afterDrawPosY - beforeDrawPosY;

            {
                // 스크롤 처리 블록
                if (_focusMove == true)
                {
                    // 포커스된 행이 있으면 해당 행이 보이도록 스크롤 조정
                    float? posY = null;
                    if (_findWidget.IsOnlyFiltered == true)
                    {
                        posY = _findWidget.FoundedFocusIndex * RowHeightWithSpacing;
                    }
                    else if (IndexKeyToLocalStorageIdx(_focusedRow?.Index ?? 0) is int localStorageIdx && localStorageIdx > 0)
                    {
                        posY = localStorageIdx * RowHeightWithSpacing;
                    }

                    if (posY.HasValue == true)
                    {
                        ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + posY.Value, 0.5f);
                    }

                    _focusMove = false;
                }

                if (ShouldScrollToEnd() == true)
                {
                    ImGui.SetScrollHereY(1.0f);
                }
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

            ImGui.EndTable();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        AdjustData();

        _showStorage = _findWidget.IsFinding && _findWidget.IsOnlyFiltered && _findWidget.FoundedList != null
            ? _findWidget.FoundedList
            : _localStorage;
    }

    public void PushData(TData data)
    {
        DataQueue.Enqueue(data);
    }

    public void ClearData()
    {
        DataQueue.Clear();
        _localStorage.Clear();
        _selection.Clear();
        DataIdx = 1;
        _findWidget.FindingTargetChange();
    }

    private void AdjustData()
    {
        while (DataQueue.TryDequeue(out var data) == true)
        {
            uint index = DataIdx++;
            var indexedRow = new IndexedRow<TData>(index, data);
            _localStorage.Add(indexedRow);

            string rowToString = Rule.RowToString(indexedRow.RowData);
            if (_findWidget.IsMachted(rowToString) == true)
            {
                _findWidget.FoundedList?.Add(indexedRow);
            }
        }

        // 선택한 데이터가 없는 경우 MaxLocalStorage 적용
        if (_selection.Size == 0)
        {
            // 로컬스토리지는 MaxLocalStorage 만큼만 데이터 저장
            int removeCount = _localStorage.Count - MaxLocalStorage;
            if (removeCount > 0)
            {
                _localStorage.RemoveRange(0, removeCount);
            }

            // 선택한 데이터가 있는 경우 MaxLocalStorage 적용을 유예시키는 이유는 MultiSelect중 앞의 데이터가 삭제될때,
            // 선택된 데이터가 삭제될수도 있기 때문이고(크래시등의 문제 확인 필요)
            // 선택이 정상적으로 유지되지 않는 버그 스러운 문제 때문.
        }

        // 선택한 데이터가 있는 경우라도, 현재 데이터의 범위가 MaxLocalStorage * 2를 초과하면 선택된 데이터를 초기화시키고, 삭제될수 있도록 처리
        if (_localStorage.Count > MaxLocalStorage * 2)
        {
            _selection.Clear();
        }
    }

    private bool ShouldScrollToEndInternal()
    {
        if (_selection.Size != 0)
        {
            return false;
        }
        else if (_findWidget.FoundedFocusIndex != 0)
        {
            // 단순 찾기 상태는 오토스크롤 유지, 포커스를 갖는 상태에서는 스크롤 정지
            return false;
        }

        return AutoScroll;
    }

    /// <summary>
    /// IndexKey를 로컬 스토리지의 인덱스로 변환. LocalStorage 가 중간에 삭제되지 않는 다는 가정. 삭제되는 구조로 변경시 수정 필요.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private int IndexKeyToLocalStorageIdx(uint index)
    {
        if (index < _localStorage.First().Index || index > _localStorage.Last().Index)
        {
            return -1;
        }

        return (int)(index - _localStorage.First().Index);
    }

    private void OnFindingTargetChanged()
    {
        _focusedRow = null;

        _findWidget.FoundedList =
            [ .. _localStorage
                .Where(indexedRow => _findWidget.IsMachted(Rule.RowToString(indexedRow.RowData)))
                .ToList(), ];
    }

    private void OnFoundedFocusMoved()
    {
        if (_findWidget.IsFinding == false)
        {
            return;
        }

        _selection.Clear();

        var foundedFocusedRow = _findWidget.FoundedList?[_findWidget.FoundedFocusIndex - 1];
        int showStorageIndex = _showStorage.FindIndex(r => r.Index == foundedFocusedRow?.Index);
        if (showStorageIndex != -1)
        {
            _focusedRow = _showStorage[showStorageIndex];
            _focusMove = true;
        }
    }

    public override void OnWindowFocused(BaseWindow ownerWindow)
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            var sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                uint selectedIndexKey = _selection.Storage.Data[i].Key;
                int targetIdx = IndexKeyToLocalStorageIdx(selectedIndexKey);
                if (targetIdx == -1)
                {
                    continue;
                }

                string rowToString = Rule.RowToString(_localStorage[targetIdx].RowData);
                sb.AppendLine(rowToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}
