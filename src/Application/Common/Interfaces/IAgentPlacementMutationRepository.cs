using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Common.Interfaces;

public interface IAgentPlacementMutationRepository
{
    Task<bool> TryPlaceAsync(
        Guid organizationId,
        Guid agentId,
        PlacementDecision decision,
        Action<Agent> onPlaced,
        CancellationToken cancellationToken
    );

    Task<bool> TryMoveUncommittedAsync(
        Guid organizationId,
        Guid agentId,
        Guid newParentAgentId,
        PlacementSide newSide,
        Guid expectedCurrentParentAgentId,
        PlacementSide expectedCurrentSide,
        string actorUserId,
        string reason,
        DateTimeOffset occurredAt,
        Action<Agent> onMoved,
        CancellationToken cancellationToken
    );
}
