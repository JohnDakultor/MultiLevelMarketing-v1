namespace modular_mlm.Application.Payouts.Commands.ApprovePayout;

public sealed class ApprovePayoutCommandValidator : AbstractValidator<ApprovePayoutCommand>
{
    public ApprovePayoutCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PayoutRequestId).NotEmpty();
    }
}
