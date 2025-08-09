using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;

namespace AltMediatR.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering handlers in the service collection.
    /// </summary>
    public static class HandlerRegistrar
    {
        /// <summary>
        /// Registers all IRequestHandler and INotificationHandler implementations from the specified assembly as transient services.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assembly"></param>
        public static void RegisterHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
                .Where(x => x.Interface.IsGenericType &&
                            (x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                             x.Interface.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                .ToList();

            foreach (var pair in types)
            {
                services.AddTransient(pair.Interface, pair.Type);
            }
        }

        /// <summary>
        /// Register a custom request pre-processor.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        public static void RegisterRequestPreProcessor(this IServiceCollection services, Type type)
        {
            services.AddTransient(typeof(IRequestPreProcessor<>), type);
        }

        /// <summary>
        /// Register a custom request post-processor.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        public static void RegisterRequestPostProcessor(this IServiceCollection services, Type type)
        {
            services.AddTransient(typeof(IRequestPostProcessor<,>), type);
        }

    }
}
