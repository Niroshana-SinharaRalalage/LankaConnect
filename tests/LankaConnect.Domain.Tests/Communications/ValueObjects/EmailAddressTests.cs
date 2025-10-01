using LankaConnect.Domain.Users.ValueObjects;
using FluentAssertions;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@gmail.com")]
    [InlineData("a@b.co")]
    public void Create_WithValidEmail_ShouldCreateEmail(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(validEmail.ToLowerInvariant());
        result.Value.ToString().Should().Be(validEmail.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespaceEmail_ShouldReturnFailure(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    [InlineData("test@.com")]
    [InlineData("test@domain.")]
    [InlineData("test@domain..com")]
    public void Create_WithInvalidEmailFormat_ShouldReturnFailure(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Fact]
    public void Create_WithValidEmails_ShouldReturnSuccess()
    {
        // Arrange
        var validEmails = new[]
        {
            "test@example.com",
            "user.name@domain.co.uk", 
            "test+tag@gmail.com",
            "123@456.co"
        };

        // Act & Assert
        validEmails.Should().AllSatisfy(email => 
            Email.Create(email).IsSuccess.Should().BeTrue($"{email} should be valid"));
    }

    [Fact]
    public void Create_WithInvalidEmails_ShouldReturnFailure()
    {
        // Arrange
        var invalidEmails = new[]
        {
            "",
            null,
            "invalid-email",
            "test@",
            "@example.com",
            "test.example.com"
        };

        // Act & Assert
        invalidEmails.Should().AllSatisfy(email => 
            Email.Create(email).IsFailure.Should().BeTrue($"{email} should be invalid"));
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("test@example.com").Value;

        // Act & Assert
        email1.Should().Be(email2);
        email1.GetHashCode().Should().Be(email2.GetHashCode());
        (email1 == email2).Should().BeTrue();
        (email1 != email2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com").Value;
        var email2 = Email.Create("test2@example.com").Value;

        // Act & Assert
        email1.Should().NotBe(email2);
        email1.GetHashCode().Should().NotBe(email2.GetHashCode());
        (email1 == email2).Should().BeFalse();
        (email1 != email2).Should().BeTrue();
    }

    [Fact]
    public void Create_WithCaseDifference_ShouldNormalize()
    {
        // Arrange & Act
        var email1 = Email.Create("Test@Example.COM").Value;
        var email2 = Email.Create("test@example.com").Value;

        // Assert
        email1.Should().Be(email2);
        email1.Value.Should().Be("test@example.com"); // Should be normalized
    }

    [Theory]
    [InlineData("  test@example.com  ", "test@example.com")]
    [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
    [InlineData("Test@Example.Com", "test@example.com")]
    public void Create_ShouldNormalizeEmailAddress(string input, string expected)
    {
        // Act
        var result = Email.Create(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }
}