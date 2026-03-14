using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Configurations;
using AltMediatR.Core.Delegates;
using Microsoft.Extensions.Logging;

namespace AltMediatR.Core.Behaviors
{
    public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
        private readonly RetryOptions _options;

        public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger, RetryOptions? options = null)
        {
            _logger = logger;
            _options = options ?? new RetryOptions();
        }

        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var maxAttempts = _options.MaxAttempts;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await next().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Never retry on cancellation — propagate immediately
                    throw;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogError(ex, "[RETRY] Attempt {Attempt} failed: {ErrorMessage}", attempt, ex.Message);
                    await Task.Delay(_options.BaseDelayMs * attempt, cancellationToken).ConfigureAwait(false);
                }
            }

            // Final attempt — let any exception propagate
            return await next().ConfigureAwait(false);
        }
    }

}
