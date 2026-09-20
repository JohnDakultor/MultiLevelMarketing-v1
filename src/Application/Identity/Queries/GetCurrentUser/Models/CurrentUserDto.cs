namespace modular_mlm.Application.Identity.Queries.GetCurrentUser.Models;

public sealed record CurrentUserDto(
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    Guid? OrganizationId,
    Guid? CustomerId,
    Guid? AgentId,
    bool EmailVerified,
    bool MfaEnabled
);
