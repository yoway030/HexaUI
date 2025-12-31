using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample;

internal class TextRow
{
    public DateTime Timestamp { get; set; } = DateTime.MinValue;
    public int Index { get; set; } = 0;
    public string Text { get; set; } = string.Empty;

}

public sealed class SampleDataType
{
    public string Name { get; init; } = "";
    public int Level { get; init; }
    public float DPS { get; init; }
    public string Class { get; init; } = "";
}