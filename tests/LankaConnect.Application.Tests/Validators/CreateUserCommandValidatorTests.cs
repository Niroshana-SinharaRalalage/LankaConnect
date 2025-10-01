using FluentValidation.TestHelper;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Commands.CreateUser;

namespace LankaConnect.Application.Tests.Validators;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidEmail_ShouldHaveEmailError(string email)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { Email = email };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test")]
    [InlineData("test@")]
    [InlineData("@test.com")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveEmailFormatError(string email)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { Email = email };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email format is invalid");
    }

    [Fact]
    public void Validate_WithEmailTooLong_ShouldHaveEmailLengthError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // Over 255 chars
        var command = TestDataBuilder.CreateValidUserCommand() with { Email = longEmail };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 255 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidFirstName_ShouldHaveFirstNameError(string firstName)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { FirstName = firstName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name is required");
    }

    [Fact]
    public void Validate_WithFirstNameTooLong_ShouldHaveFirstNameLengthError()
    {
        // Arrange
        var longFirstName = new string('a', 101); // Over 100 chars
        var command = TestDataBuilder.CreateValidUserCommand() with { FirstName = longFirstName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidLastName_ShouldHaveLastNameError(string lastName)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { LastName = lastName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name is required");
    }

    [Fact]
    public void Validate_WithLastNameTooLong_ShouldHaveLastNameLengthError()
    {
        // Arrange
        var longLastName = new string('a', 101); // Over 100 chars
        var command = TestDataBuilder.CreateValidUserCommand() with { LastName = longLastName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("invalid-phone")]
    [InlineData("++94771234567")]
    [InlineData("94771234567")] // Missing +
    public void Validate_WithInvalidPhoneNumber_ShouldHavePhoneNumberError(string phoneNumber)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { PhoneNumber = phoneNumber };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number format is invalid");
    }

    [Fact]
    public void Validate_WithValidPhoneNumber_ShouldNotHavePhoneNumberError()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { PhoneNumber = "+1-555-123-4567" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithNullPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { PhoneNumber = null };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithBioTooLong_ShouldHaveBioLengthError()
    {
        // Arrange
        var longBio = new string('a', 1001); // Over 1000 chars
        var command = TestDataBuilder.CreateValidUserCommand() with { Bio = longBio };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Bio)
            .WithErrorMessage("Bio cannot exceed 1000 characters");
    }

    [Fact]
    public void Validate_WithNullBio_ShouldNotHaveError()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { Bio = null };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Bio);
    }

    [Fact]
    public void Validate_WithEmptyBio_ShouldNotHaveError()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { Bio = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Bio);
    }

    [Fact]
    public void Validate_WithValidBio_ShouldNotHaveError()
    {
        // Arrange
        var validBio = new string('a', 500); // Within limit
        var command = TestDataBuilder.CreateValidUserCommand() with { Bio = validBio };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Bio);
    }

    [Theory]
    [InlineData("john")]
    [InlineData("JOHN")]
    [InlineData("John123")]
    [InlineData("John-Paul")]
    [InlineData("John O'Connor")]
    public void Validate_WithValidFirstNames_ShouldNotHaveError(string firstName)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { FirstName = firstName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user+tag@domain.co.uk")]
    [InlineData("valid.email@subdomain.example.org")]
    public void Validate_WithValidEmails_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUserCommand() with { Email = email };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}