using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails.Models;

public sealed record AgentCommissionSummaryDto(
    Guid CommissionId,
    CommissionType Type,
    CommissionStatus Status,
    decimal Amount,
    Guid? SourceOrderItemId,
    Guid? ReversalOfCommissionId
);
