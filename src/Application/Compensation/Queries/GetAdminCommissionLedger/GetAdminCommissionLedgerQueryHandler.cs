using modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger.Model;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger;

public sealed class GetAdminCommissionLedgerQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminCommissionLedgerQuery, AdminCommissionLedgerPageDto>
{
    public async Task<AdminCommissionLedgerPageDto> Handle(
        GetAdminCommissionLedgerQuery request,
        CancellationToken cancellationToken
    )
    {
        var currency = await db.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == request.OrganizationId)
            .Select(organization => organization.CurrencyCode)
            .SingleOrDefaultAsync(cancellationToken);
        if (currency is null)
            throw new KeyNotFoundException("Organization was not found.");

        if (
            request.AgentId.HasValue
            && !await db.Agents.AsNoTracking().AnyAsync(
                agent =>
                    agent.OrganizationId == request.OrganizationId
                    && agent.Id == request.AgentId.Value,
                cancellationToken
            )
        )
            throw new KeyNotFoundException("Agent was not found in this organization.");

        var query = db.CommissionTransactions.AsNoTracking().Where(transaction =>
            transaction.OrganizationId == request.OrganizationId
        );

        if (request.AgentId.HasValue)
            query = query.Where(transaction =>
                transaction.BeneficiaryAgentId == request.AgentId.Value
            );
        if (TryParseEnum<CommissionType>(request.CommissionType, out var commissionType))
            query = query.Where(transaction => transaction.Type == commissionType);
        if (TryParseEnum<CommissionStatus>(request.Status, out var status))
            query = query.Where(transaction => transaction.Status == status);
        if (request.From.HasValue)
        {
            var from = AsUtcOffset(request.From.Value);
            query = query.Where(transaction => transaction.Created >= from);
        }
        if (request.To.HasValue)
        {
            var to = AsUtcOffset(request.To.Value);
            query = query.Where(transaction => transaction.Created < to);
        }
        if (!request.IncludeReversals)
            query = query.Where(transaction => transaction.Type != CommissionType.Reversal);

        var totalCount = await query.CountAsync(cancellationToken);
        var totals =
            await query
                .GroupBy(_ => 1)
                .Select(group => new AdminCommissionLedgerTotalsDto(
                    group.Sum(transaction => transaction.Amount),
                    group.Where(transaction => transaction.Status == CommissionStatus.Pending)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                    group.Where(transaction => transaction.Status == CommissionStatus.Available)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                    group.Where(transaction => transaction.Status == CommissionStatus.Held)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                    group.Where(transaction => transaction.Status == CommissionStatus.Paid)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                    group.Where(transaction => transaction.Type == CommissionType.Reversal)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0m
                ))
                .SingleOrDefaultAsync(cancellationToken)
            ?? new AdminCommissionLedgerTotalsDto(0m, 0m, 0m, 0m, 0m, 0m);

        var items = await (
            from transaction in query
            join agent in db.Agents.AsNoTracking()
                on new
                {
                    AgentId = transaction.BeneficiaryAgentId,
                    transaction.OrganizationId,
                } equals new { AgentId = agent.Id, agent.OrganizationId }
            orderby transaction.Created descending, transaction.Id descending
            select new AdminCommissionLedgerItemDto(
                transaction.Id,
                transaction.BeneficiaryAgentId,
                agent.AgentCode,
                transaction.SourceOrderId,
                transaction.SourceOrderItemId,
                transaction.SourceAgentId,
                transaction.PairingRunId,
                transaction.CommissionPlanVersionId,
                transaction.RuleId,
                transaction.Type,
                transaction.BaseAmount,
                transaction.Rate,
                transaction.Amount,
                transaction.Status,
                transaction.AvailableAt,
                transaction.ReversalOfCommissionId,
                transaction.SourceOrderItemRefundId,
                transaction.Created
            )
        )
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new AdminCommissionLedgerPageDto(
            request.OrganizationId,
            currency,
            items,
            totals,
            request.Page,
            request.PageSize,
            totalCount
        );
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out result) && Enum.IsDefined(result);

    private static DateTimeOffset AsUtcOffset(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime()),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
        };
}
