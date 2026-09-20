using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.AuditLogs;

namespace modular_mlm.Infrastructure.Auditing;

public sealed class AuditWriter(
    IApplicationDbContext db,
    IAuditContextAccessor contextAccessor,
    IUser currentUser,
    IAuditPayloadRedactor redactor
) : IAuditWriter
{
    private static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");

    public void Write(
        Guid organizationId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? reason
    )
    {
        var context = contextAccessor.Current;
        var trustedActorId = context.ActorUserId ?? currentUser.Id;
        var actorId = Guid.TryParse(trustedActorId, out var parsedActorId)
            ? parsedActorId
            : SystemActorId;
        WriteAsActor(
            organizationId,
            actorId,
            action,
            entityType,
            entityId,
            beforeJson,
            afterJson,
            reason
        );
    }

    public void WriteAsActor(
        Guid organizationId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? reason
    )
    {
        var context = contextAccessor.Current;
        db.AuditLogs.Add(
            AuditLog.Record(
                organizationId,
                actorUserId,
                action,
                entityType,
                entityId,
                redactor.RedactJson(beforeJson),
                redactor.RedactJson(afterJson),
                redactor.RedactText(reason),
                context.IpAddress,
                context.UserAgent,
                context.TraceId
            )
        );
    }
}
