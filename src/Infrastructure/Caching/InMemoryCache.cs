using System.Collections.Concurrent;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Caching;

public sealed class InMemoryCache(TimeProvider clock) : ICache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _values = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_values.TryGetValue(key, out var entry))
            return Task.FromResult(default(T));
        if (entry.ExpiresAt <= clock.GetUtcNow())
        {
            _values.TryRemove(key, out _);
            return Task.FromResult(default(T));
        }
        return Task.FromResult(entry.Value is T typed ? typed : default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan lifetime,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(key) || lifetime <= TimeSpan.Zero)
            throw new ArgumentException("A cache key and positive lifetime are required.");
        if (value is not null)
            _values[key] = new CacheEntry(value, clock.GetUtcNow().Add(lifetime));
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (
            var key in _values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
        )
            _values.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(object Value, DateTimeOffset ExpiresAt);
}
