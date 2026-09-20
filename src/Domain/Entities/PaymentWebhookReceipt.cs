using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Payments;

public sealed class PaymentWebhookReceipt : BaseAuditableEntity
{
    private PaymentWebhookReceipt() { }

    public string Provider { get; private set; } = string.Empty;
    public string ProviderEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }

    public static PaymentWebhookReceipt Record(
        string provider,
        string eventId,
        string eventType,
        string payloadHash,
        DateTimeOffset receivedAt
    )
    {
        if (
            string.IsNullOrWhiteSpace(provider)
            || string.IsNullOrWhiteSpace(eventId)
            || string.IsNullOrWhiteSpace(eventType)
            || string.IsNullOrWhiteSpace(payloadHash)
        )
            throw new DomainInvariantException("Webhook receipt values are required.");
        return new PaymentWebhookReceipt
        {
            Provider = provider.Trim(),
            ProviderEventId = eventId.Trim(),
            EventType = eventType.Trim(),
            PayloadHash = payloadHash,
            ReceivedAt = receivedAt,
        };
    }
}
