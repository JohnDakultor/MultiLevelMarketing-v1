namespace modular_mlm.Domain.Events;

public sealed record AdministratorInvitationAcceptedEvent(
    Guid OrganizationId,
    Guid InvitationId,
    Guid AcceptedByUserId
) : BaseEvent;
