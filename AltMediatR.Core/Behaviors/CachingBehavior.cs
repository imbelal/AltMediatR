using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AltMediatR.Core.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            // Only cache queries
            if (request is not IQuery<TResponse>)
            {
                return await next().ConfigureAwait(false);
            }

            // Require a stable cache key provider; if not provided, bypass caching to avoid bad keys
            if (request is not ICacheable cacheable)
            {
                return await next().ConfigureAwait(false);
            }

            var cacheKey = cacheable.CacheKey;
            if (_cache.TryGetValue(cacheKey, out var obj) && obj is TResponse cached)
            {
                _logger.LogInformation("[CACHE] Hit for {CacheKey}", cacheKey);
                return cached;
            }

            var response = await next().ConfigureAwait(false);

            var options = new MemoryCacheEntryOptions();
            if (cacheable.AbsoluteExpirationRelativeToNow.HasValue)
            {
                options.SetAbsoluteExpiration(cacheable.AbsoluteExpirationRelativeToNow.Value);
            }
            else
            {
                // Sensible default if TTL not provided
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            }

            _cache.Set(cacheKey, response!, options);
            return response;
        }
    }

}
