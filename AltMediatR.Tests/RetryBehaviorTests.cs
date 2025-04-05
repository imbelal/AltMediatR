using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.Logging;

namespace AltMediatR.Tests
{
    public class SampleRetryRequest : IRequest<string>
    {
        public string Payload { get; set; }
    }

    public class RetryBehaviorTests
    {
        private readonly ILogger<RetryBehavior<SampleRetryRequest, string>> _logger;

        public RetryBehaviorTests()
        {
            // Use a simple console logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                });
            });

            _logger = loggerFactory.CreateLogger<RetryBehavior<SampleRetryRequest, string>>();
        }

        [Fact]
        public async Task Handle_ReturnsResponse_WhenNoException()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "Hello" };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_logger);

            RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

            // Act
            var result = await behavior.HandleAsync(request, CancellationToken.None, next);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task Handle_RetriesOnFailure_AndEventuallySucceeds()
        {
            // Arrange
            var request = new SampleRetryRequest { Payload = "RetryMe" };
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_logger);

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
        }

        [Fact]
        public async Task Handle_FailsAfterMaxRetries()
        {
            // Arrange
            var request = new SampleRetryRequest();
            var behavior = new RetryBehavior<SampleRetryRequest, string>(_logger);

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
        }
    }
}
