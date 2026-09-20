namespace modular_mlm.Application.Commerce.Commands.ReconcileItemRefund;

public sealed class ReconcileItemRefundCommandValidator
    : AbstractValidator<ReconcileItemRefundCommand>
{
    public ReconcileItemRefundCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderItemRefundId).NotEmpty();
    }
}
