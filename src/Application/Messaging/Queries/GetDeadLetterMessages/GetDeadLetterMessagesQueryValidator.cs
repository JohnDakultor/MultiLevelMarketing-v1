using modular_mlm.Domain.Common;

namespace modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages;

public sealed class GetDeadLetterMessagesQueryValidator
    : AbstractValidator<GetDeadLetterMessagesQuery>
{
    private static readonly HashSet<string> AllowedMessageTypes = typeof(BaseEvent)
        .Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(BaseEvent).IsAssignableFrom(type))
        .Select(type => type.FullName ?? type.Name)
        .ToHashSet(StringComparer.Ordinal);

    public GetDeadLetterMessagesQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.MessageType)
            .MaximumLength(500)
            .Must(IsAllowedMessageType)
            .WithMessage("Message type is not supported.");
        RuleFor(query => query)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From <= query.To)
            .WithMessage("From must be earlier than or equal to To.");
    }

    private static bool IsAllowedMessageType(string? messageType) =>
        string.IsNullOrWhiteSpace(messageType) || AllowedMessageTypes.Contains(messageType.Trim());
}
