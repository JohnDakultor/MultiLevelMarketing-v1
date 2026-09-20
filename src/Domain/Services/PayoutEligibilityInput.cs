// PSEUDOCODE ONLY - immutable inputs required for a payout decision.
//
// DEFINE PayoutEligibilityInput containing:
//     AgentStatus
//     WalletStatus
//     HasVerifiedPayoutAccount: bool
//     RequestedAmount: decimal
//     RequestedCurrency: string
//     WalletCurrency: string
//     WalletBalance: WalletBalanceSnapshot
//     WalletSettings
//     HasComplianceOrAdministratorHold: bool
//
// Keep this model persistence-agnostic: no EF entities, provider clients, or current-user service.
