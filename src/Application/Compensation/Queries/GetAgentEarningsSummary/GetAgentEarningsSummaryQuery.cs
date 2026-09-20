using modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary.Models;

namespace modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary;

public sealed record GetAgentEarningsSummaryQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<AgentEarningsSummaryDto>,
        IAgentScopedRequest;
