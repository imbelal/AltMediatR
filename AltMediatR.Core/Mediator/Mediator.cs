using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using AltMediatR.Core.Deligates;
using AltMediatR.Core.Configurations;
using System.Linq;
using System.Collections.Concurrent;

namespace AltMediatR.Core.Mediator
{
    /// <summary>
    /// The core Mediator implementation responsible for dispatching requests to their single handler,
    /// executing pre/post processors, and orchestrating the request pipeline behaviors.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        // Cache the open generic method and closed generic variants to avoid repeated reflection
        private static readonly MethodInfo s_sendCoreOpenMethod = typeof(Mediator)
            .GetMethod("SendCoreAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Pipeline method not found.");
        private static readonly ConcurrentDictionary<(Type Request, Type Response), MethodInfo> s_sendCoreCache = new();

        // Cache for the void (non-generic response) path
        private static readonly MethodInfo s_sendCoreVoidOpenMethod = typeof(Mediator)
            .GetMethod("SendCoreVoidAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Void pipeline method not found.");
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_sendCoreVoidCache = new();

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Sends a request to its handler and returns a response, executing:
        /// Pre-processors -> Pipeline Behaviors -> Handler -> Post-processors.
        /// </summary>
        /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
        /// <param name="request">The request instance.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The handler response.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="request"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If internal pipeline cannot be invoked or handler cannot be resolved.</exception>
        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            cancellationToken.ThrowIfCancellationRequested(); // Fast-fail if already cancelled

            var requestType = request.GetType();
            var responseType = typeof(TResponse);

            // Use cached closed generic SendCoreAsync<TRequest,TResponse>
            var generic = s_sendCoreCache.GetOrAdd((requestType, responseType), key =>
                s_sendCoreOpenMethod.MakeGenericMethod(key.Request, key.Response));

            var task = (Task<TResponse>?)generic.Invoke(this, new object[] { request, cancellationToken });
            if (task == null) throw new InvalidOperationException("Pipeline invocation failed.");
            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a request to its handler. This non-generic method adapts to the existing generic pipeline
        /// by using Unit internally, but hides it from the public API.
        /// </summary>
        /// <param name="request">The request instance.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="request"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If internal pipeline cannot be invoked or handler cannot be resolved.</exception>
        public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            cancellationToken.ThrowIfCancellationRequested(); // Fast-fail if already cancelled

            var requestType = request.GetType();
            var generic = s_sendCoreVoidCache.GetOrAdd(requestType, t => s_sendCoreVoidOpenMethod.MakeGenericMethod(t));
            if (generic.Invoke(this, new object[] { request, cancellationToken }) is not Task task)
                throw new InvalidOperationException("Void pipeline invocation failed.");
            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Strongly-typed execution path for a request/response pair that composes the full pipeline:
        /// 1) Runs all pre-processors for TRequest
        /// 2) Orders and composes registered IPipelineBehavior&lt;TRequest,TResponse&gt; around the handler
        /// 3) Invokes the handler exactly once
        /// 4) Runs all post-processors for (TRequest, TResponse)
        /// </summary>
        private async Task<TResponse> SendCoreAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
            where TRequest : IRequest<TResponse>
        {
            // 1) Pre-processors (do not call the handler)
            var preProcessors = _serviceProvider.GetServices<IRequestPreProcessor<TRequest>>();
            foreach (var pre in preProcessors)
                await pre.ProcessAsync(request, cancellationToken).ConfigureAwait(false);

            // 2) Resolve the single handler for TRequest -> TResponse
            var handlers = _serviceProvider.GetServices<IRequestHandler<TRequest, TResponse>>().ToList();
            if (handlers.Count == 0)
                throw new InvalidOperationException($"Handler for {typeof(TRequest).Name} returning {typeof(TResponse).Name} not found. Ensure it is registered.");
            if (handlers.Count > 1)
                throw new InvalidOperationException($"Multiple handlers found for {typeof(TRequest).Name} returning {typeof(TResponse).Name}. Exactly one handler must be registered. Found: {string.Join(", ", handlers.Select(h => h.GetType().FullName))}");
            var handler = handlers[0];

            // Terminal delegate that calls the handler exactly once
            RequestHandlerDelegate<TResponse> next = () => handler.HandleAsync(request, cancellationToken);

            // 3) Resolve behaviors and optionally apply ordering from PipelineConfig
            var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();

            var pipelineConfig = _serviceProvider.GetService<PipelineConfig>();
            if (pipelineConfig?.BehaviorsInOrder?.Count > 0)
            {
                // Map open generic behavior definitions to an index based on desired order
                var orderIndex = pipelineConfig.BehaviorsInOrder
                    .Select((t, i) => new { t, i })
                    .ToDictionary(x => x.t, x => x.i);

                behaviors = behaviors
                    .OrderBy(b =>
                    {
                        var def = b.GetType().IsGenericType ? b.GetType().GetGenericTypeDefinition() : b.GetType();
                        return orderIndex.TryGetValue(def, out var idx) ? idx : int.MaxValue;
                    })
                    .ToList();
            }

            // Compose the middleware-style pipeline in reverse so the first registered behavior runs outermost
            for (int i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var capturedNext = next;
                next = () => behavior.HandleAsync(request, cancellationToken, capturedNext);
            }

            // 4) Execute pipeline -> handler
            var response = await next().ConfigureAwait(false);

            // Post-processors (observe both request and response; do not re-invoke the handler)
            var postProcessors = _serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>();
            foreach (var post in postProcessors)
                await post.ProcessAsync(request, response, cancellationToken).ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// New: true void pipeline, hides Unit from public API but applies same behaviors and processors internally
        /// </summary>
        private async Task SendCoreVoidAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
            where TRequest : IRequest
        {
            // Pre-processors
            var preProcessors = _serviceProvider.GetServices<IRequestPreProcessor<TRequest>>();
            foreach (var pre in preProcessors)
                await pre.ProcessAsync(request, cancellationToken).ConfigureAwait(false);

            // Resolve void handler
            var handlers = _serviceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
            if (handlers.Count == 0)
                throw new InvalidOperationException($"Handler for {typeof(TRequest).Name} not found. Ensure it is registered.");
            if (handlers.Count > 1)
                throw new InvalidOperationException($"Multiple handlers found for {typeof(TRequest).Name}. Exactly one handler must be registered. Found: {string.Join(", ", handlers.Select(h => h.GetType().FullName))}");
            var handler = handlers[0];

            // Terminal delegate adapted to Unit for internal behaviors
            RequestHandlerDelegate<Unit> next = async () =>
            {
                await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
                return Unit.Value;
            };

            // Behaviors (use Unit internally)
            var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, Unit>>().ToList();

            var pipelineConfig = _serviceProvider.GetService<PipelineConfig>();
            if (pipelineConfig?.BehaviorsInOrder?.Count > 0)
            {
                // Map open generic behavior definitions to an index based on desired order
                var orderIndex = pipelineConfig.BehaviorsInOrder
                    .Select((t, i) => new { t, i })
                    .ToDictionary(x => x.t, x => x.i);

                behaviors = behaviors
                    .OrderBy(b =>
                    {
                        var def = b.GetType().IsGenericType ? b.GetType().GetGenericTypeDefinition() : b.GetType();
                        return orderIndex.TryGetValue(def, out var idx) ? idx : int.MaxValue;
                    })
                    .ToList();
            }

            for (int i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var capturedNext = next;
                next = () => behavior.HandleAsync(request, cancellationToken, capturedNext);
            }

            // Execute pipeline
            var _ = await next().ConfigureAwait(false);

            // Post-processors (use Unit internally)
            var postProcessors = _serviceProvider.GetServices<IRequestPostProcessor<TRequest, Unit>>();
            foreach (var post in postProcessors)
                await post.ProcessAsync(request, Unit.Value, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Publishes a notification to all registered notification handlers.
        /// Note: Behaviors are not applied to notifications; each handler is invoked directly.
        /// </summary>
        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(notification, cancellationToken);
            }
        }

    }

}
