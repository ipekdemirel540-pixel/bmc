using System.Globalization;
using System.Text.RegularExpressions;

namespace ManufacturingToolsSuite.Services.Nesting;

public static partial class SequenceParser
{
    public static IReadOnlyList<int> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<int>();

        var tokens = SplitRegex().Split(input.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => int.Parse(t.Trim(), CultureInfo.InvariantCulture))
            .ToList();

        return tokens;
    }

    [GeneratedRegex(@"[-,;\s]+", RegexOptions.Compiled)]
    private static partial Regex SplitRegex();
}
