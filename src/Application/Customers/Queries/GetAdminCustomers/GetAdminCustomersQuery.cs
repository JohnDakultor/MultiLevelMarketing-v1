using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Customers.Queries.GetAdminCustomers.Models;
using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Queries.GetAdminCustomers;

public enum CustomerSort
{
    Newest,
    NameAscending,
    NameDescending,
}

public sealed record GetAdminCustomersQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    CustomerStatus? Status = null,
    CustomerSort Sort = CustomerSort.Newest
) : IRequest<PagedResponse<AdminCustomerSummaryDto>>, IOrganizationAdminRequest;
