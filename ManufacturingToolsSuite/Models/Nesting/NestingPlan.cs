namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class NestingPlan
{
    public IReadOnlyList<NestingRow> SourceRows { get; init; } = Array.Empty<NestingRow>();
    public NestingParameters Parameters { get; init; } = new();
    public IReadOnlyList<CalculatedProfile> Profiles { get; init; } = Array.Empty<CalculatedProfile>();
    public IReadOnlyList<DailyPlanEntry> DailyPlan { get; init; } = Array.Empty<DailyPlanEntry>();
    public double TotalUsedMm { get; init; }
    public double TotalWasteMm { get; init; }
    public double OverallUtilizationPercent { get; init; }
    public int TotalProfileCount { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
}
