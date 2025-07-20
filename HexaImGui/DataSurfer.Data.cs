namespace HexaImGui;

public interface ISurfableData
{
    int DrawableFieldCount { get; }

    void FieldSetupColumn(int field);

    void FieldDraw(int field);

    void TooltipDraw();

    string FieldsToString { get; }
}

public abstract class SurfableIndexingData : ISurfableData
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

    public abstract void FieldSetupColumn(int field);

    public abstract void FieldDraw(int field);

    public abstract void TooltipDraw();

    public abstract string FieldsToString { get; }
}