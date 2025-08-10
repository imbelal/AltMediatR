using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AltMediatR.Tests
{
    public class VoidPipelineTests
    {
        public sealed class Cmd : ICommand { }

        [Fact]
        public async Task Should_Invoke_Void_Handler_Once()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();

            var handlerMock = new Mock<IRequestHandler<Cmd>>();
            handlerMock
                .Setup(h => h.HandleAsync(It.IsAny<Cmd>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            services.AddSingleton(handlerMock.Object);
            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

            await mediator.SendAsync(new Cmd());

            handlerMock.Verify(h => h.HandleAsync(It.IsAny<Cmd>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
