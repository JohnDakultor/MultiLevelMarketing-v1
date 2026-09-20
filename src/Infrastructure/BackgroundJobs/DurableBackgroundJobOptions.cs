namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class DurableBackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs:Durable";
    public bool Enabled { get; init; } = true;
    public int PollSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 25;
    public int ClaimSeconds { get; init; } = 300;
    public int MaximumAttempts { get; init; } = 8;
    public int InitialRetrySeconds { get; init; } = 30;
    public int MaximumRetrySeconds { get; init; } = 3_600;
    public int DeadLetterAlertThreshold { get; init; } = 1;

    public static bool IsValid(DurableBackgroundJobOptions value) =>
        value.PollSeconds is >= 1 and <= 300
        && value.BatchSize is >= 1 and <= 500
        && value.ClaimSeconds is >= 30 and <= 3_600
        && value.MaximumAttempts is >= 1 and <= 50
        && value.InitialRetrySeconds is >= 1 and <= 3_600
        && value.MaximumRetrySeconds >= value.InitialRetrySeconds
        && value.MaximumRetrySeconds <= 86_400
        && value.DeadLetterAlertThreshold is >= 1 and <= 50;
}
