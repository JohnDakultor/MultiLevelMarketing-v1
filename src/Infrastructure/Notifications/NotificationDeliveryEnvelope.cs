using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Notifications;

public sealed class NotificationDeliveryEnvelope
{
    private NotificationDeliveryEnvelope() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? NotificationId { get; private set; }
    public Guid? InvitationId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string TemplateKey { get; private set; } = string.Empty;
    public string ProtectedPayload { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public string ProviderIdempotencyKey { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }
    public DateTimeOffset? ClaimedUntil { get; private set; }
    public string? ClaimedBy { get; private set; }
    public string? FailureCode { get; private set; }

    public static NotificationDeliveryEnvelope Stage(
        Guid organizationId,
        Guid? notificationId,
        Guid? invitationId,
        NotificationChannel channel,
        string templateKey,
        string protectedPayload,
        string payloadHash,
        string providerIdempotencyKey,
        DateTimeOffset nextAttemptAt,
        DateTimeOffset? expiresAt = null
    )
    {
        if (organizationId == Guid.Empty || notificationId.HasValue == invitationId.HasValue)
            throw new ArgumentException("Exactly one delivery source is required.");
        if (expiresAt <= nextAttemptAt)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new NotificationDeliveryEnvelope
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NotificationId = notificationId,
            InvitationId = invitationId,
            Channel = channel,
            TemplateKey = Required(templateKey),
            ProtectedPayload = Required(protectedPayload),
            PayloadHash = Required(payloadHash),
            ProviderIdempotencyKey = Required(providerIdempotencyKey),
            NextAttemptAt = nextAttemptAt,
            ExpiresAt = expiresAt,
        };
    }

    public void Claim(string workerId, DateTimeOffset until)
    {
        if (DeliveredAt.HasValue || DeadLetteredAt.HasValue || ReadyAt is null)
            throw new InvalidOperationException("Delivery cannot be claimed.");
        ClaimedBy = Required(workerId);
        ClaimedUntil = until;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        DeliveredAt = now;
        ClaimedBy = null;
        ClaimedUntil = null;
        FailureCode = null;
        ProtectedPayload = string.Empty;
    }

    public void MarkFailed(
        string failureCode,
        bool permanent,
        DateTimeOffset now,
        int maximumAttempts,
        TimeSpan retryDelay
    )
    {
        AttemptCount++;
        FailureCode = Required(failureCode).ToUpperInvariant();
        ClaimedBy = null;
        ClaimedUntil = null;
        if (permanent || AttemptCount >= maximumAttempts)
        {
            DeadLetteredAt = now;
            ProtectedPayload = string.Empty;
        }
        else
        {
            NextAttemptAt = now.Add(retryDelay);
        }
    }

    public void Expire(DateTimeOffset now) =>
        MarkFailed("DELIVERY_EXPIRED", true, now, 1, TimeSpan.Zero);

    public void Restage(
        string templateKey,
        string protectedPayload,
        string payloadHash,
        string providerIdempotencyKey,
        DateTimeOffset nextAttemptAt,
        DateTimeOffset expiresAt
    )
    {
        if (ClaimedUntil > nextAttemptAt)
            throw new InvalidOperationException("A claimed delivery cannot be replaced.");
        if (expiresAt <= nextAttemptAt)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        TemplateKey = Required(templateKey);
        ProtectedPayload = Required(protectedPayload);
        PayloadHash = Required(payloadHash);
        ProviderIdempotencyKey = Required(providerIdempotencyKey);
        AttemptCount = 0;
        ReadyAt = null;
        NextAttemptAt = nextAttemptAt;
        ExpiresAt = expiresAt;
        DeliveredAt = null;
        DeadLetteredAt = null;
        ClaimedUntil = null;
        ClaimedBy = null;
        FailureCode = null;
    }

    internal const string ProtectionPurpose =
        "modular_mlm.Infrastructure.Notifications.DeliveryPayload.v1";

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.")
            : value.Trim();
}

internal sealed record NotificationDeliveryPayload(
    string RecipientUserId,
    string RecipientAddress,
    string Culture,
    IReadOnlyDictionary<string, string> Variables
);
