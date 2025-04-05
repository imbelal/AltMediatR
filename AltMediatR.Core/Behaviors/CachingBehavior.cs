using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
            var cacheKey = $"{typeof(TRequest).FullName}:{JsonSerializer.Serialize(request)}";

            if (_cache.TryGetValue(cacheKey, out TResponse cached))
            {
                _logger.LogInformation($"[CACHE] Hit for {cacheKey}");
                return cached;
            }

            var response = await next();
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
            return response;
        }
    }

}
