using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries.Models;

public sealed record AdminWalletEntryDto(
    Guid Id,
    Guid WalletId,
    WalletEntryType Type,
    decimal Amount,
    string SourceType,
    Guid SourceId,
    DateTimeOffset? AvailableAt,
    Guid? ReversalOfEntryId,
    Guid? ReleasedFromEntryId,
    DateTimeOffset CreatedAt
);
