using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Queries.GetAdminWallets.Models;

public sealed record AdminWalletSummaryDto(
    Guid WalletId,
    Guid AgentId,
    string AgentCode,
    string? DisplayName,
    string Currency,
    WalletStatus Status,
    decimal Pending,
    decimal Available,
    decimal Held,
    decimal PaidLifetime,
    decimal RecoverableNegative,
    decimal Net
);
