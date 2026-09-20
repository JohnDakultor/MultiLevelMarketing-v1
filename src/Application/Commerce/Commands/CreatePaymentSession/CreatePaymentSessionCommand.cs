using modular_mlm.Application.Commerce.Commands.CreatePaymentSession.Models;

namespace modular_mlm.Application.Commerce.Commands.CreatePaymentSession;

public sealed record CreatePaymentSessionCommand(
    Guid OrganizationId,
    Guid OrderId,
    Uri SuccessUrl,
    Uri CancelUrl,
    IReadOnlyList<string> PaymentMethodTypes
) : IRequest<PaymentSessionDto>, IOrderScopedRequest;
