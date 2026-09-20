namespace modular_mlm.Application.Common.Interfaces;

public interface IAuditWriter
{
    void Write(
        Guid organizationId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? reason
    );

    void WriteAsActor(
        Guid organizationId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? reason
    );
}
