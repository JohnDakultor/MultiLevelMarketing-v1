using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using modular_mlm.Domain.AuditLogs;

namespace modular_mlm.Infrastructure.Data.Interceptors;

public sealed class AuditLogImmutabilityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        EnsureImmutable(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        EnsureImmutable(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void EnsureImmutable(DbContext? context)
    {
        if (
            context
                ?.ChangeTracker.Entries<AuditLog>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted) == true
        )
            throw new InvalidOperationException("Audit records are immutable.");
    }
}
