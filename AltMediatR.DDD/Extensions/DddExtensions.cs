using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Behaviors;
using AltMediatR.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.DDD.Extensions
{
    public static class DddExtensions
    {
        public static IServiceCollection AddAltMediatorDdd(this IServiceCollection services)
        {
            services.AddScoped<IDomainEventQueue, Infrastructure.DomainEventQueue>();
            services.AddScoped<IIntegrationEventQueue, Infrastructure.IntegrationEventQueue>();
            return services;
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
    }
}
