using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages.Models;

namespace modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages;

public sealed class GetDeadLetterMessagesQueryHandler(IDeadLetterMessageStore store)
    : IRequestHandler<GetDeadLetterMessagesQuery, DeadLetterMessagesPageDto>
{
    public async Task<DeadLetterMessagesPageDto> Handle(
        GetDeadLetterMessagesQuery request,
        CancellationToken cancellationToken
    )
    {
        var page = await store.GetAsync(
            new DeadLetterMessageQuery(
                request.OrganizationId,
                request.Page,
                request.PageSize,
                Normalize(request.MessageType),
                request.From,
                request.To
            ),
            cancellationToken
        );

        return new DeadLetterMessagesPageDto(
            page.Items.Select(Map).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount
        );
    }

    private static DeadLetterMessageDto Map(DeadLetterMessageRecord message) =>
        new(
            message.MessageId,
            message.MessageType,
            message.OccurredAt,
            message.Attempts,
            message.DeadLetteredAt,
            message.NextAttemptAt,
            message.CorrelationId,
            message.LastErrorSummary
        );

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
