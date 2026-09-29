using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Customers.Queries.GetAdminCustomers.Models;

namespace modular_mlm.Application.Customers.Queries.GetAdminCustomers;

public sealed class GetAdminCustomersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminCustomersQuery, PagedResponse<AdminCustomerSummaryDto>>
{
    public async Task<PagedResponse<AdminCustomerSummaryDto>> Handle(
        GetAdminCustomersQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .CustomerProfiles.AsNoTracking()
            .Where(customer => customer.OrganizationId == request.OrganizationId);

        if (request.Status.HasValue)
            query = query.Where(customer => customer.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(customer =>
                customer.DisplayName.ToLower().Contains(search)
                || customer.Email.ToLower().Contains(search)
            );
        }

        query = request.Sort switch
        {
            CustomerSort.NameAscending => query.OrderBy(customer => customer.DisplayName),
            CustomerSort.NameDescending => query.OrderByDescending(customer => customer.DisplayName),
            _ => query.OrderByDescending(customer => customer.Created),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(customer => new AdminCustomerSummaryDto(
                customer.Id,
                customer.DisplayName,
                customer.Email,
                customer.Status,
                customer.Created,
                db.Orders.Count(order =>
                    order.OrganizationId == request.OrganizationId
                    && order.CustomerId == customer.Id
                ),
                db.Orders
                    .Where(order =>
                        order.OrganizationId == request.OrganizationId
                        && order.CustomerId == customer.Id
                    )
                    .Sum(order => (decimal?)order.GrandTotal) ?? 0m,
                db.Orders
                    .Where(order =>
                        order.OrganizationId == request.OrganizationId
                        && order.CustomerId == customer.Id
                    )
                    .Select(order => order.Currency)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResponse<AdminCustomerSummaryDto>(
            customers,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
