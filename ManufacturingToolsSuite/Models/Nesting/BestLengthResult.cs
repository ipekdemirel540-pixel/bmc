namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class BestLengthResult
{
    public int StandardLengthMm { get; init; }
    public int TotalProfiles { get; init; }
    public double TotalUsedMm { get; init; }
    public double TotalWasteMm { get; init; }
    public double Utilization { get; init; }
}
