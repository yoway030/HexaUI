namespace ELImGui.Window;

using ELImGui.Actor;
using ELImGui.Utils;
using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Data;
using System.Numerics;
using System.Text;

public class IndexedTableWidget<TData> : BaseWidget
{
    public IndexedTableWidget(DataTableRule<TData> rule, string widgetName)
        : this(rule, widgetName, String.Empty)
    {
    }

    public IndexedTableWidget(DataTableRule<TData> rule, string widgetName, string ownerWindowName, int maxLocalStorage = 10_000, int windowDepth = 0)
        : base(widgetName, ownerWindowName)
    {
        Rule = rule;
        MaxLocalStorage = maxLocalStorage;
        ShouldScrollToEnd = ShouldScrollToEndInternal;

        _findWidget = new("Find", OwnerWindowName);
        _findWidget.FindingTargetChangedFunc += OnFindingTargetChanged;
        _findWidget.FoundedFocusMovedFunc += OnFoundedFocusMoved;
    }

    private ImRenderListActor<IndexedRow<TData>> _dataActor = new();
    private uint _lastDataIdx = 0;
    private uint _headDataIdx = 1;
    private InComparerAdapter<IndexedRow<TData>> _indexedRowComparer = new(new IndexedRowComparer<TData>());
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
    public float RowHeightWithSpacing { get; private set; }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        var actorItems = _dataActor.GetDirectAdapter().Items;

        // header
        if (UseHeader)
        {
            bool prevAutoScroll = AutoScroll;
            ImGui.Checkbox($"AutoScroll##{OwnerWindowName}", ref AutoScroll);
            if (AutoScroll == true && prevAutoScroll == false)
            {
                // AutoScroll가 활성화 된 경우
                _selection.Clear();
            }

            // Selection info
            ImGuiHelper.SpacingSameLine();
            ImGui.Text($"Select:{_selection.Size}/{actorItems.Count}");
            ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");

            // Filter
            ImGuiHelper.SpacingSameLine();
            _findWidget.RenderImObject(utcNow, deltaSec, imInternalContext);
        }

        // body data
        if (_showStorage.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = _showStorage.FirstOrDefault();

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
                    colorEffect = _findWidget.IsMachted(fieldsToString) ? ImGuiColorHelper.AlphaBlendClamped(ImGuiTheme.Values.Focus, 0.8f) : Vector4.Zero;
                    colorEffect = _focusedRow?.Index == indexedRow.Index ? ImGuiTheme.Values.Focus : colorEffect;

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

                            imInternalContext.SubWindows.TryGetValue(windowName, out var subWindow);

                            if (subWindow != null)
                            {
                                ImGui.SetWindowFocus(windowName);
                            }
                            else
                            {
                                var window = new SingleWidgetWindow<RenderActionWidget<TData>>(windowName);
                                var widget = new RenderActionWidget<TData>(windowName, window.WindowName, indexedRow.RowData, Rule.TooltipRender);

                                window.InitializeWidget(widget);
                                window.IsVisibleImObject = true;

                                imInternalContext.SubWindows.Add(window.WindowName, window);
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
                    else if (_focusedRow.HasValue == true)
                    {
                        int index = _showStorage.BinarySearch(_focusedRow.Value, _indexedRowComparer);
                        posY = index * RowHeightWithSpacing;
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

    public override void OnPrevUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        if (_dataActor.IsInitialized == false)
        {
            _dataActor.Initialize(Environment.CurrentManagedThreadId);
            _dataActor.OnAdded += OnActorAdded;
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        _dataActor.Work();

        var actorItems = _dataActor.GetDirectAdapter().Items;

        // 선택한 데이터가 있는 경우 삭제를 유예한다.
        // 하지만 현재 데이터의 범위가 MaxLocalStorage * 2를 초과하면 선택된 데이터를 초기화시키고, 삭제될수 있도록 처리
        int currentCount = actorItems.Count;
        if (currentCount > MaxLocalStorage * 2)
        {
            _selection.Clear();
        }

        // 선택한 데이터가 없는 경우 MaxLocalStorage 적용
        if (_selection.Size == 0)
        {
            int dataCount = actorItems.Count;
            int removeCount = dataCount - MaxLocalStorage;
            if (removeCount > 0)
            {
                actorItems.RemoveRange(0, removeCount);
                _headDataIdx += (uint)removeCount;
            }

            // 선택한 데이터가 있는 경우 MaxLocalStorage 적용을 유예시키는 이유는 MultiSelect중 앞의 데이터가 삭제될때,
            // 선택된 데이터가 삭제될수도 있기 때문이고(크래시등의 문제 확인 필요)
            // 선택이 정상적으로 유지되지 않는 버그 스러운 문제 때문.
        }

        // 상황에 맞는 출력용 스토리지 선택
        _showStorage = _findWidget.IsFinding && _findWidget.IsOnlyFiltered
            ? _findWidget.FoundedList
            : _dataActor.GetDirectAdapter().Items;
    }

    public uint PushDataPost(TData data)
    {
        uint dataIdx = Interlocked.Increment(ref _lastDataIdx);
        var indexedRow = new IndexedRow<TData>(dataIdx, data);

        _dataActor.GetPostAdapter().AddPost(indexedRow);
        return dataIdx;
    }

    public uint PushDataDirect(TData data)
    {
        uint dataIdx = Interlocked.Increment(ref _lastDataIdx);
        var indexedRow = new IndexedRow<TData>(dataIdx, data);

        _dataActor.GetDirectAdapter().AddDirect(indexedRow);
        return dataIdx;
    }

    private void OnActorAdded(in IndexedRow<TData> added)
    {
        string rowToString = Rule.RowToString(added.RowData);
        if (_findWidget.IsMachted(rowToString) == true)
        {
            _findWidget.FoundedList.Add(added);
        }
    }

    public void ClearDataDirect()
    {
        _lastDataIdx = 0;
        _selection.Clear();
        _findWidget.FindingTargetChange();
        _dataActor.GetDirectAdapter().ClearDirect();
    }

    public async Task<List<TData>> PeekRecentDatasPost(int peekCount)
    {
        return await _dataActor.GetPostAdapter().Ask((innerAdapter) =>
        {
            var items = innerAdapter.Items;
            return items.OrderByDescending(r => r.Index)
                .Take(peekCount)
                .OrderBy(r => r.Index)
                .Select(r => r.RowData)
                .ToList();
        });
    }

    public List<TData> PeekRecentDatasDirect(int peekCount)
    {
        var items = _dataActor.GetDirectAdapter().Items;
        return items.OrderByDescending(r => r.Index)
            .Take(peekCount)
            .OrderBy(r => r.Index)
            .Select(r => r.RowData)
            .ToList();
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
    /// IndexKey를 로컬 스토리지의 인덱스로 변환. 삭제되지 않는 다는 가정. 삭제되는 구조로 변경시 수정 필요.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private int IndexKeyToLocalStorageIdx(uint index)
    {
        if (index < _headDataIdx || index >= _lastDataIdx)
        {
            return -1;
        }

        return (int)(index - _headDataIdx);
    }

    private void OnFindingTargetChanged()
    {
        _focusedRow = null;

        _findWidget.FoundedList =
            [ .. _dataActor.GetDirectAdapter().Items
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

        if (_findWidget!.TryGetFocusedData(out var foundedFocusedRow) == false)
        {
            return;
        }

        int showStorageIndex = _showStorage.FindIndex(r => r.Index == foundedFocusedRow.Index);
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
            var actorItems = _dataActor.GetDirectAdapter().Items;
            var sb = new StringBuilder();

            for (int i = 0; i < _selection.Storage.Data.Size; i++)
            {
                uint selectedIndexKey = _selection.Storage.Data[i].Key;
                int targetIdx = IndexKeyToLocalStorageIdx(selectedIndexKey);
                if (targetIdx == -1)
                {
                    continue;
                }

                string rowToString = Rule.RowToString(actorItems[targetIdx].RowData);
                sb.AppendLine(rowToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}
