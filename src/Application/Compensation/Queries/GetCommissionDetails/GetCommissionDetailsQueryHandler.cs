using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory.Models;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionDetails;

public sealed class GetCommissionDetailsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCommissionDetailsQuery, CommissionHistoryItemDto?>
{
    public async Task<CommissionHistoryItemDto?> Handle(
        GetCommissionDetailsQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.Id == request.CommissionId
                && transaction.OrganizationId == request.OrganizationId
                && transaction.BeneficiaryAgentId == request.AgentId
            )
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
            .SingleOrDefaultAsync(cancellationToken);
}
