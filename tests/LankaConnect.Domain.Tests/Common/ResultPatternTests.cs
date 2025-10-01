using System;
using FluentAssertions;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Domain.Tests.Common;

/// <summary>
/// TDD RED Phase: Result Pattern Tests
/// Testing foundational Result<T> pattern for error handling and validation
/// </summary>
public class ResultPatternTests
{
    #region Result<T> Tests (RED Phase)

    [Fact]
    public void Result_Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var value = "Success Value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Operation failed";

        // Act
        var result = Result<string>.Failure(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_AccessValue_WhenFailure_ShouldThrowException()
    {
        // Arrange
        var result = Result<string>.Failure("Error occurred");

        // Act & Assert
        var action = () => result.Value;
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access value of a failed result*");
    }

    [Fact]
    public void Result_Match_ShouldExecuteCorrectFunction()
    {
        // Arrange
        var successResult = Result<int>.Success(42);
        var failureResult = Result<int>.Failure("Error");

        // Act
        var successOutput = successResult.Match(
            value => $"Success: {value}",
            error => $"Error: {error}");

        var failureOutput = failureResult.Match(
            value => $"Success: {value}",
            error => $"Error: {error}");

        // Assert
        successOutput.Should().Be("Success: 42");
        failureOutput.Should().Be("Error: Error");
    }

    [Fact]
    public void Result_ImplicitConversion_ShouldWork()
    {
        // Act
        Result<string> successResult = "Success Value";
        Result<string> failureResult = Result<string>.Failure("Failure Message");

        // Assert
        successResult.IsSuccess.Should().BeTrue();
        successResult.Value.Should().Be("Success Value");
        
        failureResult.IsFailure.Should().BeTrue();
        failureResult.Error.Should().Be("Failure Message");
    }

    #endregion

    #region ValidationResult Tests (RED Phase)

    [Fact]
    public void ValidationResult_Valid_ShouldCreateValidResult()
    {
        // Act
        var result = ValidationResult.Valid();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.IsInvalid.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Invalid_ShouldCreateInvalidResult()
    {
        // Arrange
        var errors = new[] { "Required field missing", "Invalid format" };

        // Act
        var result = ValidationResult.Invalid(errors);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.IsInvalid.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Required field missing");
        result.Errors.Should().Contain("Invalid format");
    }

    [Fact]
    public void ValidationResult_Combine_ShouldMergeResults()
    {
        // Arrange
        var result1 = ValidationResult.Invalid(new[] { "Error 1" });
        var result2 = ValidationResult.Invalid(new[] { "Error 2" });
        var validResult = ValidationResult.Valid();

        // Act
        var combined = ValidationResult.Combine(result1, result2, validResult);

        // Assert
        combined.IsInvalid.Should().BeTrue();
        combined.Errors.Should().HaveCount(2);
        combined.Errors.Should().Contain("Error 1");
        combined.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void ValidationResult_CombineAll_WhenAllValid_ShouldReturnValid()
    {
        // Arrange
        var validResults = new[]
        {
            ValidationResult.Valid(),
            ValidationResult.Valid(),
            ValidationResult.Valid()
        };

        // Act
        var combined = ValidationResult.Combine(validResults);

        // Assert
        combined.IsValid.Should().BeTrue();
        combined.Errors.Should().BeEmpty();
    }

    #endregion

    #region Error Tests (RED Phase)

    [Fact]
    public void Error_Create_ShouldCreateErrorWithMessage()
    {
        // Arrange
        var message = "Something went wrong";

        // Act
        var error = new Error(message);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().Be(message);
        error.ToString().Should().Be(message);
    }

    [Fact]
    public void Error_ImplicitStringConversion_ShouldWork()
    {
        // Arrange
        var message = "Error message";
        
        // Act
        Error error = message;
        string converted = error;

        // Assert
        error.Message.Should().Be(message);
        converted.Should().Be(message);
    }

    [Fact]
    public void Error_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var error1 = new Error("Same message");
        var error2 = new Error("Same message");
        var error3 = new Error("Different message");

        // Act & Assert
        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
        (error1 == error2).Should().BeTrue();
        (error1 == error3).Should().BeFalse();
    }

    #endregion

    #region AggregateRoot Tests (RED Phase)

    [Fact]
    public void AggregateRoot_Create_ShouldHaveUniqueId()
    {
        // Act
        var aggregate1 = new TestAggregateRoot();
        var aggregate2 = new TestAggregateRoot();

        // Assert
        aggregate1.Id.Should().NotBe(Guid.Empty);
        aggregate2.Id.Should().NotBe(Guid.Empty);
        aggregate1.Id.Should().NotBe(aggregate2.Id);
    }

    [Fact]
    public void AggregateRoot_CreatedAt_ShouldBeSetToCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var aggregate = new TestAggregateRoot();
        var afterCreation = DateTime.UtcNow;

        // Assert
        aggregate.CreatedAt.Should().BeAfter(beforeCreation.AddMilliseconds(-100));
        aggregate.CreatedAt.Should().BeBefore(afterCreation.AddMilliseconds(100));
    }

    [Fact]
    public void AggregateRoot_Validate_ShouldReturnValidationResult()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();

        // Act
        var result = aggregate.Validate();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    #endregion
}

/// <summary>
/// Test implementation of AggregateRoot for testing purposes
/// </summary>
public class TestAggregateRoot : AggregateRoot
{
    public TestAggregateRoot() : base() { }

    public override ValidationResult Validate()
    {
        return ValidationResult.Valid();
    }
}