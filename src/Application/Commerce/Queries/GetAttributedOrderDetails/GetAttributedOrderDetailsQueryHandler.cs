using modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails;

public sealed class GetAttributedOrderDetailsQueryHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<GetAttributedOrderDetailsQuery, AttributedOrderDetailsDto?>
{
    public async Task<AttributedOrderDetailsDto?> Handle(
        GetAttributedOrderDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var agentId = await db
            .Agents.AsNoTracking()
            .Where(agent =>
                agent.OrganizationId == request.OrganizationId && agent.UserId == userId
            )
            .Select(agent => (Guid?)agent.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!agentId.HasValue)
            return null;
        var order = await db
            .Orders.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.Id == request.OrderId
                && candidate.AttributedAgentId == agentId.Value
            )
            .Select(candidate => new
            {
                candidate.Id,
                candidate.OrderNumber,
                candidate.Created,
                candidate.PaidAt,
                candidate.DeliveredAt,
                candidate.Status,
                candidate.PaymentStatus,
                candidate.Currency,
                candidate.GrandTotal,
                candidate.CustomerId,
                Items = candidate
                    .Items.Select(item => new AttributedOrderItemDto(
                        item.Id,
                        item.ProductId,
                        item.ProductVariantId,
                        item.ProductNameSnapshot,
                        item.SkuSnapshot,
                        item.Quantity,
                        item.FulfillmentStatus,
                        item.CommissionableAmount,
                        item.BusinessVolume
                    ))
                    .ToArray(),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;
        var customerName = await db
            .CustomerProfiles.AsNoTracking()
            .Where(profile =>
                profile.OrganizationId == request.OrganizationId && profile.Id == order.CustomerId
            )
            .Select(profile => profile.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);
        var commissions = await db
            .CommissionTransactions.AsNoTracking()
            .Where(transaction =>
                transaction.OrganizationId == request.OrganizationId
                && transaction.BeneficiaryAgentId == agentId.Value
                && transaction.SourceOrderId == order.Id
            )
            .OrderBy(transaction => transaction.Created)
            .Select(transaction => new AgentCommissionSummaryDto(
                transaction.Id,
                transaction.Type,
                transaction.Status,
                transaction.Amount,
                transaction.SourceOrderItemId,
                transaction.ReversalOfCommissionId
            ))
            .ToListAsync(cancellationToken);
        return new AttributedOrderDetailsDto(
            order.Id,
            order.OrderNumber,
            order.Created,
            order.PaidAt,
            order.DeliveredAt,
            order.Status,
            order.PaymentStatus,
            order.Currency,
            order.GrandTotal,
            Mask(customerName),
            order.Items,
            commissions
        );
    }

    private static string Mask(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Customer";
        return string.Join(
            ' ',
            name.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                    $"{word[0]}{new string('*', Math.Min(Math.Max(word.Length - 1, 1), 4))}"
                )
        );
    }
}
