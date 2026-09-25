using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger.Models;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger;

public sealed record GetBinaryVolumeLedgerQuery(
    Guid OrganizationId,
    Guid AgentId,
    int Page,
    int PageSize,
    DateTimeOffset? From,
    DateTimeOffset? To,
    PlacementSide? Side,
    BinaryVolumeEntryType? EntryType
) : IRequest<BinaryVolumeLedgerPageDto>, IAgentScopedRequest;
