using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Linq;

namespace AltMediatR.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering handlers in the service collection.
    /// </summary>
    public static class HandlerRegistrar
    {
        /// <summary>
        /// Registers all IRequestHandler, IRequestHandler (void), INotificationHandler, IRequestPreProcessor and IRequestPostProcessor
        /// implementations from the specified assembly as transient services.
        /// Supports both closed and open-generic implementations.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assembly"></param>
        public static IServiceCollection RegisterHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var registrations = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && (
                        i.GetGenericTypeDefinition() == typeof(Abstractions.IRequestHandler<,>) ||
                        i.GetGenericTypeDefinition() == typeof(Abstractions.IRequestHandler<>) ||
                        i.GetGenericTypeDefinition() == typeof(Abstractions.INotificationHandler<>) ||
                        i.GetGenericTypeDefinition() == typeof(Abstractions.IRequestPreProcessor<>) ||
                        i.GetGenericTypeDefinition() == typeof(Abstractions.IRequestPostProcessor<,>)
                    ))
                    .Select(i => new { Interface = i, Implementation = t }))
                .ToList();

            foreach (var reg in registrations)
            {
                var iface = reg.Interface;
                var impl = reg.Implementation;

                // If implementation is open generic, register open-generic service mapping
                if (impl.IsGenericTypeDefinition && iface.IsGenericType)
                {
                    services.AddTransient(iface.GetGenericTypeDefinition(), impl);
                }
                else
                {
                    services.AddTransient(iface, impl);
                }
            }

            return services;
        }
    }
}
