using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.Core.Extensions
{
    /// <summary>
    /// Extension methods for adding AltMediator services to the service collection.
    /// </summary>
    public static class MediatorExtensions
    {
        /// <summary>
        /// Adds the AltMediator services to the service collection.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAltMediator(this IServiceCollection services)
        {
            services.AddSingleton<IMediator, Mediator.Mediator>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

            return services;
        }
    }

}
