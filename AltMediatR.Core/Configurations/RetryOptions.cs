namespace AltMediatR.Core.Configurations
{
    /// <summary>
    /// Options for configuring the <see cref="AltMediatR.Core.Behaviors.RetryBehavior{TRequest,TResponse}"/>.
    /// </summary>
    public sealed class RetryOptions
    {
        private int _maxAttempts = 3;
        private int _baseDelayMs = 200;

        /// <summary>
        /// The maximum number of attempts (including the initial attempt).
        /// Must be at least 1. Defaults to 3.
        /// </summary>
        public int MaxAttempts
        {
            get => _maxAttempts;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxAttempts must be at least 1.");
                _maxAttempts = value;
            }
        }

        /// <summary>
        /// The base delay in milliseconds used for exponential backoff between retries.
        /// The actual delay is <c>BaseDelayMs * attemptNumber</c>. Must be non-negative. Defaults to 200 ms.
        /// </summary>
        public int BaseDelayMs
        {
            get => _baseDelayMs;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BaseDelayMs must be non-negative.");
                _baseDelayMs = value;
            }
        }
    }
}
