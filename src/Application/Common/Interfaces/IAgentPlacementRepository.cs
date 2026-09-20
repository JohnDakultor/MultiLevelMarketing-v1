using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Common.Interfaces;

public interface IAgentPlacementRepository
{
    Task<IReadOnlyCollection<PlacementCandidate>> GetCandidatesAsync(
        Guid organizationId,
        Guid rootAgentId,
        int maxDepth,
        CancellationToken cancellationToken
    );

    Task<bool> TryPlaceAsync(
        Guid organizationId,
        Guid agentId,
        PlacementDecision decision,
        Action<Agent> onPlaced,
        CancellationToken cancellationToken
    );
}
