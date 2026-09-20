namespace modular_mlm.Web.Contracts.Organizations;

public sealed record CreateOrganizationRequest(
    string Name,
    string Slug,
    string CurrencyCode,
    string TimeZone = "UTC",
    string Locale = "en"
);
