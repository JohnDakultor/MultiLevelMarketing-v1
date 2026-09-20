using modular_mlm.Application.Commerce.Queries.GetAdminOrders.Models;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetAdminOrders;

public sealed record GetAdminOrdersQuery(
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Search,
    OrderStatus? Status,
    PaymentStatus? PaymentStatus,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo
) : IRequest<AdminOrdersPageDto>, IOrganizationAdminRequest;
