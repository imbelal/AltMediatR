using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AltMediatR.Tests
{
    public class StartupValidationTests
    {
        private sealed class DummyRequest : IRequest<string> { }
        private sealed class DummyHandler1 : IRequestHandler<DummyRequest, string>
        {
            public Task<string> HandleAsync(DummyRequest request, CancellationToken cancellationToken) => Task.FromResult("ok1");
        }
        private sealed class DummyHandler2 : IRequestHandler<DummyRequest, string>
        {
            public Task<string> HandleAsync(DummyRequest request, CancellationToken cancellationToken) => Task.FromResult("ok2");
        }

        private sealed class VoidRequest : IRequest { }
        private sealed class VoidHandler1 : IRequestHandler<VoidRequest>
        {
            public Task HandleAsync(VoidRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
        }
        private sealed class VoidHandler2 : IRequestHandler<VoidRequest>
        {
            public Task HandleAsync(VoidRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        [Fact]
        public void Should_Fail_On_Duplicate_Generic_Handlers()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddTransient<IRequestHandler<DummyRequest, string>, DummyHandler1>();
            services.AddTransient<IRequestHandler<DummyRequest, string>, DummyHandler2>();

            Assert.Throws<InvalidOperationException>(() => services.ValidateAltMediatorConfiguration());
        }

        [Fact]
        public void Should_Fail_On_Duplicate_Void_Handlers()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddTransient<IRequestHandler<VoidRequest>, VoidHandler1>();
            services.AddTransient<IRequestHandler<VoidRequest>, VoidHandler2>();

            Assert.Throws<InvalidOperationException>(() => services.ValidateAltMediatorConfiguration());
        }
    }
}
