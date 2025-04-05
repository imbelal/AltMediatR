using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Processors;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace AltMediatR.Core.Mediator
{
    /// <summary>
    /// Mediator class that implements IMediator interface.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Type, MethodInfo> _handleMethods = new();

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Sends a request to the appropriate handler and returns the response.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();

            // 🔥 Resolve and run pre-processors via RequestPreProcessor
            await RunPreProcessorsAsync(request, cancellationToken, requestType);

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var handler = _serviceProvider.GetService(handlerType)
                          ?? throw new InvalidOperationException($"Handler for {requestType.Name} not found.");

            var handleMethod = _handleMethods.GetOrAdd(handlerType,
                t => t.GetMethod("HandleAsync") ?? throw new InvalidOperationException("HandleAsync method not found."));

            var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });
            var response = result is Task<TResponse> task ? await task : (TResponse)result;

            // 🔥 Resolve and run post-processors via RequestPostProcessor
            await RunPostProcessorsAsync(request, cancellationToken, requestType);

            return response;
        }

        /// <summary>
        /// Runs pre-processors for the request.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private async Task RunPreProcessorsAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken,
            Type requestType)
        {
            var wrapperType = typeof(RequestPreProcessor<,>).MakeGenericType(requestType, typeof(TResponse));
            var wrapper = _serviceProvider.GetService(wrapperType);
            if (wrapper != null)
            {
                var method = wrapperType.GetMethod("ProcessAsync");
                if (method != null)
                {
                    var taskResult = (Task?)method.Invoke(wrapper, new object[] { request, cancellationToken });
                    if (taskResult != null)
                        await taskResult;
                }
            }
        }

        /// <summary>
        /// Runs post-processors for the request.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private async Task RunPostProcessorsAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken,
            Type requestType)
        {
            var wrapperType = typeof(RequestPostProcessor<,>).MakeGenericType(requestType, typeof(TResponse));
            var wrapper = _serviceProvider.GetService(wrapperType);
            if (wrapper != null)
            {
                var method = wrapperType.GetMethod("ProcessAsync");
                if (method != null)
                {
                    var taskResult = (Task?)method.Invoke(wrapper, new object[] { request, cancellationToken });
                    if (taskResult != null)
                        await taskResult;
                }
            }
        }

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification"></typeparam>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();

            // Loop through all handlers and invoke their HandleAsync method
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(notification, cancellationToken);
            }
        }

    }

}
