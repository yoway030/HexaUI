namespace HexaImGui.Window;

public abstract class SurfableIndexingData
{
    protected uint _index;

    protected string _cachedLabel = string.Empty;

    public uint Index
    {
        get => _index; set
        {
            _index = value;
            _cachedLabel = $"{_index}";
        }
    }

    public string Label => _cachedLabel;

    public abstract int DrawableFieldCount { get; }

    public abstract IEnumerable<Action> GetColumnSetupActions();

    public abstract IEnumerable<Action> GetFieldDrawActions();

    public abstract void TooltipDraw();

    public abstract string FieldsToString { get; }
}