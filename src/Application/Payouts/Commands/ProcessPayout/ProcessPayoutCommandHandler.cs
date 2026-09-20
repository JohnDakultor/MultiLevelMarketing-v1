using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Payouts.Commands.ProcessPayout;

public sealed class ProcessPayoutCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db,
    IPayoutProvider provider,
    IPayoutAccountProtector protector,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<ProcessPayoutCommand, string>
{
    public async Task<string> Handle(
        ProcessPayoutCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await administrators.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();

        return await HandleTrustedAsync(request, cancellationToken);
    }

    public async Task<string> HandleTrustedAsync(
        ProcessPayoutCommand request,
        CancellationToken cancellationToken
    )
    {
        var payout = await db.PayoutRequests.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutRequestId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (payout is null)
            throw new KeyNotFoundException("Payout request was not found.");
        if (payout.ProviderTransferId is not null)
            return payout.ProviderTransferId;
        if (payout.Status == PayoutStatus.Approved)
        {
            var beforeJson = Serialize(payout);
            payout.StartProcessing();
            WriteAudit(payout, AuditCoverageMap.PayoutProcessingStarted, beforeJson);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (payout.Status != PayoutStatus.Processing)
            throw new InvalidOperationException("Only an approved payout can be processed.");

        var account = await db.PayoutAccounts.SingleAsync(
            candidate => candidate.Id == payout.PayoutAccountId,
            cancellationToken
        );
        if (account.VerificationStatus != PayoutVerificationStatus.Verified)
            throw new InvalidOperationException("Payout account is no longer verified.");
        var result = await provider.CreateAsync(
            new CreateProviderPayoutRequest(
                payout.Id,
                ToMinorUnits(payout.Amount),
                payout.Currency,
                new PayoutDestination(
                    protector.Unprotect(account.ProtectedAccountName),
                    protector.Unprotect(account.ProtectedAccountNumber),
                    account.BankCode,
                    account.Rail
                ),
                $"paymongo-payout:{payout.Id:N}"
            ),
            cancellationToken
        );
        payout.AttachProviderTransfer(result.BatchId, result.TransferId);
        var beforeProviderStatus = Serialize(payout);
        await ApplyProviderStatusAsync(payout, result, cancellationToken);
        if (payout.Status == PayoutStatus.Paid)
            WriteAudit(payout, AuditCoverageMap.PayoutCompleted, beforeProviderStatus);
        else if (payout.Status == PayoutStatus.Failed)
            WriteAudit(payout, AuditCoverageMap.PayoutFailed, beforeProviderStatus);
        await db.SaveChangesAsync(cancellationToken);
        return result.TransferId;
    }

    private async Task ApplyProviderStatusAsync(
        PayoutRequest payout,
        ProviderPayoutResult result,
        CancellationToken cancellationToken
    )
    {
        if (string.Equals(result.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            payout.MarkPaid(result.ProviderReference ?? result.TransferId, clock.GetUtcNow());
            await SettleWalletAsync(payout, paid: true, cancellationToken);
        }
        else if (string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            payout.MarkFailed(
                result.ProviderReference ?? result.TransferId,
                result.FailureCode,
                result.FailureMessage
            );
            await SettleWalletAsync(payout, paid: false, cancellationToken);
        }
    }

    private async Task SettleWalletAsync(
        PayoutRequest payout,
        bool paid,
        CancellationToken cancellationToken
    )
    {
        var hold = await db.WalletEntries.SingleAsync(
            entry =>
                entry.SourceType == "PayoutRequest"
                && entry.SourceId == payout.Id
                && entry.Type == WalletEntryType.Hold,
            cancellationToken
        );
        var released = await db.WalletEntries.AnyAsync(
            entry => entry.ReversalOfEntryId == hold.Id,
            cancellationToken
        );
        if (!released)
            db.WalletEntries.Add(hold.Reverse());
        if (paid)
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

    private static long ToMinorUnits(decimal amount) => decimal.ToInt64(amount * 100m);

    private void WriteAudit(
        PayoutRequest payout,
        AuditCoverageDefinition definition,
        string beforeJson
    ) =>
        auditWriter.Write(
            payout.OrganizationId,
            definition.Action,
            definition.EntityType,
            payout.Id,
            beforeJson,
            Serialize(payout),
            payout.FailureCode
        );

    private static string Serialize(PayoutRequest payout) =>
        AuditJson.Serialize(
            new
            {
                payout.Status,
                payout.ApprovedAt,
                payout.ProcessedAt,
                payout.ProviderReference,
                payout.ProviderBatchId,
                payout.ProviderTransferId,
                payout.FailureCode,
            }
        );
}
