using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetMyOrders;

[Authorize]
public sealed record GetMyOrdersQuery(
    Guid OrganizationId,
    int Page,
    int PageSize,
    OrderStatus? Status
) : IRequest<OrderSummariesPageDto>, ICurrentCustomerRequest;
