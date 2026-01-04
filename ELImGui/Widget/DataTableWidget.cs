namespace ELImGui.Window;

using ELImGui.Actor;
using ELImGui.Utils;
using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Text;

public class DataTableWidget<TData> : BaseWidget
{
    public DataTableWidget(DataTableRule<TData> rule, string widgetName)
        : this(rule, widgetName, String.Empty)
    {
    }

    public DataTableWidget(DataTableRule<TData> rule, string widgetName, string ownerWindowName, int windowDepth = 0)
        : base(widgetName, ownerWindowName)
    {
        Rule = rule;
    }

    private ImRenderListActor<TData> _dataActor = new();
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
        var actorItems = _dataActor.GetDirectAdapter().Items;

        // header
        if (UseHeader)
        {
            // Selection info
            ImGuiHelper.SpacingSameLine();
            ImGui.Text($"Select:{_selection.Size}/{actorItems.Count}");
            ImGuiHelper.HelpMarkerSameLine("선택된 데이터수 / 출력 중인 데이터수");
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

                    return (uint)index;
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
                    var containedData = actorItems[scrollIndex];
                    string fieldsToString = Rule.RowToString(containedData);
                    bool isRowHovered = false;
                    beforeDrawPosY = ImGui.GetCursorPosY();

                    // row시작
                    ImGui.TableNextRow();

                    Rule.RenderRowHead(containedData);

                    // 데이터 필드 출력
                    Rule.RenderRow(containedData);

                    ImGui.TableNextColumn();
                    {
                        // 선택기능 컬럼
                        bool item_is_selected = _selection.Contains((uint)scrollIndex);
                        ImGui.SetNextItemSelectionUserData(scrollIndex);
                        ImGui.Selectable($"##{scrollIndex}#{OwnerWindowName}", item_is_selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

                        if (ImGui.IsItemHovered())
                        {
                            isRowHovered = true;
                        }
                    }

                    Rule.RenderRowFoot(containedData);

                    afterDrawPosY = ImGui.GetCursorPosY();

                    if (Rule.TooltipRender != null && ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.Button("floating Tooltip"))
                        {
                            string windowName = $"{WidgetName}:{scrollIndex}";

                            imInternalContext.SubWindows.TryGetValue(windowName, out var subWindow);

                            if (subWindow != null)
                            {
                                ImGui.SetWindowFocus(windowName);
                            }
                            else
                            {
                                var window = new SingleWidgetWindow<RenderActionWidget<TData>>(windowName);
                                var widget = new RenderActionWidget<TData>(windowName, window.WindowName, containedData, Rule.TooltipRender);
                                
                                window.InitializeWidget(widget);
                                window.IsVisibleImObject = true;

                                imInternalContext.SubWindows.Add(window.WindowName, window);
                            }

                            ImGui.CloseCurrentPopup();
                        }

                        Rule.RenderTooltip(containedData);
                        ImGui.EndPopup();
                    }

                    if (isRowHovered)
                    {
                        if (Rule.TooltipRender != null && ImGui.BeginTooltip())
                        {
                            Rule.RenderTooltip(containedData);
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

    public override void OnPrevUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        base.OnPrevUpdate(utcNow, deltaSec, imInternalContext);

        if (_dataActor.IsInitialized == false)
        {
            _dataActor.Initialize(Environment.CurrentManagedThreadId);
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        _dataActor.Work();
    }

    public void AddDataPost(TData data)
    {
        _dataActor.GetPostAdapter().AddPost(data);
    }

    public void AddDataDirect(TData data)
    {
        _dataActor.GetDirectAdapter().AddDirect(data);
    }

    public async Task<int> FindDataAsk(TData data, IInComparer<TData>? comparer = null)
    {
        return await _dataActor.GetPostAdapter().Ask((directAdaption) =>
        {
            var items = directAdaption.Items;

            for (int i = 0; items.Count < i; i++)
            {
                bool founded = false;
                if (comparer != null && comparer.Compare(items[i], data) == 0)
                {
                    founded = true;
                }
                else if (EqualityComparer<TData>.Default.Equals(items[i], data))
                {
                    founded = true;
                }

                if (founded)
                {
                    return i;
                }
            }

            return -1;
        });
    }

    public async Task<bool> UpdateDataAsk(TData data, IInComparer<TData>? comparer = null)
    {
        return await _dataActor.GetPostAdapter().Ask((directAdapter) =>
        {
            var items = directAdapter.Items;

            for (int i = 0; items.Count < i; i++)
            {
                bool founded = false;
                if (comparer != null && comparer.Compare(items[i], data) == 0)
                {
                    founded = true;
                }
                else if (EqualityComparer<TData>.Default.Equals(items[i], data))
                {
                    founded = true;
                }

                if (founded)
                {
                    items[i] = data;
                    return true;
                }
            }

            return false;
        });
    }

    public void UpdateDataAtPost(int index, TData newData)
    {
        _dataActor.GetPostAdapter().UpdateAtPost(index, newData);
    }

    public void RemoveDataAtPost(int index)
    {
        _dataActor.GetPostAdapter().RemoveAtPost(index);
    }

    public void ClearDataDirect()
    {
        _selection.Clear();
        _dataActor.GetDirectAdapter().ClearDirect();
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
                int selectedIndexKey = (int)_selection.Storage.Data[i].Key;
                string rowToString = Rule.RowToString(actorItems[selectedIndexKey]);
                sb.AppendLine(rowToString);
            }

            ImGui.SetClipboardText(sb.ToString());
        }
    }
}
