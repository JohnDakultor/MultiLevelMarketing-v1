using modular_mlm.Application.Wallets.Queries.GetAdminWallets.Models;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Queries.GetAdminWallets;

public sealed record GetAdminWalletsQuery(
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Search,
    WalletStatus? Status,
    bool NegativeOnly
) : IRequest<AdminWalletsPageDto>, IOrganizationAdminRequest;
