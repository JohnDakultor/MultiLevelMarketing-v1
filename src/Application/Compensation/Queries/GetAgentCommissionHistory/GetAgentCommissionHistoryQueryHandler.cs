using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory.Models;

namespace modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory;

public sealed class GetAgentCommissionHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentCommissionHistoryQuery, IReadOnlyList<CommissionHistoryItemDto>>
{
    public async Task<IReadOnlyList<CommissionHistoryItemDto>> Handle(
        GetAgentCommissionHistoryQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.OrganizationId == request.OrganizationId
                && transaction.BeneficiaryAgentId == request.AgentId
            )
            .OrderByDescending(transaction => transaction.Created)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(transaction => new CommissionHistoryItemDto(
                transaction.Id,
                transaction.SourceOrderId,
                transaction.SourceOrderItemId,
                transaction.PairingRunId,
                transaction.Type,
                transaction.BaseAmount,
                transaction.Rate,
                transaction.Amount,
                transaction.Status,
                transaction.Created
            ))
            .ToListAsync(cancellationToken);
}
