using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Commerce.Commands.RequestItemRefund;

[Authorize]
public sealed record RequestItemRefundCommand(
    Guid OrganizationId,
    Guid OrderId,
    Guid OrderItemId,
    decimal Quantity,
    string Reason,
    string? AuditReason = null
) : IRequest<Guid>, IOrderScopedRequest;
