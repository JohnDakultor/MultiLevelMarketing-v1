using System.Text.Json;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Network.Services;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;

public sealed class ProcessBinaryPairingCommandHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<ProcessBinaryPairingCommand, Guid>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Guid> Handle(
        ProcessBinaryPairingCommand request,
        CancellationToken cancellationToken
    )
    {
        var existingRunId = await db
            .BinaryPairingRuns.AsNoTracking()
            .Where(run =>
                run.OrganizationId == request.OrganizationId
                && run.IdempotencyKey == request.IdempotencyKey
            )
            .Select(run => (Guid?)run.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingRunId.HasValue)
            return existingRunId.Value;

        var plan = await db.CommissionPlans.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.CommissionPlanId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (plan is null)
            throw new KeyNotFoundException("Commission plan was not found.");
        if (
            plan.Status != CommissionPlanStatus.Active
            || plan.EffectiveFrom >= request.PeriodEnd
            || (plan.EffectiveTo.HasValue && plan.EffectiveTo <= request.PeriodStart)
        )
            throw new InvalidOperationException("Commission plan is not active for this period.");
        if (!plan.BinaryPairing.Enabled)
            throw new InvalidOperationException("Binary pairing is disabled for this plan.");

        var agent = await db.Agents.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.AgentId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent was not found in this organization.");

        var balance = await db.BinaryVolumeBalances.SingleOrDefaultAsync(
            candidate =>
                candidate.AgentId == request.AgentId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (balance is null)
            throw new KeyNotFoundException("Binary volume balance was not found.");

        var now = clock.GetUtcNow();
        var run = BinaryPairingRun.Create(
            request.OrganizationId,
            request.AgentId,
            plan.Id,
            plan.Version,
            request.PeriodStart,
            request.PeriodEnd,
            request.IdempotencyKey
        );
        var qualification = await EvaluateQualificationAsync(
            request,
            agent,
            ReadQualificationRules(plan.QualificationRulesJson),
            cancellationToken
        );
        if (!qualification.Passed)
        {
            run.Skip(
                qualification.FailureReason ?? "Qualification requirements were not met.",
                balance.LeftAvailable,
                balance.RightAvailable,
                now
            );
            db.BinaryPairingRuns.Add(run);
            await db.SaveChangesAsync(cancellationToken);
            return run.Id;
        }

        var calculation = BinaryPairingCalculator.Calculate(
            balance.LeftAvailable,
            balance.RightAvailable,
            plan.BinaryPairing
        );
        if (calculation.MatchedVolume == 0m)
        {
            run.Skip(
                "No matchable binary volume was available.",
                balance.LeftAvailable,
                balance.RightAvailable,
                now,
                qualificationPassed: true
            );
            db.BinaryPairingRuns.Add(run);
            await db.SaveChangesAsync(cancellationToken);
            return run.Id;
        }

        var capRules = ReadCapRules(plan.CapRulesJson);
        var previouslyEarned = capRules.Enabled
            ? await db
                .CommissionTransactions.AsNoTracking()
                .Where(transaction =>
                    transaction.OrganizationId == request.OrganizationId
                    && transaction.BeneficiaryAgentId == request.AgentId
                    && transaction.Type == CommissionType.BinaryPairing
                    && transaction.Created >= request.PeriodStart
                    && transaction.Created < request.PeriodEnd
                    && transaction.Status != CommissionStatus.Reversed
                )
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
                ?? 0m
            : 0m;
        var cap = new CommissionCapEvaluator().Evaluate(
            new CommissionCapInput(
                calculation.GrossCommission,
                previouslyEarned,
                capRules.MaximumAmount,
                capRules.Enabled
            )
        );
        var preserveVolume = string.Equals(
            plan.BinaryPairing.CapPolicy,
            "PreserveVolume",
            StringComparison.OrdinalIgnoreCase
        );
        var leftConsumed =
            preserveVolume && cap.CapApplied
                ? TruncateVolume(calculation.LeftConsumed * cap.PayableRatio)
                : calculation.LeftConsumed;
        var rightConsumed =
            preserveVolume && cap.CapApplied
                ? TruncateVolume(calculation.RightConsumed * cap.PayableRatio)
                : calculation.RightConsumed;

        balance.Consume(leftConsumed, rightConsumed);
        AddConsumptionEntries(request, run.Id, leftConsumed, rightConsumed, now);
        if (cap.PayableCommission > 0m)
            await AddCommissionAndWalletCreditAsync(
                request,
                plan,
                run.Id,
                calculation.MatchedVolume,
                cap.PayableCommission,
                cancellationToken
            );

        run.Complete(
            calculation.LeftRemaining + calculation.LeftConsumed,
            calculation.RightRemaining + calculation.RightConsumed,
            calculation.MatchedVolume,
            leftConsumed,
            rightConsumed,
            calculation.GrossCommission,
            cap.CappedAmount,
            now
        );
        db.BinaryPairingRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return run.Id;
    }

    private async Task<QualificationResult> EvaluateQualificationAsync(
        ProcessBinaryPairingCommand request,
        Agent agent,
        QualificationRules rules,
        CancellationToken cancellationToken
    )
    {
        var paidItems =
            from item in db.OrderItems.AsNoTracking()
            join order in db.Orders.AsNoTracking() on item.OrderId equals order.Id
            where
                order.OrganizationId == request.OrganizationId
                && order.AttributedAgentId == request.AgentId
                && order.PaymentStatus == PaymentStatus.Paid
                && order.PaidAt >= request.PeriodStart
                && order.PaidAt < request.PeriodEnd
            select item;
        var personalSales =
            await paidItems.SumAsync(item => (decimal?)item.CommissionableAmount, cancellationToken)
            ?? 0m;
        var personalBv =
            await paidItems.SumAsync(item => (decimal?)item.BusinessVolume, cancellationToken)
            ?? 0m;
        var activeDirects = await db.Agents.CountAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.SponsorAgentId == request.AgentId
                && candidate.Status == AgentStatus.Active,
            cancellationToken
        );
        var activeLegs = await db
            .Agents.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.PlacementParentAgentId == request.AgentId
                && candidate.Status == AgentStatus.Active
            )
            .Select(candidate => candidate.PlacementSide)
            .ToListAsync(cancellationToken);

        return new QualificationEvaluator().Evaluate(
            new QualificationInput(
                agent.Status,
                agent.QualificationState,
                personalSales,
                personalBv,
                activeDirects,
                activeLegs.Contains(PlacementSide.Left),
                activeLegs.Contains(PlacementSide.Right)
            ),
            rules
        );
    }

    private void AddConsumptionEntries(
        ProcessBinaryPairingCommand request,
        Guid runId,
        decimal leftConsumed,
        decimal rightConsumed,
        DateTimeOffset effectiveAt
    )
    {
        if (leftConsumed > 0m)
            db.BinaryVolumeEntries.Add(
                BinaryVolumeEntry.PairConsumption(
                    request.OrganizationId,
                    request.AgentId,
                    runId,
                    PlacementSide.Left,
                    leftConsumed,
                    effectiveAt
                )
            );
        if (rightConsumed > 0m)
            db.BinaryVolumeEntries.Add(
                BinaryVolumeEntry.PairConsumption(
                    request.OrganizationId,
                    request.AgentId,
                    runId,
                    PlacementSide.Right,
                    rightConsumed,
                    effectiveAt
                )
            );
    }

    private async Task AddCommissionAndWalletCreditAsync(
        ProcessBinaryPairingCommand request,
        CommissionPlan plan,
        Guid runId,
        decimal matchedVolume,
        decimal payableCommission,
        CancellationToken cancellationToken
    )
    {
        var commission = CommissionTransaction.CreateBinaryPairing(
            request.OrganizationId,
            request.AgentId,
            runId,
            plan.Id,
            $"binary-v{plan.Version}",
            matchedVolume,
            plan.BinaryPairing.PairingRate,
            payableCommission
        );
        db.CommissionTransactions.Add(commission);
        var wallet = await db.AgentWallets.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.AgentId == request.AgentId,
            cancellationToken
        );
        if (wallet is null)
        {
            var currency = await db
                .Organizations.AsNoTracking()
                .Where(organization => organization.Id == request.OrganizationId)
                .Select(organization => organization.CurrencyCode)
                .SingleAsync(cancellationToken);
            wallet = AgentWallet.Open(request.OrganizationId, request.AgentId, currency);
            db.AgentWallets.Add(wallet);
        }
        db.WalletEntries.Add(
            WalletEntry.Create(
                wallet.Id,
                WalletEntryType.PendingCredit,
                payableCommission,
                nameof(CommissionTransaction),
                commission.Id
            )
        );
    }

    private static QualificationRules ReadQualificationRules(string json) =>
        IsEmptyRules(json)
            ? QualificationRules.None()
            : JsonSerializer.Deserialize<QualificationRules>(json, JsonOptions)
                ?? throw new InvalidOperationException("Qualification rules JSON is invalid.");

    private static PairingCapRules ReadCapRules(string json) =>
        IsEmptyRules(json)
            ? new PairingCapRules(false, 0m)
            : JsonSerializer.Deserialize<PairingCapRules>(json, JsonOptions)
                ?? throw new InvalidOperationException("Cap rules JSON is invalid.");

    private static bool IsEmptyRules(string json) =>
        string.IsNullOrWhiteSpace(json) || json.Trim() is "[]" or "{}";

    private static decimal TruncateVolume(decimal value) =>
        decimal.Truncate(value * 10_000m) / 10_000m;

    private sealed record PairingCapRules(bool Enabled, decimal MaximumAmount);
}
