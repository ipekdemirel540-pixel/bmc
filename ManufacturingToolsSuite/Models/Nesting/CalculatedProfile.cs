namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class CalculatedProfile
{
    public int ProfileIndex { get; init; }
    public string Dimensions { get; init; } = string.Empty;
    public IReadOnlyList<string> ComponentNumbers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<double> UsedLengthsMm { get; init; } = Array.Empty<double>();
    public double UsedLengthTotalMm { get; init; }
    public double RemainingLengthMm { get; init; }
    public double UtilizationPercent { get; init; }
}
