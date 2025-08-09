using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using AltMediatR.Core.Deligates;
using AltMediatR.Core.Configurations;
using System.Linq;

namespace AltMediatR.Core.Mediator
{
    /// <summary>
    /// The core Mediator implementation responsible for dispatching requests to their single handler,
    /// executing pre/post processors, and orchestrating the request pipeline behaviors.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

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

            // Delegate to strongly-typed core pipeline (avoids repeated reflection inside the pipeline)
            var core = typeof(Mediator).GetMethod("SendCoreAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException("Pipeline method not found.");
            var generic = core.MakeGenericMethod(requestType, responseType);
            var task = (Task<TResponse>?)generic.Invoke(this, new object[] { request, cancellationToken });
            if (task == null) throw new InvalidOperationException("Pipeline invocation failed.");
            return await task.ConfigureAwait(false);
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
            var handler = _serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
                          ?? throw new InvalidOperationException($"Handler for {typeof(TRequest).Name} returning {typeof(TResponse).Name} not found. Ensure it is registered.");

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
