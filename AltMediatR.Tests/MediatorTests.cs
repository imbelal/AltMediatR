using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.Samples.Commands;
using AltMediatR.Samples.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.Tests
{
    public class MediatorTests
    {
        private readonly IMediator _mediator;

        public MediatorTests()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddTransient<IRequestHandler<CreateUserCommand, string>, CreateUserHandler>();
            services.AddTransient<IRequestHandler<GetUserQuery, string>, GetUserHandler>();

            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Fact]
        public async Task Should_Handle_CreateUserCommand()
        {
            var result = await _mediator.SendAsync(new CreateUserCommand { Name = "Alice" });
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Should_Handle_GetUserQuery()
        {
            var result = await _mediator.SendAsync(new GetUserQuery { UserId = "xyz-123" });
            Assert.Equal("User found with ID: xyz-123", result);
        }
    }
}
