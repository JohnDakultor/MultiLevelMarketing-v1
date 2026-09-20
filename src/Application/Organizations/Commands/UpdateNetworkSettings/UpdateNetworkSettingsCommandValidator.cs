namespace modular_mlm.Application.Organizations.Commands.UpdateNetworkSettings;

public sealed class UpdateNetworkSettingsCommandValidator
    : AbstractValidator<UpdateNetworkSettingsCommand>
{
    public UpdateNetworkSettingsCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.DefaultPlacementStrategy).IsInEnum();
        RuleFor(x => x.MaxQueryDepth).InclusiveBetween(1, 100);
    }
}
