namespace modular_mlm.Infrastructure.Idempotency;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public bool CleanupEnabled { get; init; } = true;
    public int ClaimMinutes { get; init; } = 5;
    public int RetentionDays { get; init; } = 7;
    public int CleanupIntervalMinutes { get; init; } = 15;
    public int CleanupBatchSize { get; init; } = 500;
    public int MaximumKeyLength { get; init; } = 200;
    public int MaximumOutcomeBytes { get; init; } = 32_768;

    public static bool IsValid(IdempotencyOptions options) =>
        options.ClaimMinutes is >= 1 and <= 1_440
        && options.RetentionDays is >= 1 and <= 365
        && options.CleanupIntervalMinutes is >= 1 and <= 1_440
        && options.CleanupBatchSize is >= 1 and <= 5_000
        && options.MaximumKeyLength is >= 16 and <= 1_024
        && options.MaximumOutcomeBytes is >= 1_024 and <= 1_048_576;
}
