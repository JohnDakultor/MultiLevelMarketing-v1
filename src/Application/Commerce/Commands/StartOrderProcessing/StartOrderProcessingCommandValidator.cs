namespace modular_mlm.Application.Commerce.Commands.StartOrderProcessing;

public sealed class StartOrderProcessingCommandValidator : AbstractValidator<StartOrderProcessingCommand>
{
    public StartOrderProcessingCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
    }
}
