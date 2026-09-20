using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Compensation.Services;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Compensation.Commands.ProcessPaidOrderCommissions;

public sealed class ProcessPaidOrderCommissionsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ProcessPaidOrderCommissionsCommand, ProcessPaidOrderCommissionsResult>
{
    public async Task<ProcessPaidOrderCommissionsResult> Handle(
        ProcessPaidOrderCommissionsCommand request,
        CancellationToken cancellationToken
    )
    {
        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.OrderId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");
        if (order.PaymentStatus != PaymentStatus.Paid || order.PaidAt is null)
            throw new InvalidOperationException(
                "Only a provider-verified paid order can generate compensation."
            );

        var effectiveAt = order.PaidAt.Value;
        var plan = await db
            .CommissionPlans.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.Status == CommissionPlanStatus.Active
                && candidate.EffectiveFrom <= effectiveAt
                && (candidate.EffectiveTo == null || candidate.EffectiveTo > effectiveAt)
            )
            .OrderByDescending(candidate => candidate.Version)
            .FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
            throw new InvalidOperationException(
                "No active commission plan applies to the order payment date."
            );

        if (order.AttributedAgentId is null)
            return EmptyResult(order.Id, plan.Id);

        var attributedAgent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                agent =>
                    agent.Id == order.AttributedAgentId
                    && agent.OrganizationId == request.OrganizationId
                    && agent.Status == AgentStatus.Active,
                cancellationToken
            );
        if (attributedAgent is null)
            return EmptyResult(order.Id, plan.Id);

        var orderItemIds = order.Items.Select(item => item.Id).ToArray();
        var compensatedItemIds = await db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.OrganizationId == request.OrganizationId
                && transaction.SourceOrderId == order.Id
                && transaction.BeneficiaryAgentId == attributedAgent.Id
                && transaction.Type == CommissionType.DirectSale
                && transaction.SourceOrderItemId.HasValue
            )
            .Select(transaction => transaction.SourceOrderItemId!.Value)
            .ToListAsync(cancellationToken);
        var compensatedItems = compensatedItemIds.ToHashSet();

        AgentWallet? wallet = null;
        var directCommissionsCreated = 0;
        var grossCommissionAmount = 0m;
        foreach (var item in order.Items)
        {
            if (item.CommissionableAmount <= 0 || compensatedItems.Contains(item.Id))
                continue;

            var calculation = DirectSalesCommissionCalculator.Calculate(
                item.CommissionableAmount,
                plan.DirectSalesRate,
                item.DirectSalesRateOverride
            );
            if (calculation.GrossAmount <= 0)
                continue;

            wallet ??= await GetOrOpenWalletAsync(
                request.OrganizationId,
                attributedAgent.Id,
                order.Currency,
                cancellationToken
            );
            var commission = CommissionTransaction.CreateDirectSale(
                request.OrganizationId,
                attributedAgent.Id,
                order.Id,
                item.Id,
                plan.Id,
                $"direct-sale:v{plan.Version}",
                calculation.BaseAmount,
                calculation.Rate,
                calculation.GrossAmount
            );
            db.CommissionTransactions.Add(commission);
            db.WalletEntries.Add(
                WalletEntry.Create(
                    wallet.Id,
                    WalletEntryType.PendingCredit,
                    calculation.GrossAmount,
                    "Commission",
                    commission.Id
                )
            );
            directCommissionsCreated++;
            grossCommissionAmount += calculation.GrossAmount;
        }

        var ancestors = await db
            .PlacementClosures.AsNoTracking()
            .Where(closure =>
                closure.OrganizationId == request.OrganizationId
                && closure.DescendantAgentId == attributedAgent.Id
            )
            .ToListAsync(cancellationToken);
        var existingVolumeKeys = await db
            .BinaryVolumeEntries.AsNoTracking()
            .Where(entry =>
                entry.OrganizationId == request.OrganizationId
                && entry.SourceOrderItemId.HasValue
                && orderItemIds.Contains(entry.SourceOrderItemId.Value)
                && entry.EntryType == BinaryVolumeEntryType.Credit
            )
            .Select(entry => new
            {
                entry.OwnerAgentId,
                SourceOrderItemId = entry.SourceOrderItemId!.Value,
            })
            .ToListAsync(cancellationToken);
        var creditedVolume = existingVolumeKeys
            .Select(key => (key.OwnerAgentId, key.SourceOrderItemId))
            .ToHashSet();
        var ancestorAgentIds = ancestors
            .Select(closure => closure.AncestorAgentId)
            .Distinct()
            .ToArray();
        var balances = await db
            .BinaryVolumeBalances.Where(balance =>
                balance.OrganizationId == request.OrganizationId
                && ancestorAgentIds.Contains(balance.AgentId)
            )
            .ToDictionaryAsync(balance => balance.AgentId, cancellationToken);

        var volumeCreditsCreated = 0;
        foreach (var item in order.Items.Where(candidate => candidate.BusinessVolume > 0))
        {
            var credits = BinaryVolumePropagationService.CreateCredits(
                request.OrganizationId,
                attributedAgent.Id,
                item.Id,
                item.BusinessVolume,
                ancestors,
                effectiveAt
            );
            foreach (var credit in credits)
            {
                if (!creditedVolume.Add((credit.OwnerAgentId, credit.SourceOrderItemId!.Value)))
                    continue;
                if (!balances.TryGetValue(credit.OwnerAgentId, out var balance))
                {
                    balance = BinaryVolumeBalance.Open(request.OrganizationId, credit.OwnerAgentId);
                    balances.Add(credit.OwnerAgentId, balance);
                    db.BinaryVolumeBalances.Add(balance);
                }
                balance.Credit(credit.Side, credit.Volume);
                db.BinaryVolumeEntries.Add(credit);
                volumeCreditsCreated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ProcessPaidOrderCommissionsResult(
            order.Id,
            plan.Id,
            directCommissionsCreated,
            volumeCreditsCreated,
            grossCommissionAmount
        );
    }

    private async Task<AgentWallet> GetOrOpenWalletAsync(
        Guid organizationId,
        Guid agentId,
        string currency,
        CancellationToken cancellationToken
    )
    {
        var wallet = await db.AgentWallets.SingleOrDefaultAsync(
            candidate => candidate.OrganizationId == organizationId && candidate.AgentId == agentId,
            cancellationToken
        );
        if (wallet is not null)
        {
            if (!wallet.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Agent wallet currency does not match the paid order currency."
                );
            if (wallet.Status != WalletStatus.Active)
                throw new InvalidOperationException("Agent wallet is not active.");
            return wallet;
        }

        wallet = AgentWallet.Open(organizationId, agentId, currency);
        db.AgentWallets.Add(wallet);
        return wallet;
    }

    private static ProcessPaidOrderCommissionsResult EmptyResult(Guid orderId, Guid planId) =>
        new(orderId, planId, 0, 0, 0m);
}
