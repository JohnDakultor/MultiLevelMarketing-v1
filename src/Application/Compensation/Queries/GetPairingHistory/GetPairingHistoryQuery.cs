using modular_mlm.Application.Compensation.Queries.GetPairingHistory.Models;

namespace modular_mlm.Application.Compensation.Queries.GetPairingHistory;

public sealed record GetPairingHistoryQuery(
    Guid OrganizationId,
    Guid AgentId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd
) : IRequest<List<BinaryPairingRunDto>>, IAgentScopedRequest;
