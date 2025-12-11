using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

/// <summary>
/// Unit tests for RegistrationContact value object
/// Shared contact information for all attendees in a registration
/// Following TDD Red-Green-Refactor cycle
/// </summary>
public class RegistrationContactTests
{
    [Fact]
    public void Create_WithAllValidFields_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var phoneNumber = "+1-555-123-4567";
        var address = "123 Main St, City, State 12345";

        // Act
        var result = RegistrationContact.Create(email, phoneNumber, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(email);
        result.Value.PhoneNumber.Should().Be(phoneNumber);
        result.Value.Address.Should().Be(address);
    }

    [Fact]
    public void Create_WithoutAddress_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var phoneNumber = "+1-555-123-4567";

        // Act
        var result = RegistrationContact.Create(email, phoneNumber, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(email);
        result.Value.PhoneNumber.Should().Be(phoneNumber);
        result.Value.Address.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Act
        var result = RegistrationContact.Create(invalidEmail, "+1-555-123-4567", "123 Main St");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email is required");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test @example.com")]
    public void Create_WithInvalidEmailFormat_ShouldFail(string invalidEmail)
    {
        // Act
        var result = RegistrationContact.Create(invalidEmail, "+1-555-123-4567", "123 Main St");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.com")]
    [InlineData("user_name@test-domain.com")]
    public void Create_WithValidEmailFormats_ShouldSucceed(string validEmail)
    {
        // Act
        var result = RegistrationContact.Create(validEmail, "+1-555-123-4567", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(validEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPhoneNumber_ShouldFail(string invalidPhone)
    {
        // Act
        var result = RegistrationContact.Create("test@example.com", invalidPhone, "123 Main St");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Phone number is required");
    }

    [Theory]
    [InlineData("+1-555-123-4567")]
    [InlineData("555-123-4567")]
    [InlineData("(555) 123-4567")]
    [InlineData("+44 20 1234 5678")]
    [InlineData("5551234567")]
    [InlineData("+1 (555) 123-4567")]
    public void Create_WithValidPhoneFormats_ShouldSucceed(string validPhone)
    {
        // Act
        var result = RegistrationContact.Create("test@example.com", validPhone, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhoneNumber.Should().Be(validPhone);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromAllFields()
    {
        // Arrange
        var email = "  test@example.com  ";
        var phone = "  +1-555-123-4567  ";
        var address = "  123 Main St  ";

        // Act
        var result = RegistrationContact.Create(email, phone, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.PhoneNumber.Should().Be("+1-555-123-4567");
        result.Value.Address.Should().Be("123 Main St");
    }

    [Fact]
    public void Create_WithEmptyAddressAfterTrim_ShouldStoreAsNull()
    {
        // Arrange
        var address = "   "; // Only whitespace

        // Act
        var result = RegistrationContact.Create("test@example.com", "+1-555-123-4567", address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().BeNull();
    }

    [Fact]
    public void ValueObjectEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var contact1 = RegistrationContact.Create("test@example.com", "+1-555-123-4567", "123 Main St").Value;
        var contact2 = RegistrationContact.Create("test@example.com", "+1-555-123-4567", "123 Main St").Value;

        // Act & Assert
        contact1.Should().Be(contact2);
        (contact1 == contact2).Should().BeTrue();
        (contact1 != contact2).Should().BeFalse();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentEmails_ShouldNotBeEqual()
    {
        // Arrange
        var contact1 = RegistrationContact.Create("test1@example.com", "+1-555-123-4567", "123 Main St").Value;
        var contact2 = RegistrationContact.Create("test2@example.com", "+1-555-123-4567", "123 Main St").Value;

        // Act & Assert
        contact1.Should().NotBe(contact2);
    }

    [Fact]
    public void ValueObjectEquality_WithNullAndEmptyAddress_ShouldBeEqual()
    {
        // Arrange
        var contact1 = RegistrationContact.Create("test@example.com", "+1-555-123-4567", null).Value;
        var contact2 = RegistrationContact.Create("test@example.com", "+1-555-123-4567", "   ").Value;

        // Act & Assert (both should have null address)
        contact1.Should().Be(contact2);
    }

    [Fact]
    public void ToString_WithAllFields_ShouldReturnFormattedString()
    {
        // Arrange
        var contact = RegistrationContact.Create("test@example.com", "+1-555-123-4567", "123 Main St").Value;

        // Act
        var result = contact.ToString();

        // Assert
        result.Should().Contain("test@example.com");
        result.Should().Contain("+1-555-123-4567");
        result.Should().Contain("123 Main St");
    }

    [Fact]
    public void ToString_WithoutAddress_ShouldReturnFormattedStringWithoutAddress()
    {
        // Arrange
        var contact = RegistrationContact.Create("test@example.com", "+1-555-123-4567", null).Value;

        // Act
        var result = contact.ToString();

        // Assert
        result.Should().Contain("test@example.com");
        result.Should().Contain("+1-555-123-4567");
        result.Should().NotContain("Address:");
    }
}
