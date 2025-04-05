using AltMediatR.Core.Behaviors;

namespace AltMediatR.Core.Abstractions
{
    public interface IValidator<in T>
    {
        Task<ValidationResult> ValidateAsync(T request, CancellationToken cancellationToken);
    }

}
