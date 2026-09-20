using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory.Models;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionDetails;

public sealed record GetCommissionDetailsQuery(Guid OrganizationId, Guid AgentId, Guid CommissionId)
    : IRequest<CommissionHistoryItemDto?>,
        IAgentScopedRequest;
