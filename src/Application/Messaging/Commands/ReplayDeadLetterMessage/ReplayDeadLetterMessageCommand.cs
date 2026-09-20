using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage;

[Authorize(Roles = Roles.Administrator)]
public sealed record ReplayDeadLetterMessageCommand(
    Guid OrganizationId,
    Guid MessageId,
    int ExpectedAttempts,
    string Reason
) : IRequest<ReplayDeadLetterMessageResult>, IOrganizationAdminRequest;
