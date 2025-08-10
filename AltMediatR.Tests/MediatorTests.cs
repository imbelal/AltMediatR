using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.Samples.Commands;
using AltMediatR.Samples.Queries;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AltMediatR.Tests
{
    public class MediatorTests
    {
        private readonly IMediator _mediator;
        private readonly Mock<IRequestHandler<CreateUserCommand, string>> _createHandlerMock;
        private readonly Mock<IRequestHandler<GetUserQuery, string>> _queryHandlerMock;

        public MediatorTests()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();

            _createHandlerMock = new Mock<IRequestHandler<CreateUserCommand, string>>();
            _createHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("ok");

            _queryHandlerMock = new Mock<IRequestHandler<GetUserQuery, string>>();
            _queryHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetUserQuery q, CancellationToken _) => $"User found with ID: {q.UserId}");

            services.AddSingleton(_createHandlerMock.Object);
            services.AddSingleton(_queryHandlerMock.Object);

            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Should_Handle_CreateUserCommand()
        {
            var result = await _mediator.SendAsync(new CreateUserCommand { Name = "Alice" });
            Assert.NotNull(result);
            _createHandlerMock.Verify(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Should_Handle_GetUserQuery()
        {
            var result = await _mediator.SendAsync(new GetUserQuery { UserId = "xyz-123" });
            Assert.Equal("User found with ID: xyz-123", result);
            _queryHandlerMock.Verify(h => h.HandleAsync(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
