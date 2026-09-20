namespace modular_mlm.Application.Commerce.Commands.RequestPaymentRefund;

public sealed class RequestPaymentRefundCommandValidator
    : AbstractValidator<RequestPaymentRefundCommand>
{
    public RequestPaymentRefundCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0m);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
        RuleFor(command => command.Reason)
            .Must(reason =>
                reason is "duplicate" or "fraudulent" or "requested_by_customer" or "others"
            )
            .WithMessage("Refund reason is not supported by PayMongo.");
    }
}
