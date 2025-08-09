using AltMediatR.Core.Abstractions;

namespace AltMediatR.Core.Behaviors
{
    // Fallback validator that always succeeds. Used when no user validator is registered.
    internal class NoOpValidator<TRequest> : IValidator<TRequest>
    {
        public Task<ValidationResult> ValidateAsync(TRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ValidationResult());
        }
    }
}
