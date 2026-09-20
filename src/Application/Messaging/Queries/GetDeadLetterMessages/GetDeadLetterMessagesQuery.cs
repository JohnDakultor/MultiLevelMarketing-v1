using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages;

[Authorize(Roles = Roles.Administrator)]
public sealed record GetDeadLetterMessagesQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    string? MessageType = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
) : IRequest<DeadLetterMessagesPageDto>, IOrganizationAdminRequest;
