namespace modular_mlm.Application.Common.Interfaces;

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan CacheLifetime { get; }
}
