using Domain.Enums;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class WalletSettings : OrganizationEntity
{
    private WalletSettings() { }

    public CommissionReleaseTrigger CommissionReleaseTrigger { get; private set; }
    public int ReleaseDelayDays { get; private set; }
    public int ReturnWindowDays { get; private set; }
    public decimal MinimumPayoutAmount { get; private set; }
    public bool AllowNegativeRecoverableBalance { get; private set; }
    public decimal MaximumNegativeBalance { get; private set; }

    public static WalletSettings Create(
        CommissionReleaseTrigger commissionReleaseTrigger,
        int releaseDelayDays,
        int returnWindowDays,
        decimal minimumPayoutAmount,
        bool allowNegativeRecoverableBalance,
        decimal maximumNegativeBalance
    )
    {
        var settings = new WalletSettings();
        settings.Update(
            commissionReleaseTrigger,
            releaseDelayDays,
            returnWindowDays,
            minimumPayoutAmount,
            allowNegativeRecoverableBalance,
            maximumNegativeBalance
        );
        return settings;
    }

    public static WalletSettings Default() =>
        Create(
            CommissionReleaseTrigger.PaymentConfirmed,
            releaseDelayDays: 0,
            returnWindowDays: 0,
            minimumPayoutAmount: 10m,
            allowNegativeRecoverableBalance: false,
            maximumNegativeBalance: 0m
        );

    public void Update(
        CommissionReleaseTrigger commissionReleaseTrigger,
        int releaseDelayDays,
        int returnWindowDays,
        decimal minimumPayoutAmount,
        bool allowNegativeRecoverableBalance,
        decimal maximumNegativeBalance
    )
    {
        if (!Enum.IsDefined(commissionReleaseTrigger))
            throw new DomainInvariantException("Commission release trigger is invalid.");
        if (releaseDelayDays < 0)
            throw new DomainInvariantException("Release delay days cannot be negative.");
        if (returnWindowDays < 0)
            throw new DomainInvariantException("Return window days cannot be negative.");
        if (minimumPayoutAmount <= 0m)
            throw new DomainInvariantException("Minimum payout amount must be positive.");
        if (maximumNegativeBalance < 0m)
            throw new DomainInvariantException("Maximum negative balance cannot be negative.");
        if (!allowNegativeRecoverableBalance && maximumNegativeBalance != 0m)
            throw new DomainInvariantException(
                "Maximum negative balance must be zero when negative balances are disabled."
            );

        CommissionReleaseTrigger = commissionReleaseTrigger;
        ReleaseDelayDays = releaseDelayDays;
        ReturnWindowDays = returnWindowDays;
        MinimumPayoutAmount = minimumPayoutAmount;
        AllowNegativeRecoverableBalance = allowNegativeRecoverableBalance;
        MaximumNegativeBalance = maximumNegativeBalance;
    }
}
