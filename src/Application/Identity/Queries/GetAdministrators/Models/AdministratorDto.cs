namespace modular_mlm.Application.Identity.Queries.GetAdministrators.Models;

public sealed record AdministratorDto(
    string Id,
    Guid OrganizationId,
    string DisplayName,
    string Email,
    bool EmailConfirmed,
    string Role
);
