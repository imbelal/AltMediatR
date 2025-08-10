using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Configurations;
using AltMediatR.Core.Extensions;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AltMediatR.Tests
{
    public class BehaviorOrderingTests
    {
        public interface IProbe { void Hit(string name); }

        private sealed class Req : IRequest<string> { }
        private sealed class Handler : IRequestHandler<Req, string>
        {
            public Task<string> HandleAsync(Req request, CancellationToken cancellationToken) => Task.FromResult("ok");
        }

        private sealed class B1<TReq, TRes> : IPipelineBehavior<TReq, TRes> where TReq : IRequest<TRes>
        {
            private readonly IProbe _probe;
            public B1(IProbe probe) => _probe = probe;
            public Task<TRes> HandleAsync(TReq request, CancellationToken cancellationToken, RequestHandlerDelegate<TRes> next)
            {
                _probe.Hit("B1");
                return next();
            }
        }
        private sealed class B2<TReq, TRes> : IPipelineBehavior<TReq, TRes> where TReq : IRequest<TRes>
        {
            private readonly IProbe _probe;
            public B2(IProbe probe) => _probe = probe;
            public Task<TRes> HandleAsync(TReq request, CancellationToken cancellationToken, RequestHandlerDelegate<TRes> next)
            {
                _probe.Hit("B2");
                return next();
            }
        }

        [Fact]
        public async Task Should_Respect_Behavior_Order()
        {
            var probe = new Mock<IProbe>(MockBehavior.Strict);
            var seq = new MockSequence();
            probe.InSequence(seq).Setup(p => p.Hit("B2"));
            probe.InSequence(seq).Setup(p => p.Hit("B1"));

            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddSingleton(probe.Object);
            services.AddTransient<IRequestHandler<Req, string>, Handler>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(B1<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(B2<,>));

            var config = new PipelineConfig();
            config.BehaviorsInOrder.Clear();
            config.BehaviorsInOrder.Add(typeof(B2<,>));
            config.BehaviorsInOrder.Add(typeof(B1<,>));
            services.AddSingleton(config);

            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
            var result = await mediator.SendAsync(new Req());
            Assert.Equal("ok", result);

            probe.Verify(p => p.Hit("B2"), Times.Once);
            probe.Verify(p => p.Hit("B1"), Times.Once);
            probe.VerifyNoOtherCalls();
        }
    }
}
