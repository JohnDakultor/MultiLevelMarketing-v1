namespace modular_mlm.Application.Organizations.Commands.UpdateBranding;

public sealed class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.StoreTitle).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SupportEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.PrimaryColor).Matches("^#[0-9A-Fa-f]{6}$");
        RuleFor(x => x.SecondaryColor).Matches("^#[0-9A-Fa-f]{6}$");
        RuleFor(x => x.AccentColor).Matches("^#[0-9A-Fa-f]{6}$");
    }
}
