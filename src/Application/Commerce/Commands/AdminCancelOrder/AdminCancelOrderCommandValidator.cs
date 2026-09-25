namespace modular_mlm.Application.Commerce.Commands.AdminCancelOrder;

public sealed class AdminCancelOrderCommandValidator : AbstractValidator<AdminCancelOrderCommand>
{
    public AdminCancelOrderCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
