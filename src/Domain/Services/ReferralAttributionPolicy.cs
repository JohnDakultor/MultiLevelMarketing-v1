using modular_mlm.Domain.Organizations;

namespace modular_mlm.Domain.Referral;

public static class ReferralAttributionPolicy
{
    public static ReferralAttributionContext? Resolve(
        ReferralSettings settings,
        ReferralAttributionContext? incoming,
        ReferralAttributionContext? existing,
        bool hasCompletedFirstPurchase,
        DateTimeOffset now
    )
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (
            settings.ReferralLockAfterFirstPurchase
            && hasCompletedFirstPurchase
            && existing is not null
        )
            return existing;

        var existingIsValid =
            existing is not null
            && existing.CapturedAt.AddDays(settings.AttributionWindowDays) > now;

        var incomingIsValid =
            incoming is not null
            && incoming.CapturedAt.AddDays(settings.AttributionWindowDays) > now;

        if (!incomingIsValid)
            return existingIsValid ? existing : null;

        if (!existingIsValid)
            return incoming!;

        if (incoming!.AgentId == existing!.AgentId)
            return incoming;

        return settings.AllowReferralOverride ? incoming : existing;
    }
}
