namespace modular_mlm.Application.Commerce.Commands.ReconcilePaymentByProvider;

public sealed class ReconcilePaymentByProviderCommandValidator
    : AbstractValidator<ReconcilePaymentByProviderCommand>
{
    public ReconcilePaymentByProviderCommandValidator() =>
        RuleFor(command => command.ProviderPaymentId).NotEmpty().MaximumLength(200);
}
