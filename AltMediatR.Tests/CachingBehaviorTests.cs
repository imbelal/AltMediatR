using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace AltMediatR.Tests
{
    public class SampleRequest : IRequest<string>
    {
        public string Name { get; set; }
    }

    public class CachingBehaviorTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CachingBehavior<SampleRequest, string>> _logger;

        public CachingBehaviorTests()
        {
            _logger = NullLogger<CachingBehavior<SampleRequest, string>>.Instance;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task Handle_ReturnsCachedResponse_WhenExistsInCache()
        {
            // Arrange
            var request = new SampleRequest { Name = "FromCache" };
            var expected = "Cached Response";

            var cacheKey = $"{typeof(SampleRequest).FullName}:{JsonSerializer.Serialize(request)}";
            _memoryCache.Set(cacheKey, expected);

            var behavior = new CachingBehavior<SampleRequest, string>(_memoryCache, _logger);

            bool nextWasCalled = false;

            // This delegate should not be called
            RequestHandlerDelegate<string> next = () =>
            {
                nextWasCalled = true;
                return Task.FromResult("Should not run");
            };

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.False(nextWasCalled);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Handle_CallsNextAndCachesResult_WhenNotInCache()
        {
            // Arrange
            var request = new SampleRequest { Name = "FreshRequest" };
            var expected = "Generated Response";

            var behavior = new CachingBehavior<SampleRequest, string>(_memoryCache, _logger);

            RequestHandlerDelegate<string> next = () =>
            {
                return Task.FromResult(expected);
            };

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.Equal(expected, result);

            var cacheKey = $"{typeof(SampleRequest).FullName}:{JsonSerializer.Serialize(request)}";
            var success = _memoryCache.TryGetValue(cacheKey, out string cached);
            Assert.True(success);
            Assert.Equal(expected, cached);
        }
    }
}
