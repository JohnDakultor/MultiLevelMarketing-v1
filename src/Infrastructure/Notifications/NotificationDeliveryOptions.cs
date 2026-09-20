namespace modular_mlm.Infrastructure.Notifications;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    public bool Enabled { get; init; } = true;
    public int PollSeconds { get; init; } = 5;
    public int MaximumAttempts { get; init; } = 8;
    public int InitialRetrySeconds { get; init; } = 15;
    public int MaximumRetrySeconds { get; init; } = 3_600;
    public int BatchSize { get; init; } = 50;
    public int ClaimSeconds { get; init; } = 120;
    public string FromDisplayName { get; init; } = "Modular MLM";

    public static bool IsValid(NotificationDeliveryOptions value) =>
        value.PollSeconds is >= 1 and <= 300
        && value.MaximumAttempts is >= 1 and <= 50
        && value.InitialRetrySeconds is >= 1 and <= 3_600
        && value.MaximumRetrySeconds >= value.InitialRetrySeconds
        && value.MaximumRetrySeconds <= 86_400
        && value.BatchSize is >= 1 and <= 500
        && value.ClaimSeconds is >= 10 and <= 3_600
        && !string.IsNullOrWhiteSpace(value.FromDisplayName)
        && value.FromDisplayName.Length <= 100;
}
