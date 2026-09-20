using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Commerce.Commands.RequestCancellation;

[Authorize]
public sealed record RequestCancellationCommand(Guid OrganizationId, Guid OrderId, string Reason)
    : IRequest,
        IOrderScopedRequest;
