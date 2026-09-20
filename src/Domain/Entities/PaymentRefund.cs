using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Payments;

public sealed class PaymentRefund : OrganizationEntity
{
    private PaymentRefund() { }

    public Guid PaymentId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? ProviderRefundId { get; private set; }
    public PaymentRefundStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureMessage { get; private set; }

    public static PaymentRefund Request(
        Guid organizationId,
        Guid paymentId,
        Guid orderId,
        decimal amount,
        string currency,
        string reason,
        DateTimeOffset requestedAt
    )
    {
        if (
            organizationId == Guid.Empty
            || paymentId == Guid.Empty
            || orderId == Guid.Empty
            || amount <= 0m
            || currency.Length != 3
            || string.IsNullOrWhiteSpace(reason)
        )
            throw new DomainInvariantException("Refund values are invalid.");
        var refund = new PaymentRefund
        {
            OrganizationId = organizationId,
            PaymentId = paymentId,
            OrderId = orderId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Reason = reason.Trim(),
            RequestedAt = requestedAt,
            Status = PaymentRefundStatus.Pending,
        };
        refund.IdempotencyKey = $"paymongo-refund:{refund.Id:N}";
        return refund;
    }

    public void AttachProviderResult(
        string providerRefundId,
        string providerStatus,
        DateTimeOffset now
    )
    {
        if (string.IsNullOrWhiteSpace(providerRefundId))
            throw new DomainInvariantException("Provider refund ID is required.");
        ProviderRefundId = providerRefundId.Trim();
        if (string.Equals(providerStatus, "succeeded", StringComparison.OrdinalIgnoreCase))
            MarkSucceeded(now);
        else if (string.Equals(providerStatus, "failed", StringComparison.OrdinalIgnoreCase))
            MarkFailed("The payment provider rejected the refund.", now);
    }

    public void MarkSucceeded(DateTimeOffset completedAt)
    {
        Status = PaymentRefundStatus.Succeeded;
        CompletedAt = completedAt;
        FailureMessage = null;
    }

    public void MarkFailed(string message, DateTimeOffset completedAt)
    {
        Status = PaymentRefundStatus.Failed;
        FailureMessage = message;
        CompletedAt = completedAt;
    }
}
