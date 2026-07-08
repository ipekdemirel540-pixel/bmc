using ManufacturingToolsSuite.Models.Nesting;

namespace ManufacturingToolsSuite.Services.Nesting;

public static class DailyPlanGenerator
{
    public static IReadOnlyList<DailyPlanEntry> Generate(NestingParameters parameters)
    {
        if (parameters.CustomSequence.Count > 0)
            return FromSequence(parameters.CustomSequence);

        return FromUniform(parameters.TotalVehicles, parameters.VehiclesPerDay);
    }

    private static IReadOnlyList<DailyPlanEntry> FromSequence(IReadOnlyList<int> sequence)
    {
        var plan = new List<DailyPlanEntry>();
        var cumulative = 0;
        for (var i = 0; i < sequence.Count; i++)
        {
            cumulative += sequence[i];
            plan.Add(new DailyPlanEntry
            {
                Day = i + 1,
                VehicleCount = sequence[i],
                CumulativeVehicles = cumulative
            });
        }
        return plan;
    }

    private static IReadOnlyList<DailyPlanEntry> FromUniform(int totalVehicles, int vehiclesPerDay)
    {
        var plan = new List<DailyPlanEntry>();
        var remaining = totalVehicles;
        var day = 1;
        var cumulative = 0;

        while (remaining > 0)
        {
            var count = Math.Min(vehiclesPerDay, remaining);
            cumulative += count;
            plan.Add(new DailyPlanEntry
            {
                Day = day++,
                VehicleCount = count,
                CumulativeVehicles = cumulative
            });
            remaining -= count;
        }

        return plan;
    }
}
