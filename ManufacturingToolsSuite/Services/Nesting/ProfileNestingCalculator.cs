using ManufacturingToolsSuite.Models.Nesting;

namespace ManufacturingToolsSuite.Services.Nesting;

/// <summary>
/// SAP_to_EXCEL_V1 NestingService.CalculateNesting portu.
/// Profil ölçüsüne göre gruplar, kelepçe payı ve kerf dahil yerleşim yapar.
/// </summary>
public static class ProfileNestingCalculator
{
    public static int UsableLengthMm(int standardLengthMm, int clampPerEndMm) =>
        Math.Max(0, standardLengthMm - (2 * clampPerEndMm));
    public static IReadOnlyList<NestingRow> DuplicateByVehicle(IReadOnlyList<NestingRow> input, int vehicleCount) =>
        input.Select(row => new NestingRow
        {
            ComponentNumber = row.ComponentNumber,
            ComponentName = row.ComponentName,
            Quantity = row.Quantity * vehicleCount,
            Dimensions = row.Dimensions,
            LengthMeters = row.LengthMeters
        }).ToList();

    public static IReadOnlyList<CalculatedProfile> Calculate(
        IReadOnlyList<NestingRow> input,
        int standardLengthMm,
        int cutSpacingMm,
        int clampPerEndMm)
    {
        var usableLengthMm = UsableLengthMm(standardLengthMm, clampPerEndMm);
        var result = new List<CalculatedProfile>();

        foreach (var group in input.GroupBy(x => x.Dimensions))
        {
            var allParts = group
                .Where(item => item.LengthMm > 0 && item.Quantity > 0)
                .SelectMany(item => Enumerable.Repeat(
                    (LengthMm: item.LengthMm, Name: item.ComponentNumber),
                    item.Quantity))
                .OrderByDescending(p => p.LengthMm)
                .ToList();

            var profileIndex = 1;
            while (allParts.Count > 0)
            {
                var remainingOnProfile = (double)usableLengthMm;
                var placed = new List<(double LengthMm, string Name)>();
                var indexesToRemove = new List<int>();

                for (var i = 0; i < allParts.Count; i++)
                {
                    var part = allParts[i];
                    var required = part.LengthMm + (placed.Count > 0 ? cutSpacingMm : 0);
                    if (remainingOnProfile >= required)
                    {
                        placed.Add((part.LengthMm, part.Name));
                        remainingOnProfile -= required;
                        indexesToRemove.Add(i);
                    }
                }

                if (placed.Count > 0)
                {
                    var usedWithinUsable = usableLengthMm - remainingOnProfile;
                    var totalScrap = remainingOnProfile + (2 * clampPerEndMm);
                    var utilization = standardLengthMm > 0 ? usedWithinUsable / standardLengthMm * 100 : 0;

                    result.Add(new CalculatedProfile
                    {
                        ProfileIndex = profileIndex++,
                        Dimensions = group.Key,
                        ComponentNumbers = placed.Select(p => p.Name).ToList(),
                        UsedLengthsMm = placed.Select(p => p.LengthMm).ToList(),
                        UsedLengthTotalMm = usedWithinUsable,
                        RemainingLengthMm = totalScrap,
                        UtilizationPercent = utilization
                    });

                    foreach (var index in indexesToRemove.OrderByDescending(v => v))
                        allParts.RemoveAt(index);
                }
                else if (allParts.Count > 0)
                {
                    allParts.RemoveAt(0);
                }
            }
        }

        return result;
    }

    public static IReadOnlyList<BestLengthResult> AnalyzeBestStandardLengths(
        IReadOnlyList<NestingRow> input,
        int vehicleCount,
        IEnumerable<int> candidateLengthsMm,
        int cutSpacingMm,
        int clampPerEndMm)
    {
        var expanded = DuplicateByVehicle(input, vehicleCount);
        var results = new List<BestLengthResult>();

        foreach (var stdLen in candidateLengthsMm.Distinct().OrderBy(v => v))
        {
            var calc = Calculate(expanded, stdLen, cutSpacingMm, clampPerEndMm);
            var totalUsed = calc.Sum(p => p.UsedLengthTotalMm);
            var totalWaste = calc.Sum(p => p.RemainingLengthMm);
            var totalProfiles = calc.Count;
            var utilization = totalProfiles == 0 ? 0 : totalUsed / (totalProfiles * stdLen);

            results.Add(new BestLengthResult
            {
                StandardLengthMm = stdLen,
                TotalProfiles = totalProfiles,
                TotalUsedMm = totalUsed,
                TotalWasteMm = totalWaste,
                Utilization = utilization
            });
        }

        return results;
    }

    public static readonly int[] DefaultCandidateLengthsMm = [5400, 5600, 5800, 6000];
}
