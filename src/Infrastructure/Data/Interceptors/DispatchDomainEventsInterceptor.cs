using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using modular_mlm.Domain.Common;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Infrastructure.Data.Interceptors;

public sealed class DispatchDomainEventsInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is not null)
        {
            var entities = eventData
                .Context.ChangeTracker.Entries<BaseEntity>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Count != 0)
                .ToList();
            var events = entities.SelectMany(x => x.DomainEvents).ToList();
            entities.ForEach(x => x.ClearDomainEvents());
            if (eventData.Context is ApplicationDbContext db)
                foreach (var domainEvent in events)
                    db.OutboxMessages.Add(
                        OutboxMessage.Create(
                            domainEvent,
                            clock.GetUtcNow(),
                            Activity.Current?.TraceId.ToString()
                        )
                    );
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
