using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record OrganizationProfileDto(
    Guid Id,
    string Name,
    string Slug,
    OrganizationStatus Status,
    string CurrencyCode,
    string TimeZone,
    string Locale
);
