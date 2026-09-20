using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Compensation;

public sealed class BinaryVolumeEntry : OrganizationEntity
{
    private BinaryVolumeEntry() { }

    public Guid OwnerAgentId { get; private set; }
    public Guid? SourceAgentId { get; private set; }
    public Guid? SourceOrderItemId { get; private set; }
    public Guid? PairingRunId { get; private set; }
    public PlacementSide Side { get; private set; }
    public decimal Volume { get; private set; }
    public BinaryVolumeEntryType EntryType { get; private set; }
    public DateTimeOffset EffectiveAt { get; private set; }
    public Guid? ReversalOfEntryId { get; private set; }
    public Guid? SourceOrderItemRefundId { get; private set; }

    public static BinaryVolumeEntry Credit(
        Guid organizationId,
        Guid ownerId,
        Guid sourceAgentId,
        Guid orderItemId,
        PlacementSide side,
        decimal volume,
        DateTimeOffset effectiveAt
    )
    {
        if (volume <= 0)
            throw new DomainInvariantException("Credited volume must be positive.");
        return new BinaryVolumeEntry
        {
            OrganizationId = organizationId,
            OwnerAgentId = ownerId,
            SourceAgentId = sourceAgentId,
            SourceOrderItemId = orderItemId,
            Side = side,
            Volume = volume,
            EntryType = BinaryVolumeEntryType.Credit,
            EffectiveAt = effectiveAt,
        };
    }

    public BinaryVolumeEntry Reverse(DateTimeOffset effectiveAt) =>
        new()
        {
            OrganizationId = OrganizationId,
            OwnerAgentId = OwnerAgentId,
            SourceAgentId = SourceAgentId,
            SourceOrderItemId = SourceOrderItemId,
            Side = Side,
            Volume = -Volume,
            EntryType = BinaryVolumeEntryType.Reversal,
            EffectiveAt = effectiveAt,
            ReversalOfEntryId = Id,
        };

    public BinaryVolumeEntry ReverseForRefund(
        Guid orderItemRefundId,
        decimal volume,
        DateTimeOffset effectiveAt
    )
    {
        if (orderItemRefundId == Guid.Empty || volume <= 0m || volume > Volume)
            throw new DomainInvariantException("Refund volume reversal values are invalid.");

        return new BinaryVolumeEntry
        {
            OrganizationId = OrganizationId,
            OwnerAgentId = OwnerAgentId,
            SourceAgentId = SourceAgentId,
            SourceOrderItemId = SourceOrderItemId,
            SourceOrderItemRefundId = orderItemRefundId,
            Side = Side,
            Volume = -volume,
            EntryType = BinaryVolumeEntryType.Reversal,
            EffectiveAt = effectiveAt,
            ReversalOfEntryId = Id,
        };
    }

    public static BinaryVolumeEntry PairConsumption(
        Guid organizationId,
        Guid ownerAgentId,
        Guid pairingRunId,
        PlacementSide side,
        decimal volume,
        DateTimeOffset effectiveAt
    )
    {
        if (
            organizationId == Guid.Empty
            || ownerAgentId == Guid.Empty
            || pairingRunId == Guid.Empty
        )
            throw new DomainInvariantException("Pairing consumption identity is required.");
        if (volume <= 0m)
            throw new DomainInvariantException("Consumed volume must be positive.");

        return new BinaryVolumeEntry
        {
            OrganizationId = organizationId,
            OwnerAgentId = ownerAgentId,
            PairingRunId = pairingRunId,
            Side = side,
            Volume = -volume,
            EntryType = BinaryVolumeEntryType.PairConsumption,
            EffectiveAt = effectiveAt,
        };
    }
}
