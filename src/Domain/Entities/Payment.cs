using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Payments;

public sealed class Payment : OrganizationEntity
{
    private Payment() { }

    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? ProviderCheckoutSessionId { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public Uri? CheckoutUrl { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? CompensationProcessedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    public static Payment Initiate(
        Guid organizationId,
        Guid orderId,
        string provider,
        string idempotencyKey,
        decimal amount,
        string currency
    )
    {
        if (
            organizationId == Guid.Empty
            || orderId == Guid.Empty
            || string.IsNullOrWhiteSpace(provider)
            || string.IsNullOrWhiteSpace(idempotencyKey)
            || amount <= 0m
            || currency.Length != 3
        )
            throw new DomainInvariantException("Payment initiation values are invalid.");

        return new Payment
        {
            OrganizationId = organizationId,
            OrderId = orderId,
            Provider = provider.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Status = PaymentStatus.Pending,
        };
    }

    public void AttachCheckoutSession(string sessionId, Uri checkoutUrl)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainInvariantException(
                "Only pending payments can attach a checkout session."
            );
        if (string.IsNullOrWhiteSpace(sessionId) || !checkoutUrl.IsAbsoluteUri)
            throw new DomainInvariantException("A valid provider checkout session is required.");

        ProviderCheckoutSessionId = sessionId.Trim();
        CheckoutUrl = checkoutUrl;
    }

    public void MarkPaid(string? providerPaymentId, DateTimeOffset paidAt)
    {
        if (Status == PaymentStatus.Paid)
            return;
        if (Status is PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded)
            throw new DomainInvariantException("A refunded payment cannot be marked paid.");

        ProviderPaymentId = string.IsNullOrWhiteSpace(providerPaymentId)
            ? ProviderPaymentId
            : providerPaymentId.Trim();
        PaidAt = paidAt;
        Status = PaymentStatus.Paid;
        FailureCode = null;
        FailureMessage = null;
    }

    public void MarkCompensationProcessed(DateTimeOffset processedAt)
    {
        if (Status != PaymentStatus.Paid)
            throw new DomainInvariantException("Only paid payments can complete compensation.");
        CompensationProcessedAt ??= processedAt;
    }

    public void MarkFailed(string? code, string? message)
    {
        if (Status == PaymentStatus.Paid)
            throw new DomainInvariantException("A paid payment cannot be marked failed.");
        Status = PaymentStatus.Failed;
        FailureCode = code;
        FailureMessage = message;
        AddDomainEvent(new PaymentFailedEvent(OrganizationId, Id));
    }

    public void ReconcileRefundedAmount(decimal refundedAmount)
    {
        if (
            Status
            is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded)
        )
            throw new DomainInvariantException("Only a paid payment can be refunded.");
        if (refundedAmount < RefundedAmount || refundedAmount > Amount)
            throw new DomainInvariantException("The reconciled refund amount is invalid.");

        RefundedAmount = refundedAmount;
        Status =
            refundedAmount == Amount ? PaymentStatus.Refunded
            : refundedAmount > 0m ? PaymentStatus.PartiallyRefunded
            : PaymentStatus.Paid;
    }
}
