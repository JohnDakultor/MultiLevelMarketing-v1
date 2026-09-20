using modular_mlm.Application.Network.Queries.GetAgentQualificationStatus.Models;

namespace modular_mlm.Application.Network.Queries.GetAgentQualificationStatus;

public sealed record GetAgentQualificationStatusQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<AgentQualificationStatusDto>,
        IAgentScopedRequest;
