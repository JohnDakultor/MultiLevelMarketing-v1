namespace modular_mlm.Application.Commerce.Commands.MarkOrderDelivered;

public sealed class MarkOrderDeliveredCommandValidator : AbstractValidator<MarkOrderDeliveredCommand>
{
    public MarkOrderDeliveredCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
    }
}
