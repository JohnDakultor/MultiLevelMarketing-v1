namespace modular_mlm.Application.Payouts.Commands.ReconcilePayout;

public sealed class ReconcilePayoutCommandValidator : AbstractValidator<ReconcilePayoutCommand>
{
    public ReconcilePayoutCommandValidator() =>
        RuleFor(command => command.PayoutRequestId).NotEmpty();
}
