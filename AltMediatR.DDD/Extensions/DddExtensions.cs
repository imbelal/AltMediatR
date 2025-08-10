using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Behaviors;
using AltMediatR.DDD.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.DDD.Extensions
{
    public static class DddExtensions
    {
        public static IServiceCollection AddAltMediatorDdd(this IServiceCollection services)
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
    }
}
