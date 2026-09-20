using System.Text.Json;
using Domain.Enums;
using Domain.Services;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Commands.CreateWalletAdjustment;

public sealed class CreateWalletAdjustmentCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    IWalletAdjustmentLock adjustmentLock,
    WalletBalanceCalculator balanceCalculator,
    TimeProvider timeProvider
) : IRequestHandler<CreateWalletAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateWalletAdjustmentCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db
            .Organizations.Include(candidate => candidate.Wallet)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var agentExists = await db.Agents.AnyAsync(
            agent => agent.Id == request.AgentId && agent.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (!agentExists)
            throw new KeyNotFoundException("Agent was not found.");

        var wallet = await db.AgentWallets.SingleOrDefaultAsync(
            candidate =>
                candidate.AgentId == request.AgentId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (wallet is null)
            throw new KeyNotFoundException("Agent wallet was not found.");
        if (wallet.Status != WalletStatus.Active)
            throw new InvalidOperationException("Only an active wallet can be adjusted.");

        var normalizedCurrency = request.Currency.Trim().ToUpperInvariant();
        if (
            !wallet.Currency.Equals(normalizedCurrency, StringComparison.Ordinal)
            || !organization.CurrencyCode.Equals(normalizedCurrency, StringComparison.Ordinal)
        )
            throw new InvalidOperationException(
                "Adjustment currency must match the wallet and organization currency."
            );

        var normalizedKey = request.IdempotencyKey.Trim();
        var normalizedReason = request.Reason.Trim();
        var signedAmount =
            request.Direction == WalletAdjustmentDirection.Credit
                ? request.Amount
                : -request.Amount;

        await using var adjustmentLease = await adjustmentLock.AcquireAsync(
            wallet.Id,
            normalizedKey,
            cancellationToken
        );

        var existingEntry = await db
            .WalletEntries.AsNoTracking()
            .SingleOrDefaultAsync(
                entry => entry.WalletId == wallet.Id && entry.IdempotencyKey == normalizedKey,
                cancellationToken
            );
        if (existingEntry is not null)
        {
            var existingReason = await db
                .AuditLogs.AsNoTracking()
                .Where(audit =>
                    audit.OrganizationId == request.OrganizationId
                    && audit.EntityType == AuditCoverageMap.WalletAdjusted.EntityType
                    && audit.EntityId == existingEntry.Id
                    && audit.Action == AuditCoverageMap.WalletAdjusted.Action
                )
                .Select(audit => audit.Reason)
                .SingleOrDefaultAsync(cancellationToken);

            if (
                existingEntry.Type != WalletEntryType.Adjustment
                || existingEntry.Amount != signedAmount
                || !string.Equals(
                    existingEntry.SourceType,
                    "AdministratorAdjustment",
                    StringComparison.Ordinal
                )
                || !string.Equals(existingReason, normalizedReason, StringComparison.Ordinal)
            )
                throw new IdempotencyConflictException(
                    "The idempotency key was already used for a different wallet adjustment."
                );

            return existingEntry.Id;
        }

        if (request.Direction == WalletAdjustmentDirection.Debit)
        {
            var entries = await db
                .WalletEntries.AsNoTracking()
                .Where(entry => entry.WalletId == wallet.Id)
                .ToListAsync(cancellationToken);
            var currentBalance = balanceCalculator.Calculate(entries);
            var projectedNegative = Math.Max(0m, -(currentBalance.Net - request.Amount));

            if (
                projectedNegative > 0m
                && (
                    !organization.Wallet.AllowNegativeRecoverableBalance
                    || projectedNegative > organization.Wallet.MaximumNegativeBalance
                )
            )
                throw new InvalidOperationException(
                    "The debit adjustment exceeds the wallet's negative-balance policy."
                );
        }

        var entry = WalletEntry.CreateAdjustment(
            wallet.Id,
            request.Direction,
            request.Amount,
            normalizedKey,
            timeProvider.GetUtcNow()
        );
        db.WalletEntries.Add(entry);

        auditWriter.Write(
            request.OrganizationId,
            AuditCoverageMap.WalletAdjusted.Action,
            AuditCoverageMap.WalletAdjusted.EntityType,
            entry.Id,
            beforeJson: null,
            JsonSerializer.Serialize(
                new
                {
                    entry.WalletId,
                    entry.Type,
                    entry.Amount,
                    Currency = normalizedCurrency,
                    entry.SourceType,
                }
            ),
            normalizedReason
        );

        await db.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }
}
