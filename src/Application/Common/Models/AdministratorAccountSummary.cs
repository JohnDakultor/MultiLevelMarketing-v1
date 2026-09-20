namespace modular_mlm.Application.Common.Models;

public sealed record AdministratorAccountSummary(
    string UserId,
    Guid OrganizationId,
    string DisplayName,
    string Email,
    bool EmailConfirmed
);
