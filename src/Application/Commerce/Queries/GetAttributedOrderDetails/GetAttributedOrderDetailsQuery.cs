using modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails.Models;
using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails;

[Authorize(Roles = Roles.Agent)]
public sealed record GetAttributedOrderDetailsQuery(Guid OrganizationId, Guid OrderId)
    : IRequest<AttributedOrderDetailsDto?>,
        ICurrentAgentOrderRequest;
