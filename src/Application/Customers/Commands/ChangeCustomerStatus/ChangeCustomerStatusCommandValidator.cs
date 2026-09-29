namespace modular_mlm.Application.Customers.Commands.ChangeCustomerStatus;

public sealed class ChangeCustomerStatusCommandValidator
    : AbstractValidator<ChangeCustomerStatusCommand>
{
    public ChangeCustomerStatusCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
