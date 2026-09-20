namespace modular_mlm.Infrastructure.Messaging;

public sealed class WebhookProcessingAttempt
{
    private WebhookProcessingAttempt() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? OrganizationId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string ProviderResourceId { get; private set; } = string.Empty;
    public string ResourceKind { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string? LastError { get; private set; }

    public static WebhookProcessingAttempt Create(
        Guid? organizationId,
        string provider,
        string providerEventId,
        string eventType,
        string providerResourceId,
        string resourceKind,
        string payloadHash,
        DateTimeOffset receivedAt
    ) =>
        new()
        {
            OrganizationId = organizationId,
            Provider = provider.Trim(),
            ProviderEventId = providerEventId.Trim(),
            EventType = eventType.Trim(),
            ProviderResourceId = providerResourceId.Trim(),
            ResourceKind = resourceKind.Trim(),
            PayloadHash = payloadHash.Trim(),
            ReceivedAt = receivedAt,
            NextAttemptAt = receivedAt,
        };

    public void MarkSucceeded(DateTimeOffset now)
    {
        AttemptCount++;
        LastAttemptAt = now;
        Status = "Succeeded";
        LastError = null;
    }

    public void MarkFailed(string error, DateTimeOffset now, int maximumAttempts)
    {
        AttemptCount++;
        LastAttemptAt = now;
        LastError = error.Length <= 2_000 ? error : error[..2_000];
        Status = AttemptCount >= maximumAttempts ? "DeadLettered" : "Failed";
        NextAttemptAt = now + TimeSpan.FromSeconds(Math.Min(3_600, Math.Pow(2, AttemptCount) * 10));
    }
}
