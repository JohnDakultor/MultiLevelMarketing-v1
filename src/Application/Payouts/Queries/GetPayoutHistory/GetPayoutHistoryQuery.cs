using modular_mlm.Application.Payouts.Queries.GetPayoutHistory.Models;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutHistory;

public sealed record GetPayoutHistoryQuery(
    Guid OrganizationId,
    Guid? AgentId,
    int Page = 1,
    int PageSize = 20
) : IRequest<IReadOnlyList<PayoutHistoryItemDto>>, INullableAgentScopedRequest;
