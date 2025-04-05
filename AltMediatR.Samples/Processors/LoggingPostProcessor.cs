using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AltMediatR.Samples.Processors
{
    public class LoggingPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        private readonly ILogger<LoggingPostProcessor<TRequest, TResponse>> _logger;

        public LoggingPostProcessor(ILogger<LoggingPostProcessor<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processed request: {@Request}, Response: {@Response}", request, response);
            return Task.CompletedTask;
        }
    }

}
