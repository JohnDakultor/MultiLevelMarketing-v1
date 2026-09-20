namespace modular_mlm.Application.Payouts.Commands.ProcessPayout;

public sealed class ProcessPayoutCommandValidator : AbstractValidator<ProcessPayoutCommand>
{
    public ProcessPayoutCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PayoutRequestId).NotEmpty();
    }
}
