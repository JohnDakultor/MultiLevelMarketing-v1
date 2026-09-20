using Application.Wallets.Queries.GetWalletEntries.Models;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Queries.GetWalletEntries;

public sealed record GetWalletEntriesQuery(
    Guid OrganizationId,
    Guid AgentId,
    int Page,
    int PageSize,
    WalletEntryType? EntryType,
    DateTimeOffset? From,
    DateTimeOffset? To
) : IRequest<WalletEntriesPageDto>, IAgentScopedRequest;
