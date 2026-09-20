using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage;
using modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage.Models;
using modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages;
using modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Contracts.Messaging;

namespace modular_mlm.Web.Endpoints;

public sealed class MessagingOperations : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/messaging";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetDeadLetterPage, "dead-letters");
        group
            .MapPost(ReplayDeadLetter, "dead-letters/{messageId:guid}/replay")
            .RequireRateLimiting("financial");
    }

    public static async Task<Ok<DeadLetterMessagesPageDto>> GetDeadLetterPage(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? messageType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetDeadLetterMessagesQuery(
                    organizationId,
                    page,
                    pageSize,
                    messageType,
                    from,
                    to
                )
            )
        );

    public static async Task<Ok<ReplayDeadLetterMessageResult>> ReplayDeadLetter(
        ISender sender,
        Guid organizationId,
        Guid messageId,
        ReplayDeadLetterMessageRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new ReplayDeadLetterMessageCommand(
                    organizationId,
                    messageId,
                    request.ExpectedAttempts,
                    request.Reason
                )
            )
        );
}
