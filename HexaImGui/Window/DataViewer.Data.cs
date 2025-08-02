namespace HexaImGui.Window;

public abstract class ViewableData
{
    public abstract IEnumerable<Action> GetColumnSetupActions();

    public abstract IEnumerable<Action> GetFieldDrawActions();

    public virtual void TooltipDraw() { }

    public abstract string FieldsToString { get; }
}