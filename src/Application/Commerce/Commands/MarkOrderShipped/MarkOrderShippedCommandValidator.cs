namespace modular_mlm.Application.Commerce.Commands.MarkOrderShipped;

public sealed class MarkOrderShippedCommandValidator : AbstractValidator<MarkOrderShippedCommand>
{
    public MarkOrderShippedCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Carrier).MaximumLength(100);
        RuleFor(command => command.TrackingNumber).MaximumLength(200);
        RuleFor(command => command)
            .Must(command => string.IsNullOrWhiteSpace(command.Carrier) == string.IsNullOrWhiteSpace(command.TrackingNumber))
            .WithMessage("Carrier and tracking number must be supplied together.");
    }
}
