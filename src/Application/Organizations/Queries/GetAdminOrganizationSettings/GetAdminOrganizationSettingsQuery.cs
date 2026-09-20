using modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings;

public sealed record GetAdminOrganizationSettingsQuery(Guid OrganizationId)
    : IRequest<AdminOrganizationSettingsDto?>,
        IOrganizationAdminRequest;
