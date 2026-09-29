using modular_mlm.Application.Compensation;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.UpdateCommissionPlan;

public sealed class UpdateCommissionPlanCommandValidator
    : AbstractValidator<UpdateCommissionPlanCommand>
{
    public UpdateCommissionPlanCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CommissionPlanId).NotEmpty();
        RuleFor(command => command.ExpectedConfigurationVersion).GreaterThanOrEqualTo(0);

        RuleFor(command => command.DirectSalesRate)
            .InclusiveBetween(0m, 1m)
            .When(command => command.DirectSalesEnabled);
        RuleFor(command => command.DirectSalesRate)
            .Equal(0m)
            .When(command => !command.DirectSalesEnabled)
            .WithMessage("DirectSalesRate must be zero when direct sales are disabled.");

        RuleFor(command => command.PairingCalculationType)
            .IsInEnum()
            .When(command => command.BinaryPairingEnabled);
        RuleFor(command => command.ProcessingFrequency)
            .IsInEnum()
            .When(command => command.BinaryPairingEnabled);
        RuleFor(command => command.BinaryPairingRate)
            .NotNull()
            .InclusiveBetween(0.000001m, 1m)
            .When(IsPercentage);
        RuleFor(command => command.PairUnitBv)
            .NotNull()
            .GreaterThan(0m)
            .When(IsFixed);
        RuleFor(command => command.FixedPairAmount)
            .NotNull()
            .GreaterThan(0m)
            .When(IsFixed);

        RuleFor(command => command.BinaryPairingRate).Null().When(command => !IsPercentage(command));
        RuleFor(command => command.PairUnitBv).Null().When(command => !IsFixed(command));
        RuleFor(command => command.FixedPairAmount).Null().When(command => !IsFixed(command));

        RuleFor(command => command.QualificationRulesJson)
            .NotEmpty()
            .MaximumLength(CommissionRuleJson.MaximumLength)
            .Must(CommissionRuleJson.IsValidQualificationRules)
            .WithMessage("Qualification rules must match the supported rule structure.");
        RuleFor(command => command.CapRulesJson)
            .NotEmpty()
            .MaximumLength(CommissionRuleJson.MaximumLength)
            .Must(CommissionRuleJson.IsValidCapRules)
            .WithMessage("Cap rules must contain a non-negative maximum amount when enabled.");
    }

    private static bool IsPercentage(UpdateCommissionPlanCommand command) =>
        command.BinaryPairingEnabled
        && command.PairingCalculationType == PairingCalculationType.PercentageMatchedVolume;

    private static bool IsFixed(UpdateCommissionPlanCommand command) =>
        command.BinaryPairingEnabled
        && command.PairingCalculationType == PairingCalculationType.FixedPerPair;

}
