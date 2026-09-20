namespace modular_mlm.Web.Contracts.Organizations;

public sealed record UpdateOrganizationProfileRequest(
    string Name,
    string CurrencyCode,
    string TimeZone,
    string Locale
);
