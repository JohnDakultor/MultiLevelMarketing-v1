using Domain.Enums;

namespace modular_mlm.Application.Organizations.Queries.GetWalletSettings.Models;

public sealed record WalletSettingsDto(
    CommissionReleaseTrigger CommissionReleaseTrigger,
    int ReleaseDelayDays,
    int ReturnWindowDays,
    decimal MinimumPayoutAmount,
    bool AllowNegativeRecoverableBalance,
    decimal MaximumNegativeBalance
);
