using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Payouts;

public sealed class PayoutRequest : OrganizationEntity
{
    private PayoutRequest() { }

    public Guid AgentId { get; private set; }
    public Guid PayoutAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PayoutStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? ProviderBatchId { get; private set; }
    public string? ProviderTransferId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    public static PayoutRequest Request(
        Guid organizationId,
        Guid agentId,
        Guid accountId,
        decimal amount,
        string currency,
        DateTimeOffset requestedAt
    )
    {
        if (
            organizationId == Guid.Empty
            || agentId == Guid.Empty
            || accountId == Guid.Empty
            || amount <= 0
            || currency.Length != 3
        )
            throw new DomainInvariantException("Payout request values are invalid.");
        var payout = new PayoutRequest
        {
            OrganizationId = organizationId,
            AgentId = agentId,
            PayoutAccountId = accountId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            RequestedAt = requestedAt,
            Status = PayoutStatus.Requested,
        };
        payout.AddDomainEvent(new PayoutRequestedEvent(payout.OrganizationId, payout.Id));
        return payout;
    }

    public void StartReview()
    {
        EnsureStatus(PayoutStatus.Requested);
        Status = PayoutStatus.UnderReview;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        EnsureStatus(PayoutStatus.UnderReview);
        Status = PayoutStatus.Approved;
        ApprovedAt = approvedAt;
        AddDomainEvent(
            new PayoutApprovedEvent(OrganizationId, Id, AgentId, Amount, Currency, approvedAt)
        );
    }

    public void Reject()
    {
        EnsureStatus(PayoutStatus.UnderReview);
        Status = PayoutStatus.Rejected;
    }

    public void StartProcessing()
    {
        EnsureStatus(PayoutStatus.Approved);
        Status = PayoutStatus.Processing;
    }

    public void MarkPaid(string providerReference, DateTimeOffset processedAt)
    {
        EnsureStatus(PayoutStatus.Processing);
        Status = PayoutStatus.Paid;
        ProviderReference = providerReference;
        ProcessedAt = processedAt;
        AddDomainEvent(
            new PayoutCompletedEvent(
                OrganizationId,
                Id,
                AgentId,
                Amount,
                Currency,
                providerReference,
                processedAt
            )
        );
    }

    public void AttachProviderTransfer(string batchId, string transferId)
    {
        EnsureStatus(PayoutStatus.Processing);
        if (string.IsNullOrWhiteSpace(transferId))
            throw new DomainInvariantException("Provider transfer ID is required.");
        ProviderBatchId = string.IsNullOrWhiteSpace(batchId) ? null : batchId.Trim();
        ProviderTransferId = transferId.Trim();
    }

    public void MarkFailed(
        string? providerReference = null,
        string? failureCode = null,
        string? failureMessage = null
    )
    {
        EnsureStatus(PayoutStatus.Processing);
        Status = PayoutStatus.Failed;
        ProviderReference = providerReference;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        AddDomainEvent(
            new PayoutFailedEvent(
                OrganizationId,
                Id,
                AgentId,
                Amount,
                Currency,
                providerReference,
                failureCode
            )
        );
    }

    public void Cancel()
    {
        if (
            Status
            is not (PayoutStatus.Requested or PayoutStatus.UnderReview or PayoutStatus.Approved)
        )
            throw new DomainInvariantException("Only an unsubmitted payout can be cancelled.");
        Status = PayoutStatus.Cancelled;
    }

    private void EnsureStatus(PayoutStatus expected)
    {
        if (Status != expected)
            throw new DomainInvariantException($"Payout must be {expected}.");
    }
}
