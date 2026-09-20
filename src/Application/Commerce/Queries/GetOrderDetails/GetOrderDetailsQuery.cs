using modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;
using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Commerce.Queries.GetOrderDetails;

[Authorize]
public sealed record GetOrderDetailsQuery(Guid OrganizationId, Guid OrderId)
    : IRequest<OrderDetailsDto>,
        IOrderScopedRequest;
