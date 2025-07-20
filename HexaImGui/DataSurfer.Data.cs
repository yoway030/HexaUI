namespace HexaImGui;

public interface ISurfableData
{
    int DrawableFieldCount { get; }

    void InitDrawableField(int field);

    void DrawField(int field);

    void DrawHoverTooltip();

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

    public abstract void InitDrawableField(int field);

    public abstract void DrawField(int field);

    public abstract void DrawHoverTooltip();

    public abstract string FieldsToString { get; }
}