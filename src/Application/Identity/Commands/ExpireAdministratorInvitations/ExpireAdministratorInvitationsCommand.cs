using modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations.Models;

namespace modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations;

public sealed record ExpireAdministratorInvitationsCommand(
    Guid OrganizationId,
    int BatchSize,
    DateTimeOffset AsOf
) : IRequest<ExpireAdministratorInvitationsResult>;
