using modular_mlm.Domain.Wallets;

namespace Application.Wallets.Queries.GetWalletEntries.Models;

public sealed record WalletEntryDto(
    Guid Id,
    WalletEntryType Type,
    decimal Amount,
    string SourceType,
    Guid SourceId,
    DateTimeOffset? AvailableAt,
    Guid? ReversalOfEntryId,
    Guid? ReleasedFromEntryId,
    DateTimeOffset CreatedAt
);
