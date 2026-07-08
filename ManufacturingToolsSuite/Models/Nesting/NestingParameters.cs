namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class NestingParameters
{
    public const int MaxStandardLengthMm = 6000;
    public const int DefaultStandardLengthMm = 6000;
    public const int DefaultClampPerEndMm = 100;

    public int TotalVehicles { get; init; }
    public int KerfMm { get; init; }
    public int VehiclesPerDay { get; init; }
    public int StandardLengthMm { get; init; } = DefaultStandardLengthMm;
    public int ClampPerEndMm { get; init; } = DefaultClampPerEndMm;
    public IReadOnlyList<int> CustomSequence { get; init; } = Array.Empty<int>();
    public string? ExcelPath { get; init; }
}
