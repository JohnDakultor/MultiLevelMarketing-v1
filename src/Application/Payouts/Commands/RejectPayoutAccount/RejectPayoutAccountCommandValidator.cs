namespace modular_mlm.Application.Payouts.Commands.RejectPayoutAccount;

public sealed class RejectPayoutAccountCommandValidator
    : AbstractValidator<RejectPayoutAccountCommand>
{
    public RejectPayoutAccountCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PayoutAccountId).NotEmpty();
    }
}
