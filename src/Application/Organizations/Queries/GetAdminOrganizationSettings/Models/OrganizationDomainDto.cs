namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record OrganizationDomainDto(
    Guid Id,
    string HostName,
    bool IsPrimary,
    bool IsVerified,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VerifiedAt
);
