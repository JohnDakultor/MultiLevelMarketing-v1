namespace modular_mlm.Application.Commerce.Commands.ReconcilePayment;

public sealed class ReconcilePaymentCommandValidator : AbstractValidator<ReconcilePaymentCommand>
{
    public ReconcilePaymentCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PaymentId).NotEmpty();
    }
}
