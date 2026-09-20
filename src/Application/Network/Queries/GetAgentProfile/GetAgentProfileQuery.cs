using modular_mlm.Application.Network.Queries.GetAgentProfile.Models;

namespace modular_mlm.Application.Network.Queries.GetAgentProfile;

public sealed record GetAgentProfileQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<AgentProfileDto?>,
        IAgentScopedRequest;
