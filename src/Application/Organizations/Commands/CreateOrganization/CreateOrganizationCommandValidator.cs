using System.Globalization;

namespace modular_mlm.Application.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandValidator
    : AbstractValidator<CreateOrganizationCommand>
{
    private static readonly HashSet<string> ReservedSlugs = new(
        ["api", "admin", "auth", "login", "register", "www", "app", "agent", "account"],
        StringComparer.OrdinalIgnoreCase
    );

    public CreateOrganizationCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain lowercase letters, numbers, and single hyphens only.")
            .Must(slug => !ReservedSlugs.Contains(slug))
            .WithMessage("This organization slug is reserved.");
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain three letters.");
        RuleFor(command => command.TimeZone)
            .NotEmpty()
            .MaximumLength(100)
            .Must(BeKnownTimeZone)
            .WithMessage("Time zone is not recognized.");
        RuleFor(command => command.Locale)
            .NotEmpty()
            .MaximumLength(20)
            .Must(BeKnownLocale)
            .WithMessage("Locale is not recognized.");
    }

    internal static bool BeKnownTimeZone(string timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    internal static bool BeKnownLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return false;

        try
        {
            _ = CultureInfo.GetCultureInfo(locale.Trim());
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
