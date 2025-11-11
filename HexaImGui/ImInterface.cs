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
    public string WidgetName { get; set; }
    public string OwnerWindowName { get; set; }
}

public interface IImMenu
{
}
