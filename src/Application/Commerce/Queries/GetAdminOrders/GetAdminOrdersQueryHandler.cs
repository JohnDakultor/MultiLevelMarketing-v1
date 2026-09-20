using modular_mlm.Application.Commerce.Queries.GetAdminOrders.Models;

namespace modular_mlm.Application.Commerce.Queries.GetAdminOrders;

public sealed class GetAdminOrdersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminOrdersQuery, AdminOrdersPageDto>
{
    public async Task<AdminOrdersPageDto> Handle(
        GetAdminOrdersQuery request,
        CancellationToken cancellationToken
    )
    {
        var orders =
            from order in db.Orders.AsNoTracking()
            join customer in db.CustomerProfiles.AsNoTracking()
                on order.CustomerId equals customer.Id
            where
                order.OrganizationId == request.OrganizationId
                && customer.OrganizationId == request.OrganizationId
            select new { Order = order, Customer = customer };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            orders = orders.Where(row =>
                row.Order.OrderNumber.ToLower().Contains(search)
                || row.Customer.DisplayName.ToLower().Contains(search)
            );
        }

        if (request.Status.HasValue)
            orders = orders.Where(row => row.Order.Status == request.Status.Value);
        if (request.PaymentStatus.HasValue)
            orders = orders.Where(row => row.Order.PaymentStatus == request.PaymentStatus.Value);
        if (request.CreatedFrom.HasValue)
            orders = orders.Where(row => row.Order.Created >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            orders = orders.Where(row => row.Order.Created <= request.CreatedTo.Value);

        var totalCount = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(row => row.Order.Created)
            .ThenByDescending(row => row.Order.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new AdminOrderSummaryDto(
                row.Order.Id,
                row.Order.OrderNumber,
                row.Order.CustomerId,
                row.Customer.DisplayName,
                row.Order.Status,
                row.Order.PaymentStatus,
                row.Order.Currency,
                row.Order.GrandTotal,
                row.Order.Items.Count,
                row.Order.Created,
                row.Order.PaidAt,
                row.Order.DeliveredAt
            ))
            .ToListAsync(cancellationToken);

        return new AdminOrdersPageDto(items, request.Page, request.PageSize, totalCount);
    }
}
