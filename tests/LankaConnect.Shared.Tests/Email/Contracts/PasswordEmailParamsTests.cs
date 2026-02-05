using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.97: Verification tests for password email params.
/// These tests verify that the Year parameter is properly included in ToDictionary()
/// for footer rendering in password-related email templates.
/// </summary>
public class PasswordEmailParamsTests
{
    #region PasswordResetEmailParams Tests

    [Fact]
    public void PasswordResetEmailParams_ToDictionary_ShouldIncludeYear()
    {
        // Arrange
        var emailParams = PasswordResetEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            resetToken: "abc123token",
            resetLink: "https://lankaconnect.com/reset?token=abc123token",
            expiresAt: DateTime.UtcNow.AddHours(24)
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("Year");
        dict["Year"].Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public void PasswordResetEmailParams_ShouldImplementIEmailParameters()
    {
        // Arrange & Act
        var emailParams = new PasswordResetEmailParams();

        // Assert
        emailParams.Should().BeAssignableTo<IEmailParameters>();
    }

    [Fact]
    public void PasswordResetEmailParams_TemplateName_ShouldBeCorrect()
    {
        // Arrange & Act
        var emailParams = new PasswordResetEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be("template-password-reset");
    }

    [Fact]
    public void PasswordResetEmailParams_ToDictionary_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var emailParams = PasswordResetEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            resetToken: "abc123token",
            resetLink: "https://lankaconnect.com/reset?token=abc123token",
            expiresAt: DateTime.UtcNow.AddHours(24)
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("UserEmail");
        dict.Should().ContainKey("ResetToken");
        dict.Should().ContainKey("ResetLink");
        dict.Should().ContainKey("ExpiresAt");
        dict.Should().ContainKey("CompanyName");
        dict.Should().ContainKey("SupportEmail");
        dict.Should().ContainKey("Year");
    }

    [Fact]
    public void PasswordResetEmailParams_Validate_ShouldPassWithValidParams()
    {
        // Arrange
        var emailParams = PasswordResetEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            resetToken: "abc123token",
            resetLink: "https://lankaconnect.com/reset?token=abc123token",
            expiresAt: DateTime.UtcNow.AddHours(24)
        );

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region PasswordChangedEmailParams Tests

    [Fact]
    public void PasswordChangedEmailParams_ToDictionary_ShouldIncludeYear()
    {
        // Arrange
        var emailParams = PasswordChangedEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            changedAt: DateTime.UtcNow
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("Year");
        dict["Year"].Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public void PasswordChangedEmailParams_ShouldImplementIEmailParameters()
    {
        // Arrange & Act
        var emailParams = new PasswordChangedEmailParams();

        // Assert
        emailParams.Should().BeAssignableTo<IEmailParameters>();
    }

    [Fact]
    public void PasswordChangedEmailParams_TemplateName_ShouldBeCorrect()
    {
        // Arrange & Act
        var emailParams = new PasswordChangedEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be("template-password-change-confirmation");
    }

    [Fact]
    public void PasswordChangedEmailParams_ToDictionary_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var emailParams = PasswordChangedEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            changedAt: DateTime.UtcNow
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("UserEmail");
        dict.Should().ContainKey("ChangedAt");
        dict.Should().ContainKey("CompanyName");
        dict.Should().ContainKey("SupportEmail");
        dict.Should().ContainKey("LoginUrl");
        dict.Should().ContainKey("Year");
    }

    [Fact]
    public void PasswordChangedEmailParams_Validate_ShouldPassWithValidParams()
    {
        // Arrange
        var emailParams = PasswordChangedEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            changedAt: DateTime.UtcNow
        );

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion
}
