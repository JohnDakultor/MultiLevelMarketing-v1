using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Commands.PlaceAgent;

public sealed record PlaceAgentCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid ParentAgentId,
    PlacementSide Side
) : IRequest, IOrganizationAdminRequest;
