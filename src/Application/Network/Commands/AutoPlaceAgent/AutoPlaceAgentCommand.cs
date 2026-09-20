using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Network.Commands.AutoPlaceAgent;

/// <summary>
/// Requests automatic placement of an unplaced agent in an organization's binary network.
/// </summary>
public sealed record AutoPlaceAgentCommand(
    Guid OrganizationId,
    Guid AgentId,
    PlacementStrategyType? Strategy = null,
    PlacementSide? PreferredSide = null
) : IRequest<PlacementDecision>, IOrganizationAdminRequest;
