namespace modular_mlm.Application.Common.Features;

public static class OrganizationFeatureRequirementResolver
{
    private static readonly HashSet<string> SettlementRequests = new(StringComparer.Ordinal)
    {
        "HandlePaymentWebhookCommand",
        "ReconcilePaymentByProviderCommand",
        "ReconcilePaymentCommand",
        "ReconcilePaymentForOrganizationCommand",
        "ProcessItemRefundReversalCommand",
        "ReconcilePayoutByTransferCommand",
        "ReconcilePayoutCommand",
        "ReconcilePayoutForOrganizationCommand",
    };

    public static OrganizationFeature? Resolve(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        if (SettlementRequests.Contains(requestType.Name))
            return null;

        var requestNamespace = requestType.Namespace ?? string.Empty;
        if (requestNamespace.Contains(".Catalog.", StringComparison.Ordinal)
            || requestNamespace.Contains(".Commerce.", StringComparison.Ordinal))
            return OrganizationFeature.Commerce;
        if (requestNamespace.Contains(".Payouts.", StringComparison.Ordinal))
            return OrganizationFeature.Payout;
        if (requestNamespace.Contains(".Wallets.", StringComparison.Ordinal))
            return OrganizationFeature.Wallet;
        if (requestNamespace.Contains(".Referrals.", StringComparison.Ordinal))
            return OrganizationFeature.AgentProgram;
        if (requestNamespace.Contains(".Network.", StringComparison.Ordinal))
        {
            if (ContainsAny(requestType.Name, "Binary", "Placement", "Downline", "PreferredLeg"))
                return OrganizationFeature.BinaryNetwork;
            return OrganizationFeature.AgentProgram;
        }
        if (requestNamespace.Contains(".Compensation.", StringComparison.Ordinal))
        {
            if (requestType.Name.Contains("Pairing", StringComparison.Ordinal))
                return OrganizationFeature.BinaryPairing;
            if (requestType.Name.Contains("BinaryVolume", StringComparison.Ordinal))
                return OrganizationFeature.BinaryNetwork;
            return OrganizationFeature.AgentProgram;
        }
        return null;
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
}
