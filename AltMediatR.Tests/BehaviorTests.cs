using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AltMediatR.Tests
{
    public class BehaviorTests
    {
        private readonly IMediator _mediator;
        private readonly Mock<ILogger<LoggingBehavior<TestCommand, string>>> _loggerMock;

        // Minimal test command to avoid dependency on samples
        public record TestCommand(string Name) : IRequest<string>;

        public BehaviorTests()
        {
            var services = new ServiceCollection();
            // Mock handler to avoid side effects and assert only behavior
            var handlerMock = new Mock<IRequestHandler<TestCommand, string>>();
            handlerMock
                .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("new-id");

            // Mock logger for LoggingBehavior<TestCommand,string>
            _loggerMock = new Mock<ILogger<LoggingBehavior<TestCommand, string>>>(MockBehavior.Loose);

            services.AddAltMediator(s => s.AddLoggingBehavior());
            services.AddSingleton(handlerMock.Object);
            services.AddSingleton(_loggerMock.Object);

            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Should_Log_And_Handle_Command()
        {
            var result = await _mediator.SendAsync(new TestCommand("Bob"));
            Assert.False(string.IsNullOrWhiteSpace(result));

            // Verify logging happened before and after
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling TestCommand")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled TestCommand")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
