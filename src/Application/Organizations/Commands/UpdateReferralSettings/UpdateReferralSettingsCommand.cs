namespace modular_mlm.Application.Organizations.Commands.UpdateReferralSettings;

public sealed record UpdateReferralSettingsCommand(
    Guid OrganizationId,
    int AttributionWindowDays,
    bool AllowReferralOverride,
    bool ReferralLockAfterFirstPurchase
) : IRequest, IOrganizationAdminRequest;
