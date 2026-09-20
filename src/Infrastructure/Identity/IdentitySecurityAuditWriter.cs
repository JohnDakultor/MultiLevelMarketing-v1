using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Identity;

public sealed class IdentitySecurityAuditWriter(IApplicationDbContext db, IAuditWriter auditWriter)
    : IIdentitySecurityAuditWriter
{
    public async Task WriteAsync(
        IdentitySecurityAuditEvent securityEvent,
        CancellationToken cancellationToken
    )
    {
        var definition = AuditCoverageMap.GetByAction(securityEvent.Action);
        if (definition.EntityType != AuditEntityNames.AdministratorAccount)
            throw new InvalidOperationException("The action is not an Identity security action.");
        auditWriter.WriteAsActor(
            securityEvent.OrganizationId,
            securityEvent.SubjectUserId,
            definition.Action,
            definition.EntityType,
            securityEvent.SubjectUserId,
            null,
            AuditJson.Serialize(
                new
                {
                    securityEvent.Succeeded,
                    securityEvent.FailureCode,
                    securityEvent.OccurredAt,
                }
            ),
            securityEvent.FailureCode
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
