using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.Wallets;

namespace Domain.Services;

public sealed record PayoutEligibilityInput(
    AgentStatus AgentStatus,
    WalletStatus WalletStatus,
    bool HasVerifiedPayoutAccount,
    decimal RequestedAmount,
    string RequestedCurrency,
    string WalletCurrency,
    WalletBalanceSnapshot WalletBalance,
    WalletSettings WalletSettings,
    bool HasComplianceOrAdministratorHold
);

public class PayoutEligibilityEvaluator
{
    public PayoutEligibilityDecision Evaluate(PayoutEligibilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.WalletBalance);
        ArgumentNullException.ThrowIfNull(input.WalletSettings);

        if (input.AgentStatus != AgentStatus.Active)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.AgentNotActive,
                "Only an active agent can request a payout.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (input.WalletStatus != WalletStatus.Active)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.WalletNotActive,
                "An active agent wallet is required.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (!input.HasVerifiedPayoutAccount)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.PayoutAccountNotVerified,
                "A verified payout account is required.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (input.HasComplianceOrAdministratorHold)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.ComplianceHold,
                "Compliance or administrator hold exists.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (
            !string.Equals(
                input.RequestedCurrency,
                input.WalletCurrency,
                StringComparison.OrdinalIgnoreCase
            )
        )
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.CurrencyMismatch,
                "Payout currency does not match the wallet.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (input.RequestedAmount < input.WalletSettings.MinimumPayoutAmount)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.BelowMinimumPayout,
                $"Payout amount must be at least {input.WalletSettings.MinimumPayoutAmount}.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (
            input.WalletBalance.RecoverableNegative > 0m
            && !input.WalletSettings.AllowNegativeRecoverableBalance
        )
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.NegativeBalanceNotAllowed,
                "The wallet's recoverable negative balance is not allowed.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (input.WalletBalance.RecoverableNegative > input.WalletSettings.MaximumNegativeBalance)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.MaximumNegativeBalanceExceeded,
                "The wallet's recoverable negative balance exceeds the organization's limit.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        if (input.RequestedAmount > input.WalletBalance.Available)
            return PayoutEligibilityDecision.Ineligible(
                PayoutReasonCodes.InsufficientAvailableBalance,
                "Payout exceeds the available wallet balance.",
                input.WalletBalance.Available,
                input.WalletSettings.MinimumPayoutAmount
            );

        return PayoutEligibilityDecision.Eligible(
            input.WalletBalance.Available,
            input.WalletSettings.MinimumPayoutAmount
        );
    }
}
