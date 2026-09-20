using modular_mlm.Application.Network.Queries.GetAgentLegSummary.Models;

namespace modular_mlm.Application.Network.Queries.GetAgentLegSummary;

public sealed record GetAgentLegSummaryQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<AgentLegSummaryDto>,
        IAgentScopedRequest;
