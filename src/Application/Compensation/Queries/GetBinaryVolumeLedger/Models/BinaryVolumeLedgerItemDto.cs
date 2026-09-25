using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger.Models;

public sealed record BinaryVolumeLedgerItemDto(
    Guid Id,
    Guid OwnerAgentId,
    Guid? SourceAgentId,
    Guid? SourceOrderItemId,
    Guid? PairingRunId,
    PlacementSide Side,
    decimal Volume,
    BinaryVolumeEntryType EntryType,
    DateTimeOffset EffectiveAt,
    Guid? ReversalOfEntryId,
    Guid? SourceOrderItemRefundId
);
