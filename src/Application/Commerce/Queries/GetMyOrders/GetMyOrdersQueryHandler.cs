using modular_mlm.Application.Commerce.Queries.GetMyOrders.Models;

namespace modular_mlm.Application.Commerce.Queries.GetMyOrders;

public sealed class GetMyOrdersQueryHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<GetMyOrdersQuery, OrderSummariesPageDto>
{
    public async Task<OrderSummariesPageDto> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var customerId = await db
            .CustomerProfiles.AsNoTracking()
            .Where(customer =>
                customer.OrganizationId == request.OrganizationId && customer.UserId == userId
            )
            .Select(customer => (Guid?)customer.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!customerId.HasValue)
            throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );

        var orders = db
            .Orders.AsNoTracking()
            .Where(order =>
                order.OrganizationId == request.OrganizationId
                && order.CustomerId == customerId.Value
            );
        if (request.Status.HasValue)
            orders = orders.Where(order => order.Status == request.Status.Value);

        var totalCount = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(order => order.Created)
            .ThenByDescending(order => order.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new OrderSummaryDto(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PaymentStatus,
                order.Currency,
                order.GrandTotal,
                order.Items.Count,
                order.Created,
                order.PaidAt,
                order.DeliveredAt
            ))
            .ToListAsync(cancellationToken);

        return new OrderSummariesPageDto(request.Page, request.PageSize, totalCount, items);
    }
}
