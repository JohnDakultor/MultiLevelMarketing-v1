using modular_mlm.Application.Referrals.Queries.GetReferralLink.Models;

namespace modular_mlm.Application.Referrals.Queries.GetReferralLink;

public sealed record GetReferralLinkQuery(Guid OrganizationId, Guid AgentId, Guid? ProductId = null)
    : IRequest<ReferralLinkDto>,
        IAgentScopedRequest;
