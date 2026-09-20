namespace modular_mlm.Application.Organizations.Commands.UpdateWalletSettings;

public sealed class UpdateWalletSettingsCommandValidator
    : AbstractValidator<UpdateWalletSettingsCommand>
{
    private const int MaximumOperationalDays = 365;

    public UpdateWalletSettingsCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CommissionReleaseTrigger).IsInEnum();
        RuleFor(command => command.ReleaseDelayDays).InclusiveBetween(0, MaximumOperationalDays);
        RuleFor(command => command.ReturnWindowDays).InclusiveBetween(0, MaximumOperationalDays);
        RuleFor(command => command.MinimumPayoutAmount).GreaterThan(0m);
        RuleFor(command => command.MaximumNegativeBalance).GreaterThanOrEqualTo(0m);
        RuleFor(command => command.MaximumNegativeBalance)
            .Equal(0m)
            .When(command => !command.AllowNegativeRecoverableBalance)
            .WithMessage(
                "Maximum negative balance must be zero when negative balances are disabled."
            );
    }
}
