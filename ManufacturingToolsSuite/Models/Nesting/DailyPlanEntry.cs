namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class DailyPlanEntry
{
    public int Day { get; init; }
    public int VehicleCount { get; init; }
    public int CumulativeVehicles { get; init; }
}
