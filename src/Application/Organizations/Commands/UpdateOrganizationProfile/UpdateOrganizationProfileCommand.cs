namespace modular_mlm.Application.Organizations.Commands.UpdateOrganizationProfile;

public sealed record UpdateOrganizationProfileCommand(
    Guid OrganizationId,
    string Name,
    string CurrencyCode,
    string TimeZone,
    string Locale
) : IRequest, IOrganizationAdminRequest;
