namespace modular_mlm.Application.Commerce.Commands.HandlePaymentWebhook;

public sealed class HandlePaymentWebhookCommandValidator
    : AbstractValidator<HandlePaymentWebhookCommand>
{
    public HandlePaymentWebhookCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty().MaximumLength(200);
        RuleFor(command => command.EventType).NotEmpty().MaximumLength(100);
        RuleFor(command => command.CheckoutSessionId).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ReferenceNumber).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PayloadHash).NotEmpty().MaximumLength(128);
    }
}
