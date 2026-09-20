using modular_mlm.Application.Organizations.Commands.CreateOrganization;

namespace modular_mlm.Application.Organizations.Commands.UpdateOrganizationProfile;

public sealed class UpdateOrganizationProfileCommandValidator
    : AbstractValidator<UpdateOrganizationProfileCommand>
{
    public UpdateOrganizationProfileCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain three letters.");
        RuleFor(command => command.TimeZone)
            .NotEmpty()
            .MaximumLength(100)
            .Must(CreateOrganizationCommandValidator.BeKnownTimeZone)
            .WithMessage("Time zone is not recognized.");
        RuleFor(command => command.Locale)
            .NotEmpty()
            .MaximumLength(20)
            .Must(CreateOrganizationCommandValidator.BeKnownLocale)
            .WithMessage("Locale is not recognized.");
    }
}
