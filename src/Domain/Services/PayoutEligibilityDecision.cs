using System;

namespace modular_mlm.Domain.Services;

public static class PayoutReasonCodes
{
    public const string Eligible = "eligible";
    public const string AgentNotActive = "agent_not_active";
    public const string WalletNotActive = "wallet_not_active";
    public const string PayoutAccountNotVerified = "payout_account_not_verified";
    public const string ComplianceHold = "compliance_hold";
    public const string CurrencyMismatch = "currency_mismatch";
    public const string BelowMinimumPayout = "below_minimum_payout";
    public const string InsufficientAvailableBalance = "insufficient_available_balance";
    public const string NegativeBalanceNotAllowed = "negative_balance_not_allowed";
    public const string MaximumNegativeBalanceExceeded = "maximum_negative_balance_exceeded";
}

public sealed record PayoutEligibilityDecision(
    bool IsEligible,
    string ReasonCode,
    string Reason,
    decimal AvailableAmount,
    decimal MinimumPayoutAmount
)
{
    public static PayoutEligibilityDecision Eligible(
        decimal availableAmount,
        decimal minimumPayoutAmount
    )
    {
        return new PayoutEligibilityDecision(
            IsEligible: true,
            ReasonCode: PayoutReasonCodes.Eligible,
            Reason: "Payout requirements cleared. Funds are eligible for release extraction.",
            AvailableAmount: availableAmount,
            MinimumPayoutAmount: minimumPayoutAmount
        );
    }

    public static PayoutEligibilityDecision Ineligible(
        string reasonCode,
        string reason,
        decimal availableAmount,
        decimal minimumPayoutAmount
    )
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            throw new ArgumentException(
                "A valid machine-readable reason code is mandatory for ineligible decisions.",
                nameof(reasonCode)
            );

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "A human-readable administrative explanation must be provided.",
                nameof(reason)
            );

        return new PayoutEligibilityDecision(
            IsEligible: false,
            ReasonCode: reasonCode.Trim().ToLowerInvariant(),
            Reason: reason.Trim(),
            AvailableAmount: availableAmount,
            MinimumPayoutAmount: minimumPayoutAmount
        );
    }
}
