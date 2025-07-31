namespace HexaImGui.Window;

public abstract class SurfableIndexingData
{
    protected uint _insertedIndex;

    protected string _cachedLabel = string.Empty;

    public uint InsertedIndex
    {
        get => _insertedIndex;
        set
        {
            _insertedIndex = value;
            _cachedLabel = $"{_insertedIndex}";
        }
    }

    public string Label => _cachedLabel;

    public abstract IEnumerable<Action> GetColumnSetupActions();

    public abstract IEnumerable<Action> GetFieldDrawActions();

    public virtual void TooltipDraw() { }

    public abstract string FieldsToString { get; }
}