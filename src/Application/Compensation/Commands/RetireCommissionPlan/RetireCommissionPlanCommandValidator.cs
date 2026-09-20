namespace modular_mlm.Application.Compensation.Commands.RetireCommissionPlan;

public sealed class RetireCommissionPlanCommandValidator
    : AbstractValidator<RetireCommissionPlanCommand>
{
    public RetireCommissionPlanCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.CommissionPlanId).NotEmpty();
        RuleFor(x => x.EffectiveTo)
            .Must(value => value.Offset == TimeSpan.Zero)
            .WithMessage("EffectiveTo must use UTC.");
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1_000)
            .Must(value => value.All(character => !char.IsControl(character)));
    }
}
