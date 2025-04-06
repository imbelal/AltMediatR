using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Processors;
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
        // Cache for compiled delegates: Key = Request Type, Value = Func<handler, request, cancellationToken, Task>
        // The returned Task will actually be Task<TResponse> but stored as Task for the delegate signature.
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

            // 🔥 Resolve and run pre-processors (Keep your existing logic or adapt if needed)
            // Consider if pre/post processors should also be part of the pipeline handled below
            await RunPreProcessorsAsync(request, cancellationToken, requestType);

            // 1. Resolve the handler instance
            // Construct the specific IRequestHandler<TRequest, TResponse> type
            var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            var handler = _serviceProvider.GetService(handlerInterfaceType)
                          ?? throw new InvalidOperationException($"Handler for request type {requestType.Name} returning {responseType.Name} not found. Ensure it is registered.");

            // 2. Get or create the compiled invoker delegate
            var invoker = _handlerInvokers.GetOrAdd(requestType,
                // Factory function to build the delegate if it doesn't exist
                rt => BuildHandlerInvokerDelegate(rt, responseType, handlerInterfaceType));

            // 3. Invoke the compiled delegate
            // The delegate takes objects but calls the strongly-typed HandleAsync internally.
            // The result is Task, but it's actually Task<TResponse> underneath.
            Task taskResult = invoker(handler, request, cancellationToken);

            // 4. Await and cast the result
            // We know the underlying Task is Task<TResponse> because we compiled it that way.
            TResponse response = await (Task<TResponse>)taskResult;

            // 🔥 Resolve and run post-processors (Keep your existing logic or adapt if needed)
            await RunPostProcessorsAsync(request, cancellationToken, requestType); // Assuming TResponse isn't needed here

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
