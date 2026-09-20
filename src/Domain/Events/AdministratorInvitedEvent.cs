namespace modular_mlm.Domain.Events;

public sealed record AdministratorInvitedEvent(
    Guid OrganizationId,
    Guid InvitationId,
    Guid InvitedByUserId
) : BaseEvent;
