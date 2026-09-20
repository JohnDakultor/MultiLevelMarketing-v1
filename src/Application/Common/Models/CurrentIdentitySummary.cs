namespace modular_mlm.Application.Common.Models;

public sealed record CurrentIdentitySummary(
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    Guid? OrganizationId,
    bool EmailVerified,
    bool MfaEnabled
);
