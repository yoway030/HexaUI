namespace ELImGui.Widget;

using System;

public class RenderActionWidget<T> : BaseWidget
{
    private T _model;
    private InAction<T> _renderAction;

    public RenderActionWidget(string widgetName, string ownerWindowName, T model, InAction<T> renderAction)
        : base(widgetName, ownerWindowName)
    {
        _model = model;
        _renderAction = renderAction;
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        _renderAction.Invoke(_model);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
    }
}
