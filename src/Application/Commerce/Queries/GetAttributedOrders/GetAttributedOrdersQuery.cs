using modular_mlm.Application.Commerce.Queries.GetAttributedOrders.Models;
using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrders;

[Authorize(Roles = Roles.Agent)]
public sealed record GetAttributedOrdersQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
) : IRequest<AttributedOrdersPageDto>, ICurrentAgentRequest;
