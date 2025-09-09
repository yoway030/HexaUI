namespace ELImGui;

public interface IImRenderable
{
    void RenderImObject(DateTime utcNow, double deltaSec);
}

public interface IImUpdatable
{
    void UpdateImObject(DateTime utcNow, double deltaSec);
}

public interface IImVisible
{
    public bool IsVisibleImObject { get; set; }
}

public interface IImWindow
{
    public string WindowName { get; init; }
}

public interface IImWidget
{
    public string WidgetName { get; init; }
    public string ParentWindowId { get; init; }
}

public interface IImMenu
{
}
