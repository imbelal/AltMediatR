using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.Core.Extensions
{
    /// <summary>
    /// Startup validation helpers for AltMediator registrations.
    /// </summary>
    public static class StartupValidationExtensions
    {
        /// <summary>
        /// Validates handler and (optionally) behavior registrations and throws on problems.
        /// - Fails if more than one IRequestHandler&lt;TRequest,TResponse&gt; is registered for the same closed type.
        /// - Fails if more than one IRequestHandler&lt;TRequest&gt; (void) is registered for the same closed type.
        /// - Optionally validates behaviors (duplicate registrations and missing behaviors declared in PipelineConfig.BehaviorsInOrder).
        /// Call this at startup after registering handlers/behaviors.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="validateBehaviors">When true, validates behavior registrations and PipelineConfig ordering list.</param>
        /// <returns>The same IServiceCollection for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
        public static IServiceCollection ValidateAltMediatorConfiguration(this IServiceCollection services, bool validateBehaviors = false)
        {
            var errors = new List<string>();

            // Duplicate generic handlers (IRequestHandler<TRequest,TResponse>)
            var genericHandlerGroups = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .GroupBy(d => d.ServiceType)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var g in genericHandlerGroups)
            {
                var impls = g.Select(d => d.ImplementationType?.FullName ?? d.ImplementationInstance?.GetType().FullName ?? "<factory>");
                errors.Add($"Multiple IRequestHandler registered for {g.Key.FullName}: {string.Join(", ", impls)}");
            }

            // Duplicate void handlers (IRequestHandler<TRequest>)
            var voidHandlerGroups = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<>))
                .GroupBy(d => d.ServiceType)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var g in voidHandlerGroups)
            {
                var impls = g.Select(d => d.ImplementationType?.FullName ?? d.ImplementationInstance?.GetType().FullName ?? "<factory>");
                errors.Add($"Multiple IRequestHandler (void) registered for {g.Key.FullName}: {string.Join(", ", impls)}");
            }

            if (validateBehaviors)
            {
                // Gather registered behavior implementation open generic types
                var behaviorImplTypes = services
                    .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                    .Select(d => d.ImplementationType)
                    .Where(t => t != null)
                    .Select(t => t!.IsGenericTypeDefinition ? t! : t!.GetGenericTypeDefinition())
                    .ToList();

                // Duplicate behavior registrations of the same implementation type
                var dupBehaviors = behaviorImplTypes
                    .GroupBy(t => t)
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var g in dupBehaviors)
                {
                    errors.Add($"Duplicate pipeline behavior registrations detected for {g.Key!.FullName}.");
                }

                // Validate PipelineConfig.BehaviorsInOrder entries (if present in the service collection as an instance)
                var pipelineConfig = services
                    .FirstOrDefault(d => d.ServiceType == typeof(PipelineConfig))?.ImplementationInstance as PipelineConfig;

                if (pipelineConfig?.BehaviorsInOrder != null && pipelineConfig.BehaviorsInOrder.Count > 0)
                {
                    var registered = new HashSet<Type>(behaviorImplTypes!);
                    foreach (var expected in pipelineConfig.BehaviorsInOrder)
                    {
                        if (!registered.Contains(expected))
                        {
                            errors.Add($"Behavior listed in PipelineConfig.BehaviorsInOrder is not registered: {expected.FullName}");
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                var message = new StringBuilder()
                    .AppendLine("AltMediator startup validation failed:")
                    .AppendLine(string.Join(Environment.NewLine, errors))
                    .ToString();
                throw new InvalidOperationException(message);
            }

            return services;
        }
    }
}
