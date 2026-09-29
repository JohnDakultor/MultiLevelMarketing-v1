using modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails.Models;

namespace modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails;

public sealed class GetAdminCustomerDetailsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminCustomerDetailsQuery, AdminCustomerDetailsDto?>
{
    public async Task<AdminCustomerDetailsDto?> Handle(
        GetAdminCustomerDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var customer = await db
            .CustomerProfiles.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.Id == request.CustomerId
            )
            .Select(candidate => new
            {
                candidate.Id,
                candidate.DisplayName,
                candidate.Email,
                candidate.Status,
                candidate.Created,
                candidate.LastModified,
                AddressCount = candidate.Addresses.Count,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null)
            return null;

        var orderQuery = db.Orders.AsNoTracking().Where(order =>
            order.OrganizationId == request.OrganizationId
            && order.CustomerId == request.CustomerId
        );
        var orderCount = await orderQuery.CountAsync(cancellationToken);
        var grossOrderValue = await orderQuery.SumAsync(
            order => (decimal?)order.GrandTotal,
            cancellationToken
        ) ?? 0m;
        var recentOrders = await orderQuery
            .OrderByDescending(order => order.Created)
            .Take(10)
            .Select(order => new AdminCustomerOrderSummaryDto(
                order.Id,
                order.OrderNumber,
                (int)order.Status,
                (int)order.PaymentStatus,
                order.GrandTotal,
                order.Currency,
                order.Created
            ))
            .ToListAsync(cancellationToken);

        return new AdminCustomerDetailsDto(
            customer.Id,
            customer.DisplayName,
            customer.Email,
            customer.Status,
            customer.Created,
            customer.LastModified,
            customer.AddressCount,
            orderCount,
            grossOrderValue,
            recentOrders
        );
    }
}
