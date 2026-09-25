using modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries.Models;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries;

public sealed record GetAdminWalletEntriesQuery(
    Guid OrganizationId,
    Guid AgentId,
    int Page,
    int PageSize,
    WalletEntryType? EntryType,
    DateTimeOffset? From,
    DateTimeOffset? To
) : IRequest<AdminWalletEntriesPageDto>, IOrganizationAdminRequest;
