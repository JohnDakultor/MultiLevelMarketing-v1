namespace modular_mlm.Application.Payouts.Commands.CancelPayout;

public sealed class CancelPayoutCommandValidator : AbstractValidator<CancelPayoutCommand>
{
    public CancelPayoutCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.PayoutRequestId).NotEmpty();
    }
}
