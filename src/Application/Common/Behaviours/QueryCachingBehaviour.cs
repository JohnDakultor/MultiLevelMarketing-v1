using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Common.Behaviours;

public sealed class QueryCachingBehaviour<TRequest, TResponse>(ICache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is not ICacheableRequest cacheable)
            return await next();
        var cached = await cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        if (cached is not null)
            return cached;
        var response = await next();
        if (response is not null)
            await cache.SetAsync(
                cacheable.CacheKey,
                response,
                cacheable.CacheLifetime,
                cancellationToken
            );
        return response;
    }
}
