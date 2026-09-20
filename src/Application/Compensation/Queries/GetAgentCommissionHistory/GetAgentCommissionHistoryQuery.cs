using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory.Models;

namespace modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory;

public sealed record GetAgentCommissionHistoryQuery(
    Guid OrganizationId,
    Guid AgentId,
    int Page = 1,
    int PageSize = 20
) : IRequest<IReadOnlyList<CommissionHistoryItemDto>>, IAgentScopedRequest;
