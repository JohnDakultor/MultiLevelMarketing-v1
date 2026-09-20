namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxDispatchOptions
{
    public const string SectionName = "BackgroundJobs:Outbox";

    public bool Enabled { get; init; } = true;
    public int PollSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 50;
    public int ClaimSeconds { get; init; } = 120;
    public int MaximumAttempts { get; init; } = 8;
    public int InitialRetrySeconds { get; init; } = 10;
    public int MaximumRetrySeconds { get; init; } = 3_600;
    public int JitterPercentage { get; init; } = 20;

    public static bool IsValid(OutboxDispatchOptions value) =>
        value.PollSeconds is >= 1 and <= 300
        && value.BatchSize is >= 1 and <= 500
        && value.ClaimSeconds is >= 10 and <= 3_600
        && value.MaximumAttempts is >= 1 and <= 50
        && value.InitialRetrySeconds is >= 1 and <= 3_600
        && value.MaximumRetrySeconds >= value.InitialRetrySeconds
        && value.MaximumRetrySeconds <= 86_400
        && value.JitterPercentage is >= 0 and <= 100;
}
