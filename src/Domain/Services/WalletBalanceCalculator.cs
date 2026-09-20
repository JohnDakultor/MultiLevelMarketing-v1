using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.Wallets;

namespace Domain.Services;

public sealed class WalletBalanceCalculator
{
    public WalletBalanceSnapshot Calculate(IReadOnlyCollection<WalletEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return new WalletBalanceSnapshot(0m, 0m, 0m, 0m, 0m, 0m);
        }

        Guid targetWalletId = entries.First().WalletId;
        if (entries.Any(e => e.WalletId != targetWalletId))
        {
            throw new DomainInvariantException(
                "Wallet calculation rejected: All ledger entries must belong to the same WalletId."
            );
        }

        var distinctCount = entries.Select(e => e.Id).Distinct().Count();
        if (distinctCount != entries.Count)
        {
            throw new DomainInvariantException(
                "Wallet calculation rejected: Ledger contains duplicate Entry identifiers."
            );
        }

        var releasedPendingIds = entries
            .Where(e => e.Type == WalletEntryType.AvailableCredit && e.ReleasedFromEntryId != null)
            .Select(e => e.ReleasedFromEntryId!.Value)
            .ToHashSet();

        var reversedEntryIds = entries
            .Where(e => e.Type == WalletEntryType.Reversal && e.ReversalOfEntryId != null)
            .Select(e => e.ReversalOfEntryId!.Value)
            .ToHashSet();

        decimal pending = entries
            .Where(e =>
                e.Type == WalletEntryType.PendingCredit
                && e.Amount > 0m
                && !releasedPendingIds.Contains(e.Id)
                && !reversedEntryIds.Contains(e.Id)
            )
            .Sum(e => e.Amount);

        decimal net = entries
            .Where(e => e.Type != WalletEntryType.PendingCredit)
            .Sum(e => e.Amount);

        decimal held = Math.Abs(
            entries
                .Where(e => e.Type == WalletEntryType.Hold && !reversedEntryIds.Contains(e.Id))
                .Sum(e => e.Amount)
        );

        decimal paidOut = Math.Abs(
            entries.Where(e => e.Type == WalletEntryType.Payout).Sum(e => e.Amount)
        );

        decimal recoverableNegative = Math.Max(0m, -net);
        decimal available = Math.Max(0m, net);

        return new WalletBalanceSnapshot(
            Pending: pending,
            Available: available,
            Held: held,
            PaidOut: paidOut,
            RecoverableNegative: recoverableNegative,
            Net: net
        );
    }
}
