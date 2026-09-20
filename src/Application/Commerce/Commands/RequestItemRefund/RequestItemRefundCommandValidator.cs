namespace modular_mlm.Application.Commerce.Commands.RequestItemRefund;

public sealed class RequestItemRefundCommandValidator : AbstractValidator<RequestItemRefundCommand>
{
    private static readonly string[] SupportedReasons =
    [
        "duplicate",
        "fraudulent",
        "requested_by_customer",
        "others",
    ];

    public RequestItemRefundCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.OrderItemId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0m);
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .Must(reason => SupportedReasons.Contains(reason.Trim().ToLowerInvariant()))
            .WithMessage("Reason is not supported by PayMongo.");
        RuleFor(command => command.AuditReason).MaximumLength(1_000);
    }
}
