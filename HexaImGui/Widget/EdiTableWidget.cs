namespace ELImGui.Window;

using ELImGui.Actor;
using ELImGui.Utils;
using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Numerics;
using System.Text;

public class EdiTableWidget<TData> : BaseWidget
{
    public EdiTableWidget(DataTableRule<TData> rule, string widgetName)
        : this(rule, widgetName, String.Empty)
    {
    }

    public EdiTableWidget(DataTableRule<TData> rule, string widgetName, string ownerWindowName, int windowDepth = 0)
        : base(widgetName, ownerWindowName)
    {
        Rule = rule;
    }

    private ImRenderListActor<IndexedRow<TData>> _dataActor = new();
    private uint _lastDataIdx = 0;
    private InComparerAdapter<IndexedRow<TData>> _indexedRowComparer = new(new IndexedRowComparer<TData>());
    private ImGuiSelectionBasicStorage _selection = new();

    public DataTableRule<TData> Rule;
    public bool UseHeader { get; set; } = true;
    public float RowHeightWithSpacing { get; private set; }

    public void InitializeActor(int renderThreadId)
    {
        if (_dataActor.IsInitialized == false)
        {
            _dataActor.Initialize(renderThreadId);
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        var actorItems = _dataActor.GetInnerAdapter().Items;

        // header
        if (UseHeader)
        {
            // Selection info
            ImGuiHelper.SpacingSameLine();
            ImGui.Text($"Select:{_selection.Size}/{actorItems.Count}");
            ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");

            // Filter
            ImGuiHelper.SpacingSameLine();
        }

        // body data
        if (actorItems.Any() == false)
        {
            ImGui.Text("No data available.");
            return;
        }

        var initData = actorItems.FirstOrDefault();

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
                actorItems.Count);

            ImGuiFuncPtrHelper.SetAdapterIndexToStorageId(ref _selection,
                (storage, index) =>
                {
                    if (index < 0 || index >= actorItems.Count)
                    {
                        return unchecked((uint)-1);
                    }
                 
                    return actorItems[index].Index;
                });
            _selection.ApplyRequests(ms_io);

            // 대량 데이터를 위한 클리퍼
            ImGuiListClipper clipper = new();
            clipper.Begin(actorItems.Count);
            if (ms_io.RangeSrcItem != -1)
            {
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem);
            }

            float beforeDrawPosY = 0;
            float afterDrawPosY = 0;

            while (clipper.Step())
            {
                // 클리핑 처리
                for (int scrollIndex = clipper.DisplayStart; scrollIndex < clipper.DisplayEnd; scrollIndex++)
                {
                    var colorEffect = Vector4.Zero;
                    var indexedRow = actorItems[scrollIndex];
                    string fieldsToString = Rule.RowToString(indexedRow.RowData);
                    bool isRowHovered = false;
                    beforeDrawPosY = ImGui.GetCursorPosY();

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
                        ImGui.SetNextItemSelectionUserData(scrollIndex);
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
                                var widget = new RenderActionWidget<TData>(indexedRow.RowData, Rule.TooltipRender);
                                var window = new SingleWidgetWindow<RenderActionWidget<TData>>(windowName);
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

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        if (_dataActor.IsInitialized == false)
        {
            _dataActor.Initialize(Environment.CurrentManagedThreadId);
        }

        _dataActor.Work();
    }

    public uint AddData(TData data)
    {
        uint dataIdx = Interlocked.Increment(ref _lastDataIdx);
        var indexedRow = new IndexedRow<TData>(dataIdx, data);

        _dataActor.GetOuterAdapter().AddPost(indexedRow);
        return dataIdx;
    }

    public async Task<uint> FindData(TData data, IInComparer<TData>? comparer = null)
    { 
        return await _dataActor.GetOuterAdapter().Ask((innerAdapter) =>
        {
            var items = innerAdapter.Items;
            for (int i = 0; items.Count < i; i++)
            {
                bool founded = false;
                if (comparer != null && comparer.Compare(items[i].RowData, data) == 0)
                {
                    founded = true;
                }
                else if (EqualityComparer<TData>.Default.Equals(items[i].RowData, data))
                {
                    founded = true;
                }

                if (founded)
                {
                    return items[i].Index;
                }
            }
            return uint.MaxValue;
        });
    }

    public async Task<bool> UpdateData(TData data, IInComparer<TData>? comparer = null)
    {
        return await _dataActor.GetOuterAdapter().Ask((innerAdapter) =>
        {
            var items = innerAdapter.Items;
            for (int i = 0; items.Count < i; i++)
            {
                bool founded = false;
                if (comparer != null && comparer.Compare(items[i].RowData, data) == 0)
                {
                    founded = true;
                }
                else if (EqualityComparer<TData>.Default.Equals(items[i].RowData, data))
                {
                    founded = true;
                }

                if (founded)
                {
                    items[i] = new IndexedRow<TData>(items[i].Index, data);
                    return true;
                }
            }

            return false;
        });
    }

    public async Task<bool> UpdateIndexedData(uint index, TData newData)
    {
        return await _dataActor.GetOuterAdapter().Ask((innerAdapter) =>
        {
            var items = innerAdapter.Items;
            int idx = items.BinarySearch(new IndexedRow<TData>(index, default!), _indexedRowComparer);
            if (idx < 0)
            {
                return false;
            }

            items[idx] = new IndexedRow<TData>(index, newData);
            return true;
        });
    }

    public async Task<bool> RemoveIndex(uint index)
    {
        return await _dataActor.GetOuterAdapter().Ask((innerAdapter) =>
        {
            var items = innerAdapter.Items;
            int idx = items.BinarySearch(new IndexedRow<TData>(index, default!), _indexedRowComparer);
            if (idx < 0)
            {
                return false;
            }

            items.RemoveAt(idx);
            return true;
        });
    }

    public void ClearData()
    {
        _lastDataIdx = 0;
        _selection.Clear();
        _dataActor.GetOuterAdapter().ClearPost();
    }

    /// <summary>
    /// IndexKey를 로컬 스토리지의 인덱스로 변환. 삭제되지 않는 다는 가정. 삭제되는 구조로 변경시 수정 필요.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private int IndexKeyToLocalStorageIdx(uint index)
    {
        if (index < 0 || index >= _lastDataIdx)
        {
            return -1;
        }

        var actorItems = _dataActor.GetInnerAdapter().Items;
        return actorItems.BinarySearch(new IndexedRow<TData>(index, default!), _indexedRowComparer);
    }

    public override void OnWindowFocused(BaseWindow ownerWindow)
    {
        // Check for copy to clipboard action
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.C))
        {
            var actorItems = _dataActor.GetInnerAdapter().Items;
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
