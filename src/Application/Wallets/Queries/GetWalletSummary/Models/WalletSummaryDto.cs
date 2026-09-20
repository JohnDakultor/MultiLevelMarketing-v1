namespace modular_mlm.Application.Wallets.Queries.GetWalletSummary.Models;

public sealed record WalletSummaryDto(
    Guid WalletId,
    string Currency,
    decimal Pending,
    decimal Available,
    decimal Held,
    decimal PaidLifetime,
    decimal RecoverableNegative,
    decimal Net
);
