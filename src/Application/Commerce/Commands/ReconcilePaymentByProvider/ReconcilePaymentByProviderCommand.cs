namespace modular_mlm.Application.Commerce.Commands.ReconcilePaymentByProvider;

public sealed record ReconcilePaymentByProviderCommand(string ProviderPaymentId) : IRequest<bool>;
