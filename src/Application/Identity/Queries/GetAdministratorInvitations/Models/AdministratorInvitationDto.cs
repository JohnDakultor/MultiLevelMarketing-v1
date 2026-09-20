using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Identity.Queries.GetAdministratorInvitations.Models;

public sealed record AdministratorInvitationDto(
    Guid Id,
    Guid OrganizationId,
    Guid InvitedByUserId,
    string Email,
    DateTimeOffset InvitedAt,
    DateTimeOffset ExpiresAt,
    AdministratorInvitationStatus Status,
    Guid? AcceptedByUserId,
    DateTimeOffset? AcceptedAt,
    Guid? RevokedByUserId,
    DateTimeOffset? RevokedAt,
    string? RevocationReason
);
