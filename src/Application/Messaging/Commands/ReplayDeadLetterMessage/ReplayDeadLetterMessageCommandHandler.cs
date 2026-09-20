using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage.Models;

namespace modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage;

public sealed class ReplayDeadLetterMessageCommandHandler(
    IDeadLetterMessageStore store,
    IUser currentUser,
    TimeProvider clock
) : IRequestHandler<ReplayDeadLetterMessageCommand, ReplayDeadLetterMessageResult>
{
    public async Task<ReplayDeadLetterMessageResult> Handle(
        ReplayDeadLetterMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        if (currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var result = await store.ReplayAsync(
            new DeadLetterReplayRequest(
                request.OrganizationId,
                request.MessageId,
                request.ExpectedAttempts,
                currentUser.UserId.ToString("D"),
                request.Reason.Trim(),
                clock.GetUtcNow()
            ),
            cancellationToken
        );

        return new ReplayDeadLetterMessageResult(
            result.MessageId,
            result.PreviousAttempts,
            result.ReplayedAt,
            result.NextAttemptAt
        );
    }
}
