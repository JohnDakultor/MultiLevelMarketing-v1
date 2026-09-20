using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Compensation.Services;

public static class BinaryVolumePropagationService
{
    public static IReadOnlyList<BinaryVolumeEntry> CreateCredits(
        Guid organizationId,
        Guid sourceAgentId,
        Guid sourceOrderItemId,
        decimal volume,
        IEnumerable<PlacementClosure> ancestors,
        DateTimeOffset effectiveAt
    )
    {
        if (volume <= 0)
            return [];

        return ancestors
            .Where(closure =>
                closure.OrganizationId == organizationId
                && closure.DescendantAgentId == sourceAgentId
            )
            .Select(closure =>
                BinaryVolumeEntry.Credit(
                    organizationId,
                    closure.AncestorAgentId,
                    sourceAgentId,
                    sourceOrderItemId,
                    closure.FirstLeg,
                    volume,
                    effectiveAt
                )
            )
            .ToArray();
    }
}
