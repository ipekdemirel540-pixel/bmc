namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class NestingRow
{
    public string ComponentNumber { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string Dimensions { get; init; } = string.Empty;
    /// <summary>Boy metre cinsinden (SAP BOM formatı).</summary>
    public double LengthMeters { get; init; }

    public double LengthMm => LengthMeters * 1000.0;
}
