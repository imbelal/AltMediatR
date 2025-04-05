using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.Samples.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.Tests
{
    public class BehaviorTests
    {
        private readonly IMediator _mediator;

        public BehaviorTests()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddTransient<IRequestHandler<CreateUserCommand, string>, CreateUserHandler>();

            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Should_Log_And_Handle_Command()
        {
            var result = await _mediator.SendAsync(new CreateUserCommand { Name = "Bob" });
            Assert.NotEmpty(result);
        }
    }
}
