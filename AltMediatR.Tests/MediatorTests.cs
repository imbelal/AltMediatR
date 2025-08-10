using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AltMediatR.Tests
{
    public class MediatorTests
    {
        private readonly IMediator _mediator;
        private readonly Mock<IRequestHandler<CreateTestUserCommand, string>> _createHandlerMock;
        private readonly Mock<IRequestHandler<GetTestUserQuery, string>> _queryHandlerMock;

        public record CreateTestUserCommand(string Name) : IRequest<string>;
        public record GetTestUserQuery(string UserId) : IRequest<string>;

        public MediatorTests()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();

            _createHandlerMock = new Mock<IRequestHandler<CreateTestUserCommand, string>>();
            _createHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<CreateTestUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("ok");

            _queryHandlerMock = new Mock<IRequestHandler<GetTestUserQuery, string>>();
            _queryHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<GetTestUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetTestUserQuery q, CancellationToken _) => $"User found with ID: {q.UserId}");

            services.AddSingleton(_createHandlerMock.Object);
            services.AddSingleton(_queryHandlerMock.Object);

            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Should_Handle_CreateUserCommand()
        {
            var result = await _mediator.SendAsync(new CreateTestUserCommand("Alice"));
            Assert.NotNull(result);
            _createHandlerMock.Verify(h => h.HandleAsync(It.IsAny<CreateTestUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Should_Handle_GetUserQuery()
        {
            var result = await _mediator.SendAsync(new GetTestUserQuery("xyz-123"));
            Assert.Equal("User found with ID: xyz-123", result);
            _queryHandlerMock.Verify(h => h.HandleAsync(It.IsAny<GetTestUserQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
