using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AltMediatR.Samples.Processors
{
    class LoggingPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        private readonly ILogger<LoggingPreProcessor<TRequest>> _logger;

        public LoggingPreProcessor(ILogger<LoggingPreProcessor<TRequest>> logger)
        {
            _logger = logger;
        }

        public Task ProcessAsync(TRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing request: {@Request}", request);
            return Task.CompletedTask;
        }
    }
}
