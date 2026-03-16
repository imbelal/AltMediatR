using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Configurations;
using AltMediatR.Core.Delegates;
using Microsoft.Extensions.Logging;
using Moq;

namespace AltMediatR.Tests
{
    public class SampleRetryRequest : IRequest<string>
    {
        public required string Payload { get; set; }
    }

    public class RetryBehaviorTests
    {
        private readonly Mock<ILogger<RetryBehavior<SampleRetryRequest, string>>> _loggerMock = new(MockBehavior.Loose);

        [Fact]
        public async Task Handle_ReturnsResponse_WhenNoException()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "Hello" };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object);

            RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.Equal("Success", result);
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_RetriesOnFailure_AndEventuallySucceeds()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "RetryMe" };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object, new RetryOptions { MaxAttempts = 3, BaseDelayMs = 0 });

            int attempts = 0;

            RequestHandlerDelegate<string> next = () =>
            {
                attempts++;
                if (attempts < 3)
                    throw new InvalidOperationException("Simulated failure");
                return Task.FromResult("Recovered");
            };

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.Equal("Recovered", result);
            Assert.Equal(3, attempts);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempt 1 failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempt 2 failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FailsAfterMaxRetries()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "x" };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object, new RetryOptions { MaxAttempts = 3, BaseDelayMs = 0 });

            int attempts = 0;

            RequestHandlerDelegate<string> next = () =>
            {
                attempts++;
                throw new Exception("Always fails");
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                behavior.HandleAsync(request, CancellationToken.None, next));

            Assert.Equal("Always fails", ex.Message);
            Assert.Equal(3, attempts); // 3 retries
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempt 1 failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempt 2 failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RespectsCustomMaxAttempts()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "x" };
            var options = new RetryOptions { MaxAttempts = 5, BaseDelayMs = 0 };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object, options);

            int attempts = 0;

            RequestHandlerDelegate<string> next = () =>
            {
                attempts++;
                throw new Exception("Always fails");
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                behavior.HandleAsync(request, CancellationToken.None, next));

            Assert.Equal(5, attempts);
        }

        [Fact]
        public async Task Handle_SucceedsOnFinalAttempt_WithCustomOptions()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "x" };
            var options = new RetryOptions { MaxAttempts = 2, BaseDelayMs = 0 };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object, options);

            int attempts = 0;

            RequestHandlerDelegate<string> next = () =>
            {
                attempts++;
                if (attempts < 2)
                    throw new InvalidOperationException("First attempt fails");
                return Task.FromResult("Success on attempt 2");
            };

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.Equal("Success on attempt 2", result);
            Assert.Equal(2, attempts);
        }
    }
}
