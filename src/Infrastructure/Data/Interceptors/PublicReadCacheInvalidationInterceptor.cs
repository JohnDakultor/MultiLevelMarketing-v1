using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Common;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.Data.Interceptors;

public sealed class PublicReadCacheInvalidationInterceptor(ICache cache) : SaveChangesInterceptor
{
    private readonly ConcurrentDictionary<Guid, CacheInvalidation> _pending = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is { } context)
        {
            var changed = context
                .ChangeTracker.Entries()
                .Where(x =>
                    x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                )
                .Select(x => x.Entity)
                .ToArray();
            var organizationIds = changed
                .OfType<OrganizationEntity>()
                .Select(x => x.OrganizationId)
                .Where(x => x != Guid.Empty)
                .ToHashSet();
            foreach (var product in changed.OfType<Product>())
                organizationIds.Add(product.OrganizationId);
            foreach (var variant in changed.OfType<ProductVariant>())
            {
                var product = context
                    .ChangeTracker.Entries<Product>()
                    .Select(x => x.Entity)
                    .FirstOrDefault(x => x.Id == variant.ProductId);
                if (product is not null)
                    organizationIds.Add(product.OrganizationId);
            }
            var organizations = changed.OfType<Organization>().Select(x => x.Slug).ToHashSet();
            if (organizationIds.Count > 0 || organizations.Count > 0)
                _pending[context.ContextId.InstanceId] = new(organizationIds, organizations);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        if (
            eventData.Context is { } context
            && _pending.TryRemove(context.ContextId.InstanceId, out var invalidation)
        )
        {
            foreach (var organizationId in invalidation.OrganizationIds)
                await cache.RemoveByPrefixAsync($"catalog:{organizationId:N}:", cancellationToken);
            foreach (var slug in invalidation.OrganizationSlugs)
                await cache.RemoveByPrefixAsync(
                    $"public-config:{slug.Trim().ToLowerInvariant()}",
                    cancellationToken
                );
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is { } context)
            _pending.TryRemove(context.ContextId.InstanceId, out _);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private sealed record CacheInvalidation(
        IReadOnlySet<Guid> OrganizationIds,
        IReadOnlySet<string> OrganizationSlugs
    );
}
