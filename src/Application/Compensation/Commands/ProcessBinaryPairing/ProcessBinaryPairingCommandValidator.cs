namespace modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;

public sealed class ProcessBinaryPairingCommandValidator
    : AbstractValidator<ProcessBinaryPairingCommand>
{
    public ProcessBinaryPairingCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.CommissionPlanId).NotEmpty();
        RuleFor(x => x.PeriodStart).LessThan(x => x.PeriodEnd);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(256);
    }
}
