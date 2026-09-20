using modular_mlm.Application.Network.Queries.GetDirectPlacementChildren.Models;

namespace modular_mlm.Application.Network.Queries.GetDirectPlacementChildren;

public sealed record GetDirectPlacementChildrenQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<IReadOnlyList<PlacementChildDto>>,
        IAgentScopedRequest;
