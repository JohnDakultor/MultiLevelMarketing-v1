using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.CreateCommissionPlan;

public sealed class CreateCommissionPlanCommandValidator
    : AbstractValidator<CreateCommissionPlanCommand>
{
    public CreateCommissionPlanCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Version).GreaterThan(0);
        RuleFor(command => command.DirectSalesRate).InclusiveBetween(0m, 1m);
        RuleFor(command => command.BinaryPairingRate)
            .NotNull()
            .InclusiveBetween(0.000001m, 1m)
            .When(command =>
                command.BinaryPairingEnabled
                && command.PairingCalculationType == PairingCalculationType.PercentageMatchedVolume
            );
        RuleFor(command => command.BinaryPairingRate)
            .Null()
            .When(command =>
                !command.BinaryPairingEnabled
                || command.PairingCalculationType == PairingCalculationType.FixedPerPair
            );
        RuleFor(command => command.PairUnitBv)
            .NotNull()
            .GreaterThan(0m)
            .When(command =>
                command.BinaryPairingEnabled
                && command.PairingCalculationType == PairingCalculationType.FixedPerPair
            );
        RuleFor(command => command.FixedPairAmount)
            .NotNull()
            .GreaterThan(0m)
            .When(command =>
                command.BinaryPairingEnabled
                && command.PairingCalculationType == PairingCalculationType.FixedPerPair
            );
        RuleFor(command => command.PairUnitBv)
            .Null()
            .When(command =>
                !command.BinaryPairingEnabled
                || command.PairingCalculationType == PairingCalculationType.PercentageMatchedVolume
            );
        RuleFor(command => command.FixedPairAmount)
            .Null()
            .When(command =>
                !command.BinaryPairingEnabled
                || command.PairingCalculationType == PairingCalculationType.PercentageMatchedVolume
            );
    }
}
