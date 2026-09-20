namespace modular_mlm.Application.Organizations.Queries.GetReferralSettings.Models;

public sealed record ReferralSettingsDto(
    int AttributionWindowDays,
    bool AllowReferralOverride,
    bool ReferralLockAfterFirstPurchase
);
