namespace modular_mlm.Application.Commerce.Commands.RequestCancellation;

public sealed class RequestCancellationCommandValidator
    : AbstractValidator<RequestCancellationCommand>
{
    public RequestCancellationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
