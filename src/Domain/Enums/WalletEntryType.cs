namespace modular_mlm.Domain.Wallets;

public enum WalletEntryType
{
    PendingCredit,
    AvailableCredit,
    Hold,
    Debit,
    Payout,
    Adjustment,
    Reversal,
}
