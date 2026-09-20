using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Compensation;

public sealed class BinaryPairingRun : OrganizationEntity
{
    private BinaryPairingRun() { }

    public Guid AgentId { get; private set; }
    public Guid CommissionPlanId { get; private set; }
    public int CommissionPlanVersion { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public bool QualificationPassed { get; private set; }
    public string? QualificationFailureReason { get; private set; }
    public decimal LeftBefore { get; private set; }
    public decimal RightBefore { get; private set; }
    public decimal MatchedVolume { get; private set; }
    public decimal LeftConsumed { get; private set; }
    public decimal RightConsumed { get; private set; }
    public decimal LeftAfter { get; private set; }
    public decimal RightAfter { get; private set; }
    public decimal GrossCommission { get; private set; }
    public decimal CappedAmount { get; private set; }
    public decimal NetCommission { get; private set; }
    public bool CapApplied { get; private set; }
    public BinaryPairingRunStatus Status { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    public static BinaryPairingRun Create(
        Guid organizationId,
        Guid agentId,
        Guid commissionPlanId,
        int commissionPlanVersion,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string idempotencyKey
    )
    {
        if (
            organizationId == Guid.Empty
            || agentId == Guid.Empty
            || commissionPlanId == Guid.Empty
            || commissionPlanVersion < 1
            || string.IsNullOrWhiteSpace(idempotencyKey)
        )
            throw new DomainInvariantException("Pairing run identity is required.");
        if (periodStart >= periodEnd)
            throw new DomainInvariantException("Period start must be before period end.");

        return new BinaryPairingRun
        {
            OrganizationId = organizationId,
            AgentId = agentId,
            CommissionPlanId = commissionPlanId,
            CommissionPlanVersion = commissionPlanVersion,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = BinaryPairingRunStatus.Pending,
        };
    }

    public void Complete(
        decimal leftBefore,
        decimal rightBefore,
        decimal matchedVolume,
        decimal leftConsumed,
        decimal rightConsumed,
        decimal grossCommission,
        decimal cappedAmount,
        DateTimeOffset processedAt
    )
    {
        EnsurePending();
        ValidateResult(
            leftBefore,
            rightBefore,
            matchedVolume,
            leftConsumed,
            rightConsumed,
            grossCommission,
            cappedAmount
        );

        QualificationPassed = true;
        QualificationFailureReason = null;
        LeftBefore = leftBefore;
        RightBefore = rightBefore;
        MatchedVolume = matchedVolume;
        LeftConsumed = leftConsumed;
        RightConsumed = rightConsumed;
        LeftAfter = leftBefore - leftConsumed;
        RightAfter = rightBefore - rightConsumed;
        GrossCommission = grossCommission;
        CappedAmount = cappedAmount;
        NetCommission = grossCommission - cappedAmount;
        CapApplied = cappedAmount > 0m;
        ProcessedAt = processedAt;
        Status = BinaryPairingRunStatus.Completed;
    }

    public void Skip(
        string reason,
        decimal leftBefore,
        decimal rightBefore,
        DateTimeOffset processedAt,
        bool qualificationPassed = false
    )
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainInvariantException("A skipped pairing run requires a reason.");
        if (leftBefore < 0m || rightBefore < 0m)
            throw new DomainInvariantException("Pairing volume cannot be negative.");

        QualificationPassed = qualificationPassed;
        QualificationFailureReason = reason.Trim();
        LeftBefore = leftBefore;
        RightBefore = rightBefore;
        LeftAfter = leftBefore;
        RightAfter = rightBefore;
        ProcessedAt = processedAt;
        Status = BinaryPairingRunStatus.Skipped;
    }

    public void Fail(string reason, DateTimeOffset processedAt)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainInvariantException("A failed pairing run requires a reason.");

        QualificationPassed = false;
        QualificationFailureReason = reason.Trim();
        ProcessedAt = processedAt;
        Status = BinaryPairingRunStatus.Failed;
    }

    private void EnsurePending()
    {
        if (Status != BinaryPairingRunStatus.Pending)
            throw new DomainInvariantException("Only a pending pairing run can be finalized.");
    }

    private static void ValidateResult(
        decimal leftBefore,
        decimal rightBefore,
        decimal matchedVolume,
        decimal leftConsumed,
        decimal rightConsumed,
        decimal grossCommission,
        decimal cappedAmount
    )
    {
        if (
            leftBefore < 0m
            || rightBefore < 0m
            || matchedVolume < 0m
            || leftConsumed < 0m
            || rightConsumed < 0m
            || grossCommission < 0m
            || cappedAmount < 0m
        )
            throw new DomainInvariantException("Pairing result values cannot be negative.");
        if (matchedVolume > Math.Min(leftBefore, rightBefore))
            throw new DomainInvariantException("Matched volume exceeds available leg volume.");
        if (leftConsumed > leftBefore || rightConsumed > rightBefore)
            throw new DomainInvariantException("Consumed volume exceeds available leg volume.");
        if (leftConsumed > matchedVolume || rightConsumed > matchedVolume)
            throw new DomainInvariantException("Consumed volume exceeds matched volume.");
        if (cappedAmount > grossCommission)
            throw new DomainInvariantException("Capped amount exceeds gross commission.");
    }
}
