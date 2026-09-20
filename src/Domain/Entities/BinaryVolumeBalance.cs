using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Compensation;

public sealed class BinaryVolumeBalance : OrganizationEntity
{
    private BinaryVolumeBalance() { }

    public Guid AgentId { get; private set; }
    public decimal LeftAvailable { get; private set; }
    public decimal RightAvailable { get; private set; }
    public decimal LeftLifetime { get; private set; }
    public decimal RightLifetime { get; private set; }
    public long Version { get; private set; }

    public static BinaryVolumeBalance Open(Guid organizationId, Guid agentId) =>
        organizationId == Guid.Empty || agentId == Guid.Empty
            ? throw new DomainInvariantException("Organization and agent are required.")
            : new BinaryVolumeBalance { OrganizationId = organizationId, AgentId = agentId };

    public void Credit(PlacementSide side, decimal volume)
    {
        if (volume <= 0)
            throw new DomainInvariantException("Volume must be positive.");
        if (side == PlacementSide.Left)
        {
            LeftAvailable += volume;
            LeftLifetime += volume;
        }
        else
        {
            RightAvailable += volume;
            RightLifetime += volume;
        }
        Version++;
    }

    public void Consume(decimal left, decimal right)
    {
        if (left < 0 || right < 0 || left > LeftAvailable || right > RightAvailable)
            throw new DomainInvariantException("Volume consumption exceeds available balance.");
        LeftAvailable -= left;
        RightAvailable -= right;
        Version++;
    }

    public void ReverseCredit(PlacementSide side, decimal volume)
    {
        if (volume <= 0)
            throw new DomainInvariantException("Reversed volume must be positive.");
        if (side == PlacementSide.Left)
        {
            LeftAvailable -= volume;
            LeftLifetime -= volume;
        }
        else
        {
            RightAvailable -= volume;
            RightLifetime -= volume;
        }
        Version++;
    }
}
