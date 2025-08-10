using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Mediator;
using AltMediatR.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AltMediatR.Tests
{
    public class MediatorRuntimeValidationTests
    {
        private sealed class Query : IRequest<string> { }
        private sealed class H1 : IRequestHandler<Query, string>
        {
            public Task<string> HandleAsync(Query request, CancellationToken cancellationToken) => Task.FromResult("1");
        }
        private sealed class H2 : IRequestHandler<Query, string>
        {
            public Task<string> HandleAsync(Query request, CancellationToken cancellationToken) => Task.FromResult("2");
        }

        [Fact]
        public async Task Should_Throw_When_Multiple_Handlers_Resolved_At_Runtime()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddTransient<IRequestHandler<Query, string>, H1>();
            services.AddTransient<IRequestHandler<Query, string>, H2>();
            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new Query()));
            Assert.Contains("Multiple handlers", ex.Message);
        }
    }
}
