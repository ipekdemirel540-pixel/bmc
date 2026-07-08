using ManufacturingToolsSuite.Models.Nesting;

namespace ManufacturingToolsSuite.Services.Nesting;

public static class NestingEngine
{
    public static NestingPlan Optimize(IReadOnlyList<NestingRow> sourceRows, NestingParameters parameters)
    {
        var validation = NestingValidator.Validate(sourceRows, parameters);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));

        var expanded = ProfileNestingCalculator.DuplicateByVehicle(sourceRows, parameters.TotalVehicles);
        var profiles = ProfileNestingCalculator.Calculate(
            expanded,
            parameters.StandardLengthMm,
            parameters.KerfMm,
            parameters.ClampPerEndMm);

        var totalUsed = profiles.Sum(p => p.UsedLengthTotalMm);
        var totalWaste = profiles.Sum(p => p.RemainingLengthMm);
        var totalStock = profiles.Count * parameters.StandardLengthMm;

        return new NestingPlan
        {
            SourceRows = sourceRows,
            Parameters = parameters,
            Profiles = profiles,
            DailyPlan = DailyPlanGenerator.Generate(parameters),
            TotalUsedMm = totalUsed,
            TotalWasteMm = totalWaste,
            OverallUtilizationPercent = totalStock > 0 ? totalUsed / totalStock * 100 : 0,
            TotalProfileCount = profiles.Count,
            GeneratedAt = DateTime.Now
        };
    }

    public static NestingPlan OptimizeDailyBatch(
        IReadOnlyList<NestingRow> sourceRows,
        NestingParameters parameters,
        int batchVehicles)
    {
        var batchParams = new NestingParameters
        {
            TotalVehicles = batchVehicles,
            KerfMm = parameters.KerfMm,
            VehiclesPerDay = parameters.VehiclesPerDay,
            StandardLengthMm = parameters.StandardLengthMm,
            ClampPerEndMm = parameters.ClampPerEndMm,
            ExcelPath = parameters.ExcelPath
        };
        return Optimize(sourceRows, batchParams);
    }

    public static BestLengthResult FindBestLength(
        IReadOnlyList<NestingRow> sourceRows,
        NestingParameters parameters)
    {
        var candidates = ProfileNestingCalculator.DefaultCandidateLengthsMm
            .Concat([parameters.StandardLengthMm])
            .Where(x => x > 0 && x <= NestingParameters.MaxStandardLengthMm)
            .Distinct()
            .OrderBy(x => x);

        var results = ProfileNestingCalculator.AnalyzeBestStandardLengths(
            sourceRows,
            parameters.TotalVehicles,
            candidates,
            parameters.KerfMm,
            parameters.ClampPerEndMm);

        return results
            .OrderBy(r => r.TotalWasteMm)
            .ThenBy(r => r.TotalProfiles)
            .ThenByDescending(r => r.Utilization)
            .First();
    }

    public static NestingAnalysis Analyze(IReadOnlyList<NestingRow> sourceRows, NestingParameters parameters)
    {
        var expanded = ProfileNestingCalculator.DuplicateByVehicle(sourceRows, parameters.TotalVehicles);
        var pieceCount = expanded.Sum(r => r.Quantity);
        var materialMm = expanded.Sum(r => r.LengthMm * r.Quantity);

        return new NestingAnalysis
        {
            PartTypeCount = sourceRows.Count,
            TotalPieceCount = pieceCount,
            TotalMaterialMm = materialMm,
            EstimatedDays = parameters.VehiclesPerDay > 0
                ? (int)Math.Ceiling((double)parameters.TotalVehicles / parameters.VehiclesPerDay)
                : 0,
            DimensionGroupCount = sourceRows.Select(r => r.Dimensions).Distinct().Count()
        };
    }
}

public sealed class NestingAnalysis
{
    public int PartTypeCount { get; init; }
    public int TotalPieceCount { get; init; }
    public double TotalMaterialMm { get; init; }
    public int EstimatedDays { get; init; }
    public int DimensionGroupCount { get; init; }
}
