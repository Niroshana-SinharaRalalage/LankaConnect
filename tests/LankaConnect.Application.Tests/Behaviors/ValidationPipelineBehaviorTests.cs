using FluentValidation;
using LankaConnect.Application.Common.Behaviors;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Commands.CreateUser;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Tests.Behaviors;

public class ValidationPipelineBehaviorTests
{
    private readonly Mock<IValidator<CreateUserCommand>> _validator;
    private readonly Mock<RequestHandlerDelegate<Result<Guid>>> _next;
    private readonly ValidationPipelineBehavior<CreateUserCommand, Result<Guid>> _behavior;

    public ValidationPipelineBehaviorTests()
    {
        _validator = new Mock<IValidator<CreateUserCommand>>();
        _next = new Mock<RequestHandlerDelegate<Result<Guid>>>();
        _behavior = new ValidationPipelineBehavior<CreateUserCommand, Result<Guid>>(new[] { _validator.Object });
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidUserCommand();
        var expectedResult = Result<Guid>.Success(Guid.NewGuid());

        var validationResult = new FluentValidation.Results.ValidationResult();
        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _next.Setup(x => x())
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _behavior.Handle(request, _next.Object, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        _validator.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()), Times.Once);
        _next.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidationErrors_ShouldReturnFailureWithoutCallingNext()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidUserCommand();

        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Email is required"),
            new("FirstName", "First name is required")
        };

        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _behavior.Handle(request, _next.Object, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Email is required");
        result.Errors.Should().Contain("First name is required");

        _validator.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()), Times.Once);
        _next.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldValidateAll()
    {
        // Arrange
        var secondValidator = new Mock<IValidator<CreateUserCommand>>();
        var behaviorWithTwoValidators = new ValidationPipelineBehavior<CreateUserCommand, Result<Guid>>(
            new[] { _validator.Object, secondValidator.Object });

        var request = TestDataBuilder.CreateValidUserCommand();

        var validationResult1 = new FluentValidation.Results.ValidationResult();
        var validationResult2 = new FluentValidation.Results.ValidationResult();

        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult1);
        secondValidator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult2);

        _next.Setup(x => x())
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        // Act
        await behaviorWithTwoValidators.Handle(request, _next.Object, CancellationToken.None);

        // Assert
        _validator.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()), Times.Once);
        secondValidator.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()), Times.Once);
        _next.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleValidatorsHavingErrors_ShouldCombineAllErrors()
    {
        // Arrange
        var secondValidator = new Mock<IValidator<CreateUserCommand>>();
        var behaviorWithTwoValidators = new ValidationPipelineBehavior<CreateUserCommand, Result<Guid>>(
            new[] { _validator.Object, secondValidator.Object });

        var request = TestDataBuilder.CreateValidUserCommand();

        var validationFailures1 = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Email is required")
        };

        var validationFailures2 = new List<FluentValidation.Results.ValidationFailure>
        {
            new("FirstName", "First name is required")
        };

        var validationResult1 = new FluentValidation.Results.ValidationResult(validationFailures1);
        var validationResult2 = new FluentValidation.Results.ValidationResult(validationFailures2);

        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult1);
        secondValidator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult2);

        // Act
        var result = await behaviorWithTwoValidators.Handle(request, _next.Object, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Email is required");
        result.Errors.Should().Contain("First name is required");
        _next.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var behaviorWithNoValidators = new ValidationPipelineBehavior<CreateUserCommand, Result<Guid>>(
            Array.Empty<IValidator<CreateUserCommand>>());

        var request = TestDataBuilder.CreateValidUserCommand();
        var expectedResult = Result<Guid>.Success(Guid.NewGuid());

        _next.Setup(x => x())
            .ReturnsAsync(expectedResult);

        // Act
        var result = await behaviorWithNoValidators.Handle(request, _next.Object, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        _next.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidatorThrows_ShouldPropagateException()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidUserCommand();

        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Validator error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, _next.Object, CancellationToken.None));

        _next.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancellationRequested_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidUserCommand();
        var cancellationToken = new CancellationToken(true);

        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .Returns((ValidationContext<CreateUserCommand> context, CancellationToken ct) => {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new FluentValidation.Results.ValidationResult());
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _behavior.Handle(request, _next.Object, cancellationToken));
    }

    [Fact]
    public async Task Handle_WithMixedValidationResults_ShouldOnlyReturnErrors()
    {
        // Arrange
        var secondValidator = new Mock<IValidator<CreateUserCommand>>();
        var behaviorWithTwoValidators = new ValidationPipelineBehavior<CreateUserCommand, Result<Guid>>(
            new[] { _validator.Object, secondValidator.Object });

        var request = TestDataBuilder.CreateValidUserCommand();

        // First validator has no errors
        var validationResult1 = new FluentValidation.Results.ValidationResult();

        // Second validator has errors
        var validationFailures2 = new List<FluentValidation.Results.ValidationFailure>
        {
            new("FirstName", "First name is required")
        };
        var validationResult2 = new FluentValidation.Results.ValidationResult(validationFailures2);

        _validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult1);
        secondValidator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateUserCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult2);

        // Act
        var result = await behaviorWithTwoValidators.Handle(request, _next.Object, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("First name is required");
        result.Errors.Should().HaveCount(1);
        _next.Verify(x => x(), Times.Never);
    }
}