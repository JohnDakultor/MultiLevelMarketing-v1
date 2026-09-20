using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Commerce.Commands.RequestRefund;

[Authorize]
public sealed record RequestRefundCommand(
    Guid OrganizationId,
    Guid OrderId,
    Guid OrderItemId,
    int Quantity,
    string Reason
) : IRequest<Guid>, IOrderScopedRequest;
