using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Commands.AdminMoveUncommittedPlacement;

public sealed record AdminMoveUncommittedPlacementCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid NewParentAgentId,
    PlacementSide NewSide,
    Guid ExpectedCurrentParentAgentId,
    PlacementSide ExpectedCurrentSide,
    string Reason
) : IRequest, IOrganizationAdminRequest;
