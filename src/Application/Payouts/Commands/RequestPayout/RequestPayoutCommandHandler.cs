using Domain.Services;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Payouts.Commands.RequestPayout;

public sealed class RequestPayoutCommandHandler(
    IUser currentUser,
    IApplicationDbContext db,
    IAdministratorAccountService administrators,
    WalletBalanceCalculator balanceCalculator,
    PayoutEligibilityEvaluator eligibilityEvaluator,
    TimeProvider clock
) : IRequestHandler<RequestPayoutCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestPayoutCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var agent = await db.Agents.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.AgentId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent was not found.");
        if (
            agent.UserId != userId
            && !await administrators.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();
        var hasVerifiedPayoutAccount = await db.PayoutAccounts.AnyAsync(
            x =>
                x.Id == request.PayoutAccountId
                && x.OrganizationId == request.OrganizationId
                && x.AgentId == request.AgentId
                && x.VerificationStatus == PayoutVerificationStatus.Verified,
            cancellationToken
        );
        var wallet = await db.AgentWallets.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.AgentId == request.AgentId,
            cancellationToken
        );
        if (wallet is null)
            throw new InvalidOperationException("An active agent wallet is required.");

        var walletSettings = await db.WalletSettings.SingleOrDefaultAsync(
            settings => settings.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (walletSettings is null)
            throw new InvalidOperationException("Organization wallet settings are missing.");
        var entries = await db
            .WalletEntries.AsNoTracking()
            .Where(entry => entry.WalletId == wallet.Id)
            .ToListAsync(cancellationToken);
        var balance = balanceCalculator.Calculate(entries);
        var decision = eligibilityEvaluator.Evaluate(
            new PayoutEligibilityInput(
                agent.Status,
                wallet.Status,
                hasVerifiedPayoutAccount,
                request.Amount,
                request.Currency,
                wallet.Currency,
                balance,
                walletSettings,
                HasComplianceOrAdministratorHold: false
            )
        );
        if (!decision.IsEligible)
            throw new InvalidOperationException(decision.Reason);

        var payout = PayoutRequest.Request(
            request.OrganizationId,
            request.AgentId,
            request.PayoutAccountId,
            request.Amount,
            request.Currency,
            clock.GetUtcNow()
        );
        payout.StartReview();
        db.PayoutRequests.Add(payout);
        db.WalletEntries.Add(
            WalletEntry.Create(
                wallet.Id,
                WalletEntryType.Hold,
                -request.Amount,
                "PayoutRequest",
                payout.Id
            )
        );
        payout.AddDomainEvent(
            new WalletFundsReservedEvent(
                request.OrganizationId,
                wallet.Id,
                payout.Id,
                request.Amount,
                payout.Currency
            )
        );
        await db.SaveChangesAsync(cancellationToken);
        return payout.Id;
    }
}
