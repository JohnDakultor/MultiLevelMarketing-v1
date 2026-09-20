using modular_mlm.Application.Network.Queries.GetPlacementAncestors.Models;

namespace modular_mlm.Application.Network.Queries.GetPlacementAncestors;

public sealed record GetPlacementAncestorsQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<IReadOnlyList<PlacementAncestorDto>>,
        IAgentScopedRequest;
