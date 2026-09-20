namespace modular_mlm.Application.Payouts.Commands.SubmitPayoutAccountForVerification;

public sealed class SubmitPayoutAccountForVerificationCommandValidator
    : AbstractValidator<SubmitPayoutAccountForVerificationCommand>
{
    public SubmitPayoutAccountForVerificationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.PayoutAccountId).NotEmpty();
    }
}
