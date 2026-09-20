using System.Text.Json;
using modular_mlm.Domain.Common;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? OrganizationId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string? CorrelationId { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }
    public DateTimeOffset? ClaimedUntil { get; private set; }
    public string? ClaimedBy { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage Create(
        BaseEvent domainEvent,
        DateTimeOffset occurredAt,
        string? correlationId
    ) =>
        new()
        {
            OrganizationId = TryGetOrganizationId(domainEvent),
            OccurredAt = occurredAt,
            MessageType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            CorrelationId = correlationId,
            NextAttemptAt = occurredAt,
        };

    public void Claim(string workerId, DateTimeOffset claimedUntil)
    {
        ClaimedBy = workerId;
        ClaimedUntil = claimedUntil;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
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
        else
            NextAttemptAt = now + retryDelay;
        return false;
    }

    public void Replay(DateTimeOffset replayedAt)
    {
        if (DeadLetteredAt is null)
            throw new InvalidOperationException("Only a dead-letter message can be replayed.");
        DeadLetteredAt = null;
        ClaimedBy = null;
        ClaimedUntil = null;
        LastError = null;
        NextAttemptAt = replayedAt;
    }

    private static Guid? TryGetOrganizationId(BaseEvent domainEvent)
    {
        var value = domainEvent.GetType().GetProperty("OrganizationId")?.GetValue(domainEvent);
        return value is Guid id && id != Guid.Empty ? id : null;
    }

    private static string Sanitize(string error) => error.Length <= 2_000 ? error : error[..2_000];
}
