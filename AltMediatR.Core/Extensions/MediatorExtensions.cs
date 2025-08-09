using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System;
using AltMediatR.Core.Configurations;

namespace AltMediatR.Core.Extensions
{
    /// <summary>
    /// Extension methods for adding AltMediator services to the service collection.
    /// </summary>
    public static class MediatorExtensions
    {
        /// <summary>
        /// Adds the core AltMediator services to the service collection.
        /// Behaviors are not registered by default; register them explicitly using helper methods below.
        /// </summary>
        public static IServiceCollection AddAltMediator(this IServiceCollection services)
        {
            // Scoped lifetime so handlers can depend on scoped services (e.g., DbContext)
            services.AddScoped<IMediator, Mediator.Mediator>();

            // Register a default validator so ValidationBehavior can resolve when no custom validator is provided
            services.AddTransient(typeof(IValidator<>), typeof(NoOpValidator<>));

            return services;
        }

        /// <summary>
        /// Adds AltMediator and allows the caller to configure pipeline behaviors explicitly.
        /// Example:
        /// services.AddAltMediator(s => {
        ///     s.AddLoggingBehavior().AddValidationBehavior().AddPerformanceBehavior().AddRetryBehavior().AddCachingBehavior();
        /// });
        /// </summary>
        public static IServiceCollection AddAltMediator(this IServiceCollection services, Action<IServiceCollection> configureBehaviors)
        {
            AddAltMediator(services);
            configureBehaviors?.Invoke(services);
            return services;
        }

        /// <summary>
        /// Register an open-generic pipeline behavior type, e.g., typeof(LoggingBehavior<,>).
        /// </summary>
        public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type openGenericBehaviorType)
        {
            if (openGenericBehaviorType == null) throw new ArgumentNullException(nameof(openGenericBehaviorType));
            if (!openGenericBehaviorType.IsGenericTypeDefinition || openGenericBehaviorType.GetGenericArguments().Length != 2)
                throw new ArgumentException("Behavior type must be an open generic with two type parameters, e.g., typeof(MyBehavior<,>).", nameof(openGenericBehaviorType));

            services.AddTransient(typeof(IPipelineBehavior<,>), openGenericBehaviorType);
            return services;
        }

        public static IServiceCollection AddLoggingBehavior(this IServiceCollection services)
            => services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

        public static IServiceCollection AddValidationBehavior(this IServiceCollection services)
            => services.AddPipelineBehavior(typeof(ValidationBehavior<,>));

        public static IServiceCollection AddPerformanceBehavior(this IServiceCollection services)
            => services.AddPipelineBehavior(typeof(PerformanceBehavior<,>));

        public static IServiceCollection AddRetryBehavior(this IServiceCollection services)
            => services.AddPipelineBehavior(typeof(RetryBehavior<,>));

        public static IServiceCollection AddCachingBehavior(this IServiceCollection services)
            => services.AddPipelineBehavior(typeof(CachingBehavior<,>));

        // Restrict caching to queries (behavior itself already bypasses non-queries)
        public static IServiceCollection AddCachingForQueries(this IServiceCollection services)
            => services.AddCachingBehavior();

        public static IServiceCollection AddCachingForQueries(this IServiceCollection services, Action<CachingOptions> configure)
        {
            var options = new CachingOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            return services.AddCachingBehavior();
        }
    }

}
