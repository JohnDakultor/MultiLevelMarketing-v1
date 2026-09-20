using Domain.Enums;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Wallets;

public sealed class WalletEntry : BaseAuditableEntity
{
    private WalletEntry() { }

    public Guid WalletId { get; private set; }
    public WalletEntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public DateTimeOffset? AvailableAt { get; private set; }
    public Guid? ReversalOfEntryId { get; private set; }
    public Guid? ReleasedFromEntryId { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public static WalletEntry Create(
        Guid walletId,
        WalletEntryType type,
        decimal amount,
        string sourceType,
        Guid sourceId,
        DateTimeOffset? availableAt = null
    )
    {
        if (
            walletId == Guid.Empty
            || sourceId == Guid.Empty
            || amount == 0
            || string.IsNullOrWhiteSpace(sourceType)
        )
            throw new DomainInvariantException("Wallet entry values are invalid.");
        if (type == WalletEntryType.Adjustment)
            throw new DomainInvariantException(
                "Administrator adjustments must use the adjustment factory."
            );

        return new WalletEntry
        {
            WalletId = walletId,
            Type = type,
            Amount = amount,
            SourceType = sourceType,
            SourceId = sourceId,
            AvailableAt = availableAt,
        };
    }

    public static WalletEntry CreateAdjustment(
        Guid walletId,
        WalletAdjustmentDirection direction,
        decimal amount,
        string idempotencyKey,
        DateTimeOffset createdAt
    )
    {
        if (walletId == Guid.Empty || amount <= 0m)
            throw new DomainInvariantException(
                "Adjustment wallet and positive amount are required."
            );
        if (!Enum.IsDefined(direction))
            throw new DomainInvariantException("Wallet adjustment direction is invalid.");

        var normalizedKey = idempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey) || normalizedKey.Length > 128)
            throw new DomainInvariantException(
                "Adjustment idempotency key is required and cannot exceed 128 characters."
            );

        var entry = new WalletEntry
        {
            WalletId = walletId,
            Type = WalletEntryType.Adjustment,
            Amount = direction == WalletAdjustmentDirection.Credit ? amount : -amount,
            SourceType = "AdministratorAdjustment",
            AvailableAt = createdAt,
            IdempotencyKey = normalizedKey,
        };
        entry.SourceId = entry.Id;
        return entry;
    }

    public WalletEntry Release(DateTimeOffset availableAt)
    {
        if (Type != WalletEntryType.PendingCredit || Amount <= 0m)
            throw new DomainInvariantException("Only a positive pending credit can be released.");

        return new WalletEntry
        {
            WalletId = WalletId,
            Type = WalletEntryType.AvailableCredit,
            Amount = Amount,
            SourceType = SourceType,
            SourceId = SourceId,
            AvailableAt = availableAt,
            ReleasedFromEntryId = Id,
        };
    }

    public WalletEntry Reverse() =>
        new()
        {
            WalletId = WalletId,
            Type = WalletEntryType.Reversal,
            Amount = -Amount,
            SourceType = SourceType,
            SourceId = SourceId,
            ReversalOfEntryId = Id,
            AvailableAt = AvailableAt,
        };

    public WalletEntry ReverseForRefund(Guid orderItemRefundId, decimal amount)
    {
        if (orderItemRefundId == Guid.Empty || amount <= 0m || amount > Math.Abs(Amount))
            throw new DomainInvariantException("Wallet refund reversal values are invalid.");

        return new WalletEntry
        {
            WalletId = WalletId,
            Type = WalletEntryType.Reversal,
            Amount = -amount,
            SourceType = "OrderItemRefund",
            SourceId = orderItemRefundId,
            ReversalOfEntryId = Id,
            AvailableAt = AvailableAt,
        };
    }
}
