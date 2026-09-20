namespace modular_mlm.Application.Commerce.Commands.ReconcilePayment;

public sealed record ReconcilePaymentCommand(Guid OrganizationId, Guid PaymentId) : IRequest<bool>;
