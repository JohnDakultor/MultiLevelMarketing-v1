namespace modular_mlm.Application.Organizations.Commands.UpdateFeatureSettings;

public sealed class UpdateFeatureSettingsCommandValidator
    : AbstractValidator<UpdateFeatureSettingsCommand>
{
    public UpdateFeatureSettingsCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x)
            .Must(x => !x.BinaryPairingEnabled || x.BinaryNetworkEnabled)
            .WithMessage("Binary pairing requires the binary network.");
        RuleFor(x => x)
            .Must(x => !x.PayoutEnabled || x.WalletEnabled)
            .WithMessage("Payouts require the wallet.");
    }
}
