using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Payouts.Commands.ReconcilePayout;

public sealed class ReconcilePayoutCommandHandler(
    IApplicationDbContext db,
    IPayoutProvider provider,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<ReconcilePayoutCommand, bool>
{
    public async Task<bool> Handle(
        ReconcilePayoutCommand request,
        CancellationToken cancellationToken
    )
    {
        var payout = await db.PayoutRequests.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutRequestId
                && (
                    !request.OrganizationId.HasValue
                    || candidate.OrganizationId == request.OrganizationId.Value
                ),
            cancellationToken
        );
        if (payout is null)
            throw new KeyNotFoundException("Payout request was not found.");
        if (payout.Status != PayoutStatus.Processing || payout.ProviderTransferId is null)
            return false;

        var result = await provider.GetStatusAsync(payout.ProviderTransferId, cancellationToken);
        if (!string.Equals(result.TransferId, payout.ProviderTransferId, StringComparison.Ordinal))
            throw new InvalidOperationException("Provider returned a different transfer.");
        if (string.Equals(result.Status, "pending", StringComparison.OrdinalIgnoreCase))
            return false;

        var beforeJson = Serialize(payout);

        var hold = await db.WalletEntries.SingleAsync(
            entry =>
                entry.SourceType == "PayoutRequest"
                && entry.SourceId == payout.Id
                && entry.Type == WalletEntryType.Hold,
            cancellationToken
        );
        if (
            !await db.WalletEntries.AnyAsync(
                entry => entry.ReversalOfEntryId == hold.Id,
                cancellationToken
            )
        )
            db.WalletEntries.Add(hold.Reverse());

        if (string.Equals(result.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            payout.MarkPaid(result.ProviderReference ?? result.TransferId, clock.GetUtcNow());
            db.WalletEntries.Add(
                WalletEntry.Create(
                    hold.WalletId,
                    WalletEntryType.Payout,
                    -payout.Amount,
                    "PayoutRequest",
                    payout.Id
                )
            );
        }
        else if (string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))
            payout.MarkFailed(
                result.ProviderReference ?? result.TransferId,
                result.FailureCode,
                result.FailureMessage
            );
        else
            throw new InvalidOperationException(
                $"Unknown provider payout status '{result.Status}'."
            );

        var audit =
            payout.Status == PayoutStatus.Paid
                ? AuditCoverageMap.PayoutCompleted
                : AuditCoverageMap.PayoutFailed;
        auditWriter.Write(
            payout.OrganizationId,
            audit.Action,
            audit.EntityType,
            payout.Id,
            beforeJson,
            Serialize(payout),
            payout.FailureCode
        );

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string Serialize(PayoutRequest payout) =>
        AuditJson.Serialize(
            new
            {
                payout.Status,
                payout.ProcessedAt,
                payout.ProviderReference,
                payout.ProviderTransferId,
                payout.FailureCode,
            }
        );
}
