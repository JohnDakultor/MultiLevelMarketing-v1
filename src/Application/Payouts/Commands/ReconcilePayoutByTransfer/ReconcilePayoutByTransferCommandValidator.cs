namespace modular_mlm.Application.Payouts.Commands.ReconcilePayoutByTransfer;

public sealed class ReconcilePayoutByTransferCommandValidator
    : AbstractValidator<ReconcilePayoutByTransferCommand>
{
    public ReconcilePayoutByTransferCommandValidator() =>
        RuleFor(command => command.ProviderTransferId).NotEmpty().MaximumLength(200);
}
