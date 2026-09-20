using modular_mlm.Application.Referrals.Queries.GetReferralDashboard.Models;

namespace modular_mlm.Application.Referrals.Queries.GetReferralDashboard;

public sealed record GetReferralDashboardQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<ReferralDashboardDto>,
        IAgentScopedRequest;
