using modular_mlm.Application.Referrals.Queries.GetAgentStorefront.Models;

namespace modular_mlm.Application.Referrals.Queries.GetAgentStorefront;

public sealed record GetAgentStorefrontQuery(
    Guid OrganizationId,
    string ReferralCode,
    int PageNumber = 1,
    int PageSize = 24
) : IRequest<AgentStorefrontDto>;
