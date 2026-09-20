using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class ReferralSettings
{
    private ReferralSettings() { }

    public int AttributionWindowDays { get; private set; }
    public bool AllowReferralOverride { get; private set; }
    public bool ReferralLockAfterFirstPurchase { get; private set; }

    public static ReferralSettings Default() => Create(30, false, true);

    public static ReferralSettings Create(
        int attributionWindowDays,
        bool allowReferralOverride,
        bool referralLockAfterFirstPurchase
    )
    {
        if (attributionWindowDays <= 0)
            throw new DomainInvariantException(
                "Attribution window must be greater than zero days."
            );

        return new ReferralSettings
        {
            AttributionWindowDays = attributionWindowDays,
            AllowReferralOverride = allowReferralOverride,
            ReferralLockAfterFirstPurchase = referralLockAfterFirstPurchase,
        };
    }
}
