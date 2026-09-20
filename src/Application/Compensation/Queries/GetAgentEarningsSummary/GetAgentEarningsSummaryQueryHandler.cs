using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary.Models;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary;

public sealed class GetAgentEarningsSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentEarningsSummaryQuery, AgentEarningsSummaryDto>
{
    public async Task<AgentEarningsSummaryDto> Handle(
        GetAgentEarningsSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var transactions = db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.OrganizationId == request.OrganizationId
                && transaction.BeneficiaryAgentId == request.AgentId
            );

        return new AgentEarningsSummaryDto(
            request.AgentId,
            await SumAsync(transactions, CommissionStatus.Pending, cancellationToken),
            await SumAsync(transactions, CommissionStatus.Available, cancellationToken),
            await SumAsync(transactions, CommissionStatus.Paid, cancellationToken),
            await transactions
                .Where(transaction => transaction.Type == CommissionType.DirectSale)
                .SumAsync(transaction => transaction.Amount, cancellationToken),
            await transactions
                .Where(transaction => transaction.Type == CommissionType.BinaryPairing)
                .SumAsync(transaction => transaction.Amount, cancellationToken)
        );
    }

    private static Task<decimal> SumAsync(
        IQueryable<CommissionTransaction> transactions,
        CommissionStatus status,
        CancellationToken cancellationToken
    ) =>
        transactions
            .Where(transaction => transaction.Status == status)
            .SumAsync(transaction => transaction.Amount, cancellationToken);
}
