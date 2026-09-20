using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class AdministratorInvitation : OrganizationEntity
{
    private AdministratorInvitation() { }

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = null!;

    public Guid InvitedByUserId { get; private set; }
    public DateTimeOffset InvitedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public AdministratorInvitationStatus Status { get; private set; }

    public Guid? AcceptedByUserId { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }

    public static AdministratorInvitation Create(
        Guid organizationId,
        string email,
        string normalizedEmail,
        string tokenHash,
        Guid invitedByUserId,
        DateTimeOffset invitedAt,
        DateTimeOffset expiresAt
    )
    {
        if (organizationId == Guid.Empty)
            throw new DomainInvariantException("Organization is required.");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(normalizedEmail))
            throw new DomainInvariantException("Email and normalized email are required.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainInvariantException("Token hash is required.");
        if (invitedByUserId == Guid.Empty)
            throw new DomainInvariantException("Inviting administrator is required.");
        if (expiresAt <= invitedAt)
            throw new DomainInvariantException(
                "Invitation expiration must follow invitation time."
            );

        var invitation = new AdministratorInvitation
        {
            OrganizationId = organizationId,
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail.Trim(),
            TokenHash = tokenHash,
            InvitedByUserId = invitedByUserId,
            InvitedAt = invitedAt,
            ExpiresAt = expiresAt,
            Status = AdministratorInvitationStatus.Pending,
        };
        invitation.AddDomainEvent(
            new AdministratorInvitedEvent(
                invitation.OrganizationId,
                invitation.Id,
                invitation.InvitedByUserId
            )
        );
        return invitation;
    }

    public bool CanBeAccepted(DateTimeOffset currentTime)
    {
        return Status == AdministratorInvitationStatus.Pending && ExpiresAt > currentTime;
    }

    public void Accept(Guid acceptedByUserId, DateTimeOffset acceptedAt)
    {
        if (Status != AdministratorInvitationStatus.Pending)
            throw new DomainInvariantException("Only pending invitations can be accepted.");

        if (acceptedAt >= ExpiresAt)
            throw new DomainInvariantException("Accepted at must be before expiration date.");

        if (acceptedByUserId == Guid.Empty)
            throw new DomainInvariantException("Accepting administrator is required.");

        Status = AdministratorInvitationStatus.Accepted;
        AcceptedByUserId = acceptedByUserId;
        AcceptedAt = acceptedAt;

        InvalidateToken();
        AddDomainEvent(
            new AdministratorInvitationAcceptedEvent(OrganizationId, Id, acceptedByUserId)
        );
    }

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt, string revocationReason)
    {
        if (Status != AdministratorInvitationStatus.Pending)
            throw new DomainInvariantException("Only pending invitations can be revoked.");

        if (revokedByUserId == Guid.Empty)
            throw new DomainInvariantException("Revoking administrator is required.");

        if (string.IsNullOrWhiteSpace(revocationReason))
            throw new DomainInvariantException("Revocation reason is required.");

        Status = AdministratorInvitationStatus.Revoked;
        RevokedByUserId = revokedByUserId;
        RevokedAt = revokedAt;
        RevocationReason = revocationReason.Trim();

        InvalidateToken();
    }

    public void MarkExpired(DateTimeOffset currentTime)
    {
        if (Status != AdministratorInvitationStatus.Pending)
            throw new DomainInvariantException(
                "Only pending invitations can be marked as expired."
            );

        if (currentTime < ExpiresAt)
            throw new DomainInvariantException("Current time must be on or after expiration date.");

        Status = AdministratorInvitationStatus.Expired;

        InvalidateToken();
    }

    public void Renew(string tokenHash, DateTimeOffset invitedAt, DateTimeOffset expiresAt)
    {
        if (Status != AdministratorInvitationStatus.Pending)
            throw new DomainInvariantException("Only a pending invitation can be renewed.");
        if (string.IsNullOrWhiteSpace(tokenHash) || expiresAt <= invitedAt)
            throw new DomainInvariantException("Renewed invitation values are invalid.");
        TokenHash = tokenHash;
        InvitedAt = invitedAt;
        ExpiresAt = expiresAt;
        AddDomainEvent(new AdministratorInvitedEvent(OrganizationId, Id, InvitedByUserId));
    }

    private void InvalidateToken() => TokenHash = $"invalidated:{Guid.NewGuid():N}";
}
