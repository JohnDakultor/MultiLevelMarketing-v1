using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Common;

namespace modular_mlm.Infrastructure.Data.Interceptors;

public sealed class AuditableEntityInterceptor(IUser user, TimeProvider clock)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null)
            return;
        foreach (
            var entry in context
                .ChangeTracker.Entries<BaseAuditableEntity>()
                .Where(x => x.State is EntityState.Added or EntityState.Modified)
        )
        {
            var now = clock.GetUtcNow();
            if (entry.State == EntityState.Added)
                entry.Entity.SetCreationAudit(now, user.Id);
            else
                entry.Entity.SetModificationAudit(now, user.Id);
        }
    }
}
