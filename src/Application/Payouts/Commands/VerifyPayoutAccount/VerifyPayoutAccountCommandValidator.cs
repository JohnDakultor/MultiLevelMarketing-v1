namespace modular_mlm.Application.Payouts.Commands.VerifyPayoutAccount;

public sealed class VerifyPayoutAccountCommandValidator
    : AbstractValidator<VerifyPayoutAccountCommand>
{
    public VerifyPayoutAccountCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PayoutAccountId).NotEmpty();
    }
}
