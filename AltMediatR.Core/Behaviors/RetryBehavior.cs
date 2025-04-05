using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;
using Microsoft.Extensions.Logging;

namespace AltMediatR.Core.Behaviors
{
    public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;

        public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await next();
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogError(ex, $"[RETRY] Attempt {attempt} failed: {ex.Message}");
                    await Task.Delay(200 * attempt);
                }
            }

            // Final attempt
            return await next();
        }
    }

}
