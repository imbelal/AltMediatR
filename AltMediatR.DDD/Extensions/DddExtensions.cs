using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Behaviors;
using AltMediatR.DDD.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AltMediatR.DDD.Extensions
{
    public static class DddExtensions
    {
        public static IServiceCollection AddDddIntegrationDdd(this IServiceCollection services)
        {
            services.AddSingleton(new DddMediatorOptions());
            return services;
        }

        public static IServiceCollection ConfigureDddMediator(this IServiceCollection services, Action<DddMediatorOptions> configure)
        {
            var opts = new DddMediatorOptions();
            configure?.Invoke(opts);
            return services.AddSingleton(opts);
        }

        public static IServiceCollection AddTransactionalOutboxBehavior(this IServiceCollection services)
            => services.AddTransient(typeof(AltMediatR.Core.Abstractions.IPipelineBehavior<,>), typeof(Behaviors.TransactionalEventDispatcherBehavior<,>));

        public static IServiceCollection AddCachingForQueries(this IServiceCollection services, Action<CachingOptions> configure)
        {
            var options = new CachingOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            return services.AddTransient(typeof(AltMediatR.Core.Abstractions.IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        }

        public static IServiceCollection AddInMemoryOutboxStore(this IServiceCollection services)
            => services.AddSingleton<IOutboxStore, Infrastructure.InMemoryOutboxStore>();

        // Basic in-memory publisher for local dev/testing
        private sealed class InMemoryPublisher : IIntegrationEventPublisher
        {
            public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
        public static IServiceCollection AddInMemoryIntegrationEventPublisher(this IServiceCollection services)
            => services.AddSingleton<IIntegrationEventPublisher, InMemoryPublisher>();

        public static IServiceCollection AddInMemoryOutboxProcessor(this IServiceCollection services)
            => services.AddSingleton<IOutboxProcessor, Infrastructure.InMemoryOutboxProcessor>();

        public static IServiceCollection AddOutboxProcessorHostedService(this IServiceCollection services, TimeSpan? pollInterval = null)
        {
            var interval = pollInterval ?? TimeSpan.FromSeconds(10);
            services.AddSingleton<IHostedService>(sp => new Infrastructure.OutboxProcessorHostedService(
                sp.GetRequiredService<IOutboxProcessor>(),
                interval));
            return services;
        }

        public static IServiceCollection AddInMemoryInboundMessageProcessor(this IServiceCollection services)
            => services.AddSingleton<IInboundMessageProcessor, Infrastructure.InMemoryInboundMessageProcessor>()
                       .AddSingleton(sp => (Infrastructure.InMemoryInboundMessageProcessor)sp.GetRequiredService<IInboundMessageProcessor>());
        public static IServiceCollection UseInMemoryLoopbackPublisher(this IServiceCollection services)
            => services.AddSingleton<IIntegrationEventPublisher, Infrastructure.InMemoryLoopbackPublisher>();
    }
}
