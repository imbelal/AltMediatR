using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Deligates;
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
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object);

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
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_loggerMock.Object);

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
    }
}
