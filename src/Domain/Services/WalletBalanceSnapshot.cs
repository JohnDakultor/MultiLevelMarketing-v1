namespace modular_mlm.Domain.Services;

public sealed record WalletBalanceSnapshot(
    decimal Pending,
    decimal Available,
    decimal Held,
    decimal PaidOut,
    decimal RecoverableNegative,
    decimal Net
);
