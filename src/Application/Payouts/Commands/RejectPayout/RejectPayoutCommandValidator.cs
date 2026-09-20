namespace modular_mlm.Application.Payouts.Commands.RejectPayout;

public sealed class RejectPayoutCommandValidator : AbstractValidator<RejectPayoutCommand>
{
    public RejectPayoutCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PayoutRequestId).NotEmpty();
    }
}
