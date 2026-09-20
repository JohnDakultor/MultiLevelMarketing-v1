using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Network;

public sealed class PlacementClosure : OrganizationEntity
{
    private PlacementClosure() { }

    public Guid AncestorAgentId { get; private set; }
    public Guid DescendantAgentId { get; private set; }
    public int Depth { get; private set; }
    public PlacementSide FirstLeg { get; private set; }

    public static PlacementClosure Create(
        Guid organizationId,
        Guid ancestorId,
        Guid descendantId,
        int depth,
        PlacementSide firstLeg
    )
    {
        if (organizationId == Guid.Empty || ancestorId == Guid.Empty || descendantId == Guid.Empty)
            throw new DomainInvariantException("Placement identities are required.");
        if (ancestorId == descendantId || depth < 1)
            throw new DomainInvariantException(
                "Placement closure must connect distinct agents at a positive depth."
            );
        return new PlacementClosure
        {
            OrganizationId = organizationId,
            AncestorAgentId = ancestorId,
            DescendantAgentId = descendantId,
            Depth = depth,
            FirstLeg = firstLeg,
        };
    }
}
