using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Infrastructure.Data.Interceptors;

public sealed class AdministratorSecurityAuditInterceptor(
    IUser currentUser,
    IAuditContextAccessor auditContextAccessor
) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        AddSecurityAudits(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        AddSecurityAudits(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddSecurityAudits(DbContext? db)
    {
        if (db is null || !currentUser.IsAdmin)
            return;

        var context = auditContextAccessor.Current;
        foreach (
            var userEntry in db
                .ChangeTracker.Entries<ApplicationUser>()
                .Where(entry => entry.State == EntityState.Modified)
                .ToList()
        )
        {
            if (
                userEntry.Entity.OrganizationId is not { } organizationId
                || !Guid.TryParse(userEntry.Entity.Id, out var subjectUserId)
            )
                continue;

            if (IsChanged(userEntry, nameof(ApplicationUser.PasswordHash)))
                AddAudit(
                    db,
                    context,
                    organizationId,
                    subjectUserId,
                    AuditCoverageMap.AdministratorPasswordChanged,
                    "Password credential changed."
                );
            if (IsChanged(userEntry, nameof(ApplicationUser.TwoFactorEnabled)))
                AddAudit(
                    db,
                    context,
                    organizationId,
                    subjectUserId,
                    AuditCoverageMap.AdministratorTwoFactorChanged,
                    "Two-factor authentication setting changed."
                );
        }
    }

    private void AddAudit(
        DbContext db,
        AuditContext context,
        Guid organizationId,
        Guid subjectUserId,
        AuditCoverageDefinition definition,
        string reason
    )
    {
        var actorId = Guid.TryParse(context.ActorUserId ?? currentUser.Id, out var parsed)
            ? parsed
            : subjectUserId;
        db.Set<AuditLog>()
            .Add(
                AuditLog.Record(
                    organizationId,
                    actorId,
                    definition.Action,
                    definition.EntityType,
                    subjectUserId,
                    "{\"Changed\":false}",
                    "{\"Changed\":true}",
                    reason,
                    context.IpAddress,
                    context.UserAgent,
                    context.TraceId
                )
            );
    }

    private static bool IsChanged(EntityEntry<ApplicationUser> entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        return property.IsModified && !Equals(property.OriginalValue, property.CurrentValue);
    }
}
