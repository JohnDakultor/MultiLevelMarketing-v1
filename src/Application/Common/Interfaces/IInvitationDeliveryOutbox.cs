namespace modular_mlm.Application.Common.Interfaces;

public interface IInvitationDeliveryOutbox
{
    Task StageAsync(
        Guid invitationId,
        Guid organizationId,
        string recipient,
        string rawToken,
        string templateKey,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    );

    Task ActivateAsync(
        Guid organizationId,
        Guid invitationId,
        DateTimeOffset readyAt,
        CancellationToken cancellationToken
    );
}
