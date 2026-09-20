namespace modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution.Models;

public sealed record ReferralAttributionDto(
    Guid OrganizationId,
    Guid AgentId,
    string AgentCode,
    string ReferralCode
);
