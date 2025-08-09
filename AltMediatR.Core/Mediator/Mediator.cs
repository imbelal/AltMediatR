using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace AltMediatR.Core.Mediator
{
    /// <summary>
    /// Mediator class that implements IMediator interface.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        // Cache for value-returning handlers: Key = Request Type
        private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerInvokers = new();

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
            cancellationToken.ThrowIfCancellationRequested(); // Early check

            var requestType = request.GetType();
            var responseType = typeof(TResponse); // Capture TResponse type

            // Run pre-processors
            await RunPreProcessorsAsync(request, cancellationToken, requestType);

            // Resolve the handler instance
            var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            var handler = _serviceProvider.GetService(handlerInterfaceType)
                          ?? throw new InvalidOperationException($"Handler for request type {requestType.Name} returning {responseType.Name} not found. Ensure it is registered.");

            // Get or create the compiled invoker delegate
            var invoker = _handlerInvokers.GetOrAdd(requestType,
                // Factory function to build the delegate if it doesn't exist
                rt => BuildHandlerInvokerDelegate(rt, responseType, handlerInterfaceType));

            // Invoke the compiled delegate and await the Task<TResponse>
            Task taskResult = invoker(handler, request, cancellationToken);
            TResponse response = await (Task<TResponse>)taskResult;

            // Run post-processors with response
            await RunPostProcessorsAsync(request, response, cancellationToken, requestType);

            return response;
        }

        /// <summary>
        /// Builds a delegate to invoke the handler's HandleAsync method.
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="responseType"></param>
        /// <param name="handlerInterfaceType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static Func<object, object, CancellationToken, Task> BuildHandlerInvokerDelegate(
            Type requestType, Type responseType, Type handlerInterfaceType)
        {
            try
            {
                // Find the HandleAsync(TRequest, CancellationToken) method on the specific handler interface
                MethodInfo handleMethodInfo = handlerInterfaceType.GetMethod(
                    "HandleAsync",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                    null, // Use default binder
                    new[] { requestType, typeof(CancellationToken) }, // Signature we are looking for
                    null  // No parameter modifiers
                ) ?? throw new InvalidOperationException($"Could not find HandleAsync({requestType.Name}, CancellationToken) method on {handlerInterfaceType.Name}");

                // Define expression parameters for the Func<object, object, CancellationToken, Task>
                var handlerObjParam = Expression.Parameter(typeof(object), "handler");
                var requestObjParam = Expression.Parameter(typeof(object), "request");
                var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                // Cast the object parameters to their specific types
                var handlerTypedParam = Expression.Convert(handlerObjParam, handlerInterfaceType);
                var requestTypedParam = Expression.Convert(requestObjParam, requestType);

                // Create the method call expression
                // Calls handlerTyped.HandleAsync(requestTyped, ctParam)
                // The result of this call is Task<TResponse>
                var call = Expression.Call(handlerTypedParam, handleMethodInfo, requestTypedParam, ctParam);

                // Compile the expression into a delegate
                // The 'call' returns Task<TResponse>, which is assignable to the 'Task' return type of the Func.
                var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task>>(
                    call,
                    handlerObjParam, requestObjParam, ctParam);

                return lambda.Compile();
            }
            catch (Exception ex)
            {
                // Wrap exception for better context during debugging
                throw new InvalidOperationException($"Failed to compile handler invoker for request {requestType.Name} -> {responseType.Name}. See inner exception.", ex);
            }
        }

        /// <summary>
        /// Runs pre-processors for the request.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private async Task RunPreProcessorsAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken, Type requestType)
        {
            // Resolve all IRequestPreProcessor<TRequest> for this request type and execute them
            var preProcessorInterface = typeof(IRequestPreProcessor<>).MakeGenericType(requestType);
            var processAsync = preProcessorInterface.GetMethod("ProcessAsync");
            if (processAsync == null)
                return;

            var processors = _serviceProvider.GetServices(preProcessorInterface);
            foreach (var processor in processors)
            {
                var task = (Task?)processAsync.Invoke(processor, new object[] { request, cancellationToken });
                if (task != null)
                    await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs post-processors for the request with response.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private async Task RunPostProcessorsAsync<TResponse>(IRequest<TResponse> request, TResponse response, CancellationToken cancellationToken, Type requestType)
        {
            // Resolve all IRequestPostProcessor<TRequest, TResponse> and execute them
            var postProcessorInterface = typeof(IRequestPostProcessor<,>).MakeGenericType(requestType, typeof(TResponse));
            var processAsync = postProcessorInterface.GetMethod("ProcessAsync");
            if (processAsync == null)
                return;

            var processors = _serviceProvider.GetServices(postProcessorInterface);
            foreach (var processor in processors)
            {
                var task = (Task?)processAsync.Invoke(processor, new object[] { request, response, cancellationToken });
                if (task != null)
                    await task.ConfigureAwait(false);
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
