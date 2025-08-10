using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AltMediatR.DDD.Configurations;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        private readonly CachingOptions _options;

        public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger, CachingOptions? options = null)
        {
            _cache = cache;
            _logger = logger;
            _options = options ?? new CachingOptions();
        }

        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request is not IQuery<TResponse>)
            {
                return await next().ConfigureAwait(false);
            }

            if (request is not ICacheable cacheable)
            {
                return await next().ConfigureAwait(false);
            }

            var cacheKey = string.IsNullOrEmpty(_options.KeyPrefix)
                ? cacheable.CacheKey
                : _options.KeyPrefix + cacheable.CacheKey;

            if (_cache.TryGetValue(cacheKey, out var obj) && obj is TResponse cached)
            {
                _logger.LogInformation("[CACHE] Hit for {CacheKey}", cacheKey);
                return cached;
            }

            var response = await next().ConfigureAwait(false);

            var options = new MemoryCacheEntryOptions();
            var ttl = cacheable.AbsoluteExpirationRelativeToNow ?? _options.DefaultTtl;
            options.SetAbsoluteExpiration(ttl);

            _cache.Set(cacheKey, response!, options);
            return response;
        }
    }
}
