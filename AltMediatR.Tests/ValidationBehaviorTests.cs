using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Deligates;
using System.ComponentModel.DataAnnotations;
using ValidationResult = AltMediatR.Core.Behaviors.ValidationResult;

namespace AltMediatR.Tests
{
    public class SampleValidationRequest : IRequest<string>
    {
        public string Name { get; set; }
    }

    // Simple validator implementation without using third-party libs
    public class SampleValidationRequestValidator : IValidator<SampleValidationRequest>
    {
        public Task<ValidationResult> ValidateAsync(SampleValidationRequest request, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(request.Name))
                result.Errors.Add("Name is required.");

            return Task.FromResult(result);
        }
    }

    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_ThrowsValidationException_WhenInvalid()
        {
            // Arrange
            var validator = new SampleValidationRequestValidator();
            var behavior = new ValidationBehavior<SampleValidationRequest, string>(validator);

            var invalidRequest = new SampleValidationRequest { Name = "" };

            RequestHandlerDelegate<string> next = () => Task.FromResult("Should not be called");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                behavior.HandleAsync(invalidRequest, CancellationToken.None, next));

            Assert.Contains("Name is required", exception.Message);
        }

        [Fact]
        public async Task Handle_CallsNext_WhenValid()
        {
            // Arrange
            var validator = new SampleValidationRequestValidator();
            var behavior = new ValidationBehavior<SampleValidationRequest, string>(validator);

            var validRequest = new SampleValidationRequest { Name = "Valid" };

            bool nextWasCalled = false;
            RequestHandlerDelegate<string> next = () =>
            {
                nextWasCalled = true;
                return Task.FromResult("Success");
            };

            // Act
            var result = await behavior.HandleAsync(validRequest, CancellationToken.None, next);

            // Assert
            Assert.True(nextWasCalled);
            Assert.Equal("Success", result);
        }
    }
}
