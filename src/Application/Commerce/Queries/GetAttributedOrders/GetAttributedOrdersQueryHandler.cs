using modular_mlm.Application.Commerce.Queries.GetAttributedOrders.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrders;

public sealed class GetAttributedOrdersQueryHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<GetAttributedOrdersQuery, AttributedOrdersPageDto>
{
    public async Task<AttributedOrdersPageDto> Handle(
        GetAttributedOrdersQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var agent =
            await db
                .Agents.AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OrganizationId == request.OrganizationId
                        && candidate.UserId == userId,
                    cancellationToken
                )
            ?? throw new KeyNotFoundException("Current Agent profile was not found.");
        if (agent.Status != AgentStatus.Active)
            throw new InvalidOperationException("An active Agent is required.");
        var query = db
            .Orders.AsNoTracking()
            .Where(order =>
                order.OrganizationId == request.OrganizationId
                && order.AttributedAgentId == agent.Id
            );
        if (request.Status.HasValue)
            query = query.Where(order => order.Status == request.Status);
        if (request.From.HasValue)
            query = query.Where(order => order.Created >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(order => order.Created < request.To.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(order => order.Created)
            .ThenByDescending(order => order.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.Created,
                order.Status,
                order.PaymentStatus,
                order.Currency,
                order.GrandTotal,
                order.CustomerId,
                CommissionableAmount = order.Items.Sum(item => item.CommissionableAmount),
                BusinessVolume = order.Items.Sum(item => item.BusinessVolume),
                ItemCount = order.Items.Sum(item => item.Quantity),
                ProductNames = order
                    .Items.Select(item => item.ProductNameSnapshot)
                    .Distinct()
                    .ToArray(),
            })
            .ToListAsync(cancellationToken);
        var orderIds = rows.Select(row => row.Id).ToArray();
        var customerIds = rows.Select(row => row.CustomerId).Distinct().ToArray();
        var names = await db
            .CustomerProfiles.AsNoTracking()
            .Where(profile =>
                profile.OrganizationId == request.OrganizationId && customerIds.Contains(profile.Id)
            )
            .ToDictionaryAsync(
                profile => profile.Id,
                profile => profile.DisplayName,
                cancellationToken
            );
        var commissions = await db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.OrganizationId == request.OrganizationId
                && transaction.BeneficiaryAgentId == agent.Id
                && transaction.SourceOrderId.HasValue
                && orderIds.Contains(transaction.SourceOrderId.Value)
            )
            .GroupBy(transaction => transaction.SourceOrderId!.Value)
            .Select(group => new
            {
                OrderId = group.Key,
                Amount = group.Sum(transaction => transaction.Amount),
            })
            .ToDictionaryAsync(value => value.OrderId, value => value.Amount, cancellationToken);
        var items = rows.Select(row => new AttributedOrderSummaryDto(
                row.Id,
                row.OrderNumber,
                row.Created,
                row.Status,
                row.PaymentStatus,
                row.Currency,
                row.GrandTotal,
                row.CommissionableAmount,
                row.BusinessVolume,
                row.ItemCount,
                row.ProductNames,
                Mask(names.GetValueOrDefault(row.CustomerId)),
                commissions.GetValueOrDefault(row.Id)
            ))
            .ToArray();
        return new AttributedOrdersPageDto(items, request.Page, request.PageSize, totalCount);
    }

    private static string Mask(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Customer";
        var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(
            ' ',
            words.Select(word =>
                word.Length == 1
                    ? $"{word[0]}*"
                    : $"{word[0]}{new string('*', Math.Min(word.Length - 1, 4))}"
            )
        );
    }
}
