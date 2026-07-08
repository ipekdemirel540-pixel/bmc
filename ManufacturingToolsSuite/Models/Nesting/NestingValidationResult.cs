namespace ManufacturingToolsSuite.Models.Nesting;

public sealed class NestingValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static NestingValidationResult Ok(IReadOnlyList<string>? warnings = null) =>
        new() { IsValid = true, Warnings = warnings ?? Array.Empty<string>() };

    public static NestingValidationResult Fail(params string[] errors) =>
        new() { IsValid = false, Errors = errors };
}
