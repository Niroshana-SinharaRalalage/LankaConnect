using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

/// <summary>
/// Tests for Email value object from Users domain (used by Communications)
/// Note: Email is in Users.ValueObjects but used extensively in Communications
/// </summary>
public class EmailComprehensiveTests
{
    #region Creation Tests

    [Fact]
    public void Create_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEmail_ShouldReturnFailure(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email is required", result.Errors);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user.domain.com")]
    [InlineData("user@domain")]
    [InlineData("user@domain.")]
    public void Create_WithInvalidEmailFormat_ShouldReturnFailure(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid email format", result.Errors);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    [InlineData("test123@test-domain.com")]
    [InlineData("user_name@example-domain.net")]
    public void Create_WithValidEmailFormats_ShouldReturnSuccess(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email.ToLowerInvariant(), result.Value.Value);
    }

    #endregion

    #region Value Object Behavior Tests

    [Fact]
    public void Equality_WithSameEmail_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("test@example.com").Value;

        // Act & Assert
        Assert.Equal(email1, email2);
        Assert.True(email1.Equals(email2));
        Assert.True(email1 == email2);
        Assert.False(email1 != email2);
        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentEmails_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com").Value;
        var email2 = Email.Create("test2@example.com").Value;

        // Act & Assert
        Assert.NotEqual(email1, email2);
        Assert.False(email1.Equals(email2));
    }

    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue).Value;

        // Act
        var result = email.ToString();

        // Assert
        Assert.Equal(emailValue, result);
    }

    #endregion

    #region Case Normalization Tests

    [Fact]
    public void Create_ShouldNormalizeEmailToLowerCase()
    {
        // Arrange
        var mixedCaseEmail = "Test.User@EXAMPLE.COM";

        // Act
        var result = Email.Create(mixedCaseEmail);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test.user@example.com", result.Value.Value);
    }

    [Fact]
    public void Equality_ShouldBeCaseInsensitive()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("TEST@EXAMPLE.COM").Value;

        // Act & Assert
        Assert.Equal(email1, email2);
    }

    #endregion

    #region Whitespace Handling Tests

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var emailWithWhitespace = "  test@example.com  ";

        // Act
        var result = Email.Create(emailWithWhitespace);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test@example.com", result.Value.Value);
    }

    #endregion

    #region GetEqualityComponents Tests

    [Fact]
    public void GetEqualityComponents_ShouldReturnEmailValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue).Value;

        // Act
        var components = email.GetEqualityComponents().ToList();

        // Assert
        Assert.Single(components);
        Assert.Equal(emailValue, components[0]);
    }

    #endregion

    #region Business Context Tests

    [Theory]
    [InlineData("support@lankaconnect.com")]
    [InlineData("noreply@lankaconnect.com")]
    [InlineData("admin@lankaconnect.com")]
    [InlineData("notifications@lankaconnect.com")]
    public void Create_WithSystemEmails_ShouldReturnSuccess(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Value);
    }

    [Theory]
    [InlineData("user@sri-lanka-domain.lk")]
    [InlineData("business@colombo.lk")]
    [InlineData("contact@kandy.lk")]
    public void Create_WithSriLankanDomains_ShouldReturnSuccess(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Value);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithMaxReasonableLength_ShouldReturnSuccess()
    {
        // Arrange - Create a valid email at reasonable maximum length
        var localPart = new string('a', 60); // Reasonable local part length
        var domain = "example.com";
        var email = $"{localPart}@{domain}";

        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithComplexValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var complexEmail = "user.name+tag123@sub-domain.example-site.co.uk";

        // Act
        var result = Email.Create(complexEmail);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(complexEmail.ToLowerInvariant(), result.Value.Value);
    }

    #endregion
}