namespace modular_mlm.Application.Commerce.Commands.RequestRefund;

public sealed class RequestRefundCommandValidator : AbstractValidator<RequestRefundCommand>
{
    public RequestRefundCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.OrderItemId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1_000);
    }
}
