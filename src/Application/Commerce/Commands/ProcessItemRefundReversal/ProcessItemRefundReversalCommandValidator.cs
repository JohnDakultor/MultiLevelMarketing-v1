namespace modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;

public sealed class ProcessItemRefundReversalCommandValidator
    : AbstractValidator<ProcessItemRefundReversalCommand>
{
    public ProcessItemRefundReversalCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderItemRefundId).NotEmpty();
    }
}
