namespace HexaImGui.Widget;

public abstract class SurfableIndexingData : ViewableData
{
    protected uint _index;
    protected string _cachedIndexString = string.Empty;

    public uint Index
    {
        get => _index;
        set
        {
            _index = value;
            _cachedIndexString = $"{_index}";
        }
    }

    public string IndexString => _cachedIndexString;
}