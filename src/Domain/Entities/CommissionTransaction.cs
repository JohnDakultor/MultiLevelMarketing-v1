using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Compensation;

public sealed class CommissionTransaction : OrganizationEntity
{
    private CommissionTransaction() { }

    public Guid BeneficiaryAgentId { get; private set; }
    public Guid? SourceOrderId { get; private set; }
    public Guid? SourceOrderItemId { get; private set; }
    public Guid? SourceAgentId { get; private set; }
    public Guid? PairingRunId { get; private set; }
    public Guid CommissionPlanVersionId { get; private set; }
    public string RuleId { get; private set; } = string.Empty;
    public CommissionType Type { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal? Rate { get; private set; }
    public decimal Amount { get; private set; }
    public CommissionStatus Status { get; private set; }
    public DateTimeOffset? AvailableAt { get; private set; }
    public Guid? ReversalOfCommissionId { get; private set; }
    public Guid? SourceOrderItemRefundId { get; private set; }

    public static CommissionTransaction Create(
        Guid organizationId,
        Guid beneficiaryId,
        Guid orderId,
        Guid planId,
        string ruleId,
        CommissionType type,
        decimal baseAmount,
        decimal? rate,
        decimal amount
    )
    {
        if (amount < 0 || baseAmount < 0 || string.IsNullOrWhiteSpace(ruleId))
            throw new DomainInvariantException("Commission values are invalid.");
        var transaction = new CommissionTransaction
        {
            OrganizationId = organizationId,
            BeneficiaryAgentId = beneficiaryId,
            SourceOrderId = orderId,
            CommissionPlanVersionId = planId,
            RuleId = ruleId,
            Type = type,
            BaseAmount = baseAmount,
            Rate = rate,
            Amount = amount,
            Status = CommissionStatus.Pending,
        };
        transaction.AddDomainEvent(new CommissionCreatedEvent(transaction.Id));
        return transaction;
    }

    public static CommissionTransaction CreateDirectSale(
        Guid organizationId,
        Guid beneficiaryId,
        Guid orderId,
        Guid orderItemId,
        Guid planId,
        string ruleId,
        decimal baseAmount,
        decimal rate,
        decimal amount
    )
    {
        var transaction = Create(
            organizationId,
            beneficiaryId,
            orderId,
            planId,
            ruleId,
            CommissionType.DirectSale,
            baseAmount,
            rate,
            amount
        );
        transaction.SourceOrderItemId = orderItemId;
        transaction.SourceAgentId = beneficiaryId;
        return transaction;
    }

    public static CommissionTransaction CreateBinaryPairing(
        Guid organizationId,
        Guid beneficiaryId,
        Guid pairingRunId,
        Guid planId,
        string ruleId,
        decimal baseAmount,
        decimal? rate,
        decimal amount
    )
    {
        if (pairingRunId == Guid.Empty)
            throw new DomainInvariantException("Pairing run is required.");

        var transaction = Create(
            organizationId,
            beneficiaryId,
            Guid.Empty,
            planId,
            ruleId,
            CommissionType.BinaryPairing,
            baseAmount,
            rate,
            amount
        );
        transaction.SourceOrderId = null;
        transaction.PairingRunId = pairingRunId;
        return transaction;
    }

    public void Release(DateTimeOffset availableAt)
    {
        if (Status != CommissionStatus.Pending)
            throw new DomainInvariantException("Only pending commission can be released.");
        Status = CommissionStatus.Available;
        AvailableAt = availableAt;
        AddDomainEvent(
            new CommissionReleasedEvent(OrganizationId, Id, BeneficiaryAgentId, availableAt)
        );
    }

    public void Hold()
    {
        if (Status is CommissionStatus.Paid or CommissionStatus.Reversed)
            throw new DomainInvariantException("Finalized commission cannot be held.");
        Status = CommissionStatus.Held;
    }

    public void MarkPaid()
    {
        if (Status != CommissionStatus.Available)
            throw new DomainInvariantException("Only available commission can be paid.");
        Status = CommissionStatus.Paid;
    }

    public CommissionTransaction Reverse()
    {
        if (Status == CommissionStatus.Reversed)
            throw new DomainInvariantException("Commission is already reversed.");
        Status = CommissionStatus.Reversed;
        return new CommissionTransaction
        {
            OrganizationId = OrganizationId,
            BeneficiaryAgentId = BeneficiaryAgentId,
            SourceOrderId = SourceOrderId,
            SourceOrderItemId = SourceOrderItemId,
            SourceAgentId = SourceAgentId,
            PairingRunId = PairingRunId,
            CommissionPlanVersionId = CommissionPlanVersionId,
            RuleId = $"{RuleId}:reversal",
            Type = CommissionType.Reversal,
            BaseAmount = BaseAmount,
            Amount = -Amount,
            Status = CommissionStatus.Available,
            ReversalOfCommissionId = Id,
        };
    }

    public CommissionTransaction ReverseForRefund(
        Guid orderItemRefundId,
        decimal baseAmount,
        decimal amount,
        bool fullyReversed
    )
    {
        if (orderItemRefundId == Guid.Empty || baseAmount < 0m || amount <= 0m || amount > Amount)
            throw new DomainInvariantException("Refund reversal values are invalid.");
        if (Type == CommissionType.Reversal)
            throw new DomainInvariantException(
                "A reversal cannot be reversed as a source commission."
            );

        if (fullyReversed)
            Status = CommissionStatus.Reversed;

        return new CommissionTransaction
        {
            OrganizationId = OrganizationId,
            BeneficiaryAgentId = BeneficiaryAgentId,
            SourceOrderId = SourceOrderId,
            SourceOrderItemId = SourceOrderItemId,
            SourceOrderItemRefundId = orderItemRefundId,
            SourceAgentId = SourceAgentId,
            PairingRunId = PairingRunId,
            CommissionPlanVersionId = CommissionPlanVersionId,
            RuleId = $"{RuleId}:refund:{orderItemRefundId:N}",
            Type = CommissionType.Reversal,
            BaseAmount = baseAmount,
            Rate = Rate,
            Amount = -amount,
            Status = CommissionStatus.Available,
            ReversalOfCommissionId = Id,
        };
    }
}
