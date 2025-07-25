namespace HexaImGui;

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

    public abstract void FieldSetupColumn(int field);

    public abstract void FieldDraw(int field);

    public abstract void TooltipDraw();

    public abstract string FieldsToString { get; }
}