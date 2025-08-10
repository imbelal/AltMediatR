using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.DDD.Extensions
{
    /// <summary>
    /// Registration helpers for DDD-specific handler types.
    /// </summary>
    public static class HandlerRegistrarExtensions
    {
        /// <summary>
        /// Registers IIntegrationEventHandler<> implementations from the specified assembly as transient services.
        /// </summary>
        public static IServiceCollection RegisterDddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var registrations = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && (
                        i.GetGenericTypeDefinition() == typeof(Abstractions.IIntegrationEventHandler<>)
                    ))
                    .Select(i => new { Interface = i, Implementation = t }))
                .ToList();

            foreach (var reg in registrations)
            {
                var iface = reg.Interface;
                var impl = reg.Implementation;

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
