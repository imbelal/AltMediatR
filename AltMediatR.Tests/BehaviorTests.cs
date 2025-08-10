using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Extensions;
using AltMediatR.Samples.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AltMediatR.Tests
{
    public class BehaviorTests
    {
        private readonly IMediator _mediator;
        private readonly Mock<ILogger<LoggingBehavior<CreateUserCommand, string>>> _loggerMock;

        public BehaviorTests()
        {
            var services = new ServiceCollection();
            // Mock handler to avoid side effects and assert only behavior
            var handlerMock = new Mock<IRequestHandler<CreateUserCommand, string>>();
            handlerMock
                .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("new-id");

            // Mock logger for LoggingBehavior<CreateUserCommand,string>
            _loggerMock = new Mock<ILogger<LoggingBehavior<CreateUserCommand, string>>>(MockBehavior.Loose);

            services.AddAltMediator(s => s.AddLoggingBehavior());
            services.AddSingleton(handlerMock.Object);
            services.AddSingleton(_loggerMock.Object);

            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Should_Log_And_Handle_Command()
        {
            var result = await _mediator.SendAsync(new CreateUserCommand { Name = "Bob" });
            Assert.False(string.IsNullOrWhiteSpace(result));

            // Verify logging happened before and after
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling CreateUserCommand")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled CreateUserCommand")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
