using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Behaviors;
using AltMediatR.Core.Deligates;
using Moq;
using System.ComponentModel.DataAnnotations;
using ValidationResult = AltMediatR.Core.Behaviors.ValidationResult;

namespace AltMediatR.Tests
{
    public class SampleValidationRequest : IRequest<string>
    {
        public required string Name { get; set; }
    }

    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_ThrowsValidationException_WhenInvalid()
        {
            // Arrange
            var validator = new Mock<IValidator<SampleValidationRequest>>();
            validator
                .Setup(v => v.ValidateAsync(It.IsAny<SampleValidationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult { Errors = { "Name is required." } });

            var behavior = new ValidationBehavior<SampleValidationRequest, string>(validator.Object);
            var invalidRequest = new SampleValidationRequest { Name = "" };

            RequestHandlerDelegate<string> next = () => Task.FromResult("Should not be called");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                behavior.HandleAsync(invalidRequest, CancellationToken.None, next));

            Assert.Contains("Name is required", exception.Message);
            validator.Verify(v => v.ValidateAsync(It.IsAny<SampleValidationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CallsNext_WhenValid()
        {
            // Arrange
            var validator = new Mock<IValidator<SampleValidationRequest>>();
            validator
                .Setup(v => v.ValidateAsync(It.IsAny<SampleValidationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<SampleValidationRequest, string>(validator.Object);
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
            validator.Verify(v => v.ValidateAsync(It.IsAny<SampleValidationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
