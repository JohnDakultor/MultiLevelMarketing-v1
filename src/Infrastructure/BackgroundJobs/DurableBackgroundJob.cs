using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class DurableBackgroundJob
{
    private DurableBackgroundJob() { }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string JobName { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public int Attempts { get; private set; }
    public string? ClaimedBy { get; private set; }
    public DateTimeOffset? ClaimedUntil { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }
    public string? LastError { get; private set; }

    public static DurableBackgroundJob Create(
        Guid? organizationId,
        string jobName,
        string payloadJson,
        string idempotencyKey,
        Guid correlationId,
        DateTimeOffset createdAt,
        DateTimeOffset? notBefore = null
    )
    {
        var normalizedName = DurableJobRegistry.Normalize(jobName);
        if (string.IsNullOrWhiteSpace(payloadJson) || payloadJson.Length > 65_536)
            throw new ArgumentException("A bounded job payload is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            throw new ArgumentException("A bounded idempotency key is required.");
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation ID is required.");
        if (notBefore < createdAt)
            throw new ArgumentOutOfRangeException(nameof(notBefore));

        return new DurableBackgroundJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            JobName = normalizedName,
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey.Trim(),
            CorrelationId = correlationId,
            CreatedAt = createdAt,
            NextAttemptAt = notBefore ?? createdAt,
        };
    }

    public void MarkCompleted(DateTimeOffset now)
    {
        CompletedAt = now;
        ClaimedBy = null;
        ClaimedUntil = null;
        LastError = null;
    }

    public bool MarkFailed(
        string error,
        DateTimeOffset now,
        int maximumAttempts,
        TimeSpan retryDelay
    )
    {
        Attempts++;
        LastError = Sanitize(error);
        ClaimedBy = null;
        ClaimedUntil = null;
        if (Attempts >= maximumAttempts)
        {
            DeadLetteredAt = now;
            return true;
        }
        NextAttemptAt = now.Add(retryDelay);
        return false;
    }

    public void Replay(DateTimeOffset now)
    {
        if (DeadLetteredAt is null)
            throw new InvalidOperationException("Only dead-lettered jobs may be replayed.");
        DeadLetteredAt = null;
        ClaimedBy = null;
        ClaimedUntil = null;
        LastError = null;
        NextAttemptAt = now;
    }

    private static string Sanitize(string value)
    {
        var safe = string.IsNullOrWhiteSpace(value) ? "UNSPECIFIED_FAILURE" : value;
        safe = safe.Replace("\r", " ").Replace("\n", " ");
        return safe.Length <= 2_000 ? safe : safe[..2_000];
    }
}
