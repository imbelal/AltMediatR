using AltMediatR.DDD.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace AltMediatR.Tests
{
    public class SampleRequest : IQuery<string>, ICacheable
    {
        public required string Name { get; set; }
        public string CacheKey => $"SampleRequest:{Name}";
        public TimeSpan? AbsoluteExpirationRelativeToNow => null;
    }

    public class CachingBehaviorTests
    {
        private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
        private readonly Mock<ILogger<AltMediatR.DDD.Behaviors.CachingBehavior<SampleRequest, string>>> _loggerMock = new(MockBehavior.Loose);

        [Fact]
        public async Task Handle_ReturnsCachedResponse_WhenExistsInCache()
        {
            // Arrange
            var request = new SampleRequest { Name = "FromCache" };
            var expected = "Cached Response";
            _memoryCache.Set(request.CacheKey, expected);

            var behavior = new AltMediatR.DDD.Behaviors.CachingBehavior<SampleRequest, string>(_memoryCache, _loggerMock.Object);

            bool nextWasCalled = false;
            AltMediatR.Core.Deligates.RequestHandlerDelegate<string> next = () =>
            {
                nextWasCalled = true;
                return Task.FromResult("Should not run");
            };

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.False(nextWasCalled);
            Assert.Equal(expected, result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[CACHE] Hit")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CallsNextAndCachesResult_WhenNotInCache()
        {
            // Arrange
            var request = new SampleRequest { Name = "FreshRequest" };
            var expected = "Generated Response";

            var behavior = new AltMediatR.DDD.Behaviors.CachingBehavior<SampleRequest, string>(_memoryCache, _loggerMock.Object);

            AltMediatR.Core.Deligates.RequestHandlerDelegate<string> next = () => Task.FromResult(expected);

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.Equal(expected, result);
            Assert.True(_memoryCache.TryGetValue(request.CacheKey, out string? cached));
            Assert.Equal(expected, cached);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[CACHE] Hit")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }
    }
}
