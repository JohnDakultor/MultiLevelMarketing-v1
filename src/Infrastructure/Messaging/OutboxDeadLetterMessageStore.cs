using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxDeadLetterMessageStore(
    ApplicationDbContext db,
    OutboxConsumerRegistry registry,
    IAuditWriter auditWriter
) : IDeadLetterMessageStore
{
    public async Task<DeadLetterMessagePage> GetAsync(
        DeadLetterMessageQuery query,
        CancellationToken cancellationToken
    )
    {
        var messages = db
            .OutboxMessages.AsNoTracking()
            .Where(message =>
                message.OrganizationId == query.OrganizationId && message.DeadLetteredAt != null
            );
        if (!string.IsNullOrWhiteSpace(query.MessageType))
            messages = messages.Where(message => message.MessageType == query.MessageType);
        if (query.From.HasValue)
            messages = messages.Where(message => message.DeadLetteredAt >= query.From);
        if (query.To.HasValue)
            messages = messages.Where(message => message.DeadLetteredAt <= query.To);

        var total = await messages.CountAsync(cancellationToken);
        var records = await messages
            .OrderByDescending(message => message.DeadLetteredAt)
            .ThenByDescending(message => message.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(message => new DeadLetterMessageRecord(
                message.Id,
                message.MessageType,
                message.OccurredAt,
                message.Attempts,
                message.DeadLetteredAt!.Value,
                message.NextAttemptAt,
                message.CorrelationId,
                message.LastError == null ? null : "Processing failed."
            ))
            .ToListAsync(cancellationToken);
        return new DeadLetterMessagePage(records, query.Page, query.PageSize, total);
    }

    public async Task<DeadLetterReplayResult> ReplayAsync(
        DeadLetterReplayRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Guid.TryParse(request.ActorUserId, out var actorId))
            throw new UnauthorizedAccessException();

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() =>
            ReplayWithinTransactionAsync(request, actorId, cancellationToken)
        );
    }

    private async Task<DeadLetterReplayResult> ReplayWithinTransactionAsync(
        DeadLetterReplayRequest request,
        Guid actorId,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var message = await db
            .OutboxMessages.FromSqlInterpolated(
                $"SELECT * FROM \"OutboxMessages\" WHERE \"Id\" = {request.MessageId} AND \"OrganizationId\" = {request.OrganizationId} FOR UPDATE"
            )
            .SingleOrDefaultAsync(cancellationToken);
        if (message is null)
            throw new KeyNotFoundException("Dead-letter message was not found.");
        if (!registry.IsAllowed(message.MessageType))
            throw new InvalidOperationException("The message type is not approved for replay.");
        if (message.DeadLetteredAt is null || message.Attempts != request.ExpectedAttempts)
            throw new ConflictException("The dead-letter message changed before replay.");

        var previousAttempts = message.Attempts;
        var before = AuditJson.Serialize(
            new
            {
                message.Id,
                message.MessageType,
                message.Attempts,
                message.DeadLetteredAt,
                message.NextAttemptAt,
            }
        );
        message.Replay(request.ReplayedAt);
        var audit = AuditCoverageMap.OutboxMessageReplayed;
        auditWriter.WriteAsActor(
            request.OrganizationId,
            actorId,
            audit.Action,
            audit.EntityType,
            message.Id,
            before,
            AuditJson.Serialize(
                new
                {
                    message.Id,
                    message.MessageType,
                    message.Attempts,
                    message.DeadLetteredAt,
                    message.NextAttemptAt,
                }
            ),
            request.Reason
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DeadLetterReplayResult(
            message.Id,
            previousAttempts,
            request.ReplayedAt,
            message.NextAttemptAt
        );
    }
}
