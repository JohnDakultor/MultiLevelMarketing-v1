namespace modular_mlm.Application.Organizations.Commands.UpdateReferralSettings;

public sealed class UpdateReferralSettingsCommandValidator
    : AbstractValidator<UpdateReferralSettingsCommand>
{
    public UpdateReferralSettingsCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();

        RuleFor(x => x.AttributionWindowDays).GreaterThan(0);
    }
}
