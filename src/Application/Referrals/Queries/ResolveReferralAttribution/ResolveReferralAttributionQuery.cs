using modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution.Models;

namespace modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution;

public sealed record ResolveReferralAttributionQuery(Guid OrganizationId, string ReferralCode)
    : IRequest<ReferralAttributionDto?>;
