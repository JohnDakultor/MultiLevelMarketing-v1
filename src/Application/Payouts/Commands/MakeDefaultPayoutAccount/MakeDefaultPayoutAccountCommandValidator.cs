namespace modular_mlm.Application.Payouts.Commands.MakeDefaultPayoutAccount;

public sealed class MakeDefaultPayoutAccountCommandValidator
    : AbstractValidator<MakeDefaultPayoutAccountCommand>
{
    public MakeDefaultPayoutAccountCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.PayoutAccountId).NotEmpty();
    }
}
