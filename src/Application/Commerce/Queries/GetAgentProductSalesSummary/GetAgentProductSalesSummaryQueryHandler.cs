using modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary;

public sealed class GetAgentProductSalesSummaryQueryHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<GetAgentProductSalesSummaryQuery, AgentProductSalesPageDto>
{
    public async Task<AgentProductSalesPageDto> Handle(
        GetAgentProductSalesSummaryQuery request,
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

        var orderItems = db
            .OrderItems.AsNoTracking()
            .Join(
                db.Orders.AsNoTracking(),
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { Item = item, Order = order }
            )
            .Where(row =>
                row.Order.OrganizationId == request.OrganizationId
                && row.Order.AttributedAgentId == agent.Id
                && (
                    row.Order.PaymentStatus == PaymentStatus.Paid
                    || row.Order.PaymentStatus == PaymentStatus.PartiallyRefunded
                    || row.Order.PaymentStatus == PaymentStatus.Refunded
                )
            );

        if (request.From.HasValue)
            orderItems = orderItems.Where(row => row.Order.Created >= request.From.Value);
        if (request.To.HasValue)
            orderItems = orderItems.Where(row => row.Order.Created < request.To.Value);

        var grouped = orderItems
            .Select(row => new
            {
                row.Item.Id,
                row.Item.ProductId,
                row.Item.ProductVariantId,
                ProductName = row.Item.ProductNameSnapshot,
                Sku = row.Item.SkuSnapshot,
                row.Order.Currency,
                Quantity = (decimal)row.Item.Quantity,
                Gross = row.Item.LineTotal,
                Commissionable = row.Item.CommissionableAmount,
                row.Item.BusinessVolume,
                RefundedQuantity = db.OrderItemRefunds.Where(refund =>
                        refund.OrganizationId == request.OrganizationId
                        && refund.OrderItemId == row.Item.Id
                        && refund.Status == OrderItemRefundStatus.Reversed
                    )
                    .Sum(refund => (decimal?)refund.Quantity)
                    ?? 0m,
                RefundedAmount = db.OrderItemRefunds.Where(refund =>
                        refund.OrganizationId == request.OrganizationId
                        && refund.OrderItemId == row.Item.Id
                        && refund.Status == OrderItemRefundStatus.Reversed
                    )
                    .Sum(refund => (decimal?)refund.RefundAmount)
                    ?? 0m,
                ReversedCommissionable = db.OrderItemRefunds.Where(refund =>
                        refund.OrganizationId == request.OrganizationId
                        && refund.OrderItemId == row.Item.Id
                        && refund.Status == OrderItemRefundStatus.Reversed
                    )
                    .Sum(refund => (decimal?)refund.CommissionableAmountToReverse)
                    ?? 0m,
                ReversedVolume = db.OrderItemRefunds.Where(refund =>
                        refund.OrganizationId == request.OrganizationId
                        && refund.OrderItemId == row.Item.Id
                        && refund.Status == OrderItemRefundStatus.Reversed
                    )
                    .Sum(refund => (decimal?)refund.BusinessVolumeToReverse)
                    ?? 0m,
            })
            .GroupBy(row => new
            {
                row.ProductId,
                row.ProductVariantId,
                row.ProductName,
                row.Sku,
                row.Currency,
            })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.ProductVariantId,
                group.Key.ProductName,
                group.Key.Sku,
                group.Key.Currency,
                QuantitySold = group.Sum(row => row.Quantity - row.RefundedQuantity),
                GrossAttributedSales = group.Sum(row => row.Gross - row.RefundedAmount),
                CommissionableSales = group.Sum(row =>
                    row.Commissionable - row.ReversedCommissionable
                ),
                BusinessVolume = group.Sum(row => row.BusinessVolume - row.ReversedVolume),
            });

        var totalCount = await grouped.CountAsync(cancellationToken);
        var rows = await grouped
            .OrderByDescending(row => row.GrossAttributedSales)
            .ThenBy(row => row.ProductName)
            .ThenBy(row => row.Sku)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var variantIds = rows.Select(row => row.ProductVariantId).ToArray();
        var commissionByVariant = await db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.OrganizationId == request.OrganizationId
                && transaction.BeneficiaryAgentId == agent.Id
                && transaction.SourceOrderItemId.HasValue
            )
            .Join(
                orderItems.Where(row => variantIds.Contains(row.Item.ProductVariantId)),
                transaction => transaction.SourceOrderItemId!.Value,
                row => row.Item.Id,
                (transaction, row) => new { row.Item.ProductVariantId, transaction.Amount }
            )
            .GroupBy(row => row.ProductVariantId)
            .Select(group => new
            {
                ProductVariantId = group.Key,
                Amount = group.Sum(value => value.Amount),
            })
            .ToDictionaryAsync(row => row.ProductVariantId, row => row.Amount, cancellationToken);

        var items = rows.Select(row => new AgentProductSalesSummaryDto(
                row.ProductId,
                row.ProductVariantId,
                row.ProductName,
                row.Sku,
                row.QuantitySold,
                row.Currency,
                row.GrossAttributedSales,
                row.CommissionableSales,
                row.BusinessVolume,
                commissionByVariant.GetValueOrDefault(row.ProductVariantId)
            ))
            .ToArray();

        return new AgentProductSalesPageDto(items, request.Page, request.PageSize, totalCount);
    }
}
