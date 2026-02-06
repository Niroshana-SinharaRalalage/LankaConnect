using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.97: Tests for EmailTemplateContract - Single Source of Truth.
///
/// These tests verify that:
/// 1. Template names are correctly defined
/// 2. Parameter names are consistently used across TypedEmailParams classes
/// 3. TypedEmailParams.ToDictionary() uses the contract constants
/// </summary>
public class EmailTemplateContractTests
{
    #region Template Name Tests

    [Fact]
    public void TemplateNames_RefundRequested_ShouldMatchRefundEmailParams()
    {
        // Arrange
        var emailParams = new RefundEmailParams().AsRequest();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundRequested);
    }

    [Fact]
    public void TemplateNames_RefundCompleted_ShouldMatchRefundEmailParams()
    {
        // Arrange
        var emailParams = new RefundEmailParams().AsCompleted();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundCompleted);
    }

    [Fact]
    public void TemplateNames_EventRegistrationCancellation_ShouldMatchRegistrationCancellationEmailParams()
    {
        // Arrange
        var emailParams = new RegistrationCancellationEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.EventRegistrationCancellation);
    }

    [Fact]
    public void TemplateNames_PasswordReset_ShouldMatchPasswordResetEmailParams()
    {
        // Arrange
        var emailParams = new PasswordResetEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.PasswordReset);
    }

    [Fact]
    public void TemplateNames_PasswordChangeConfirmation_ShouldMatchPasswordChangedEmailParams()
    {
        // Arrange
        var emailParams = new PasswordChangedEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.PasswordChangeConfirmation);
    }

    #endregion

    #region Common Parameters Tests

    [Fact]
    public void RefundEmailParams_ToDictionary_ShouldIncludeCommonParameters()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateRequest(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            refundReason: "User cancelled",
            requestedAt: DateTime.UtcNow
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Common.UserName);
        dict.Should().ContainKey(EmailTemplateContract.Common.Year);
        dict.Should().ContainKey(EmailTemplateContract.Common.SupportEmail);
    }

    [Fact]
    public void RegistrationCancellationEmailParams_ToDictionary_ShouldIncludeCommonParameters()
    {
        // Arrange
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.Empty,
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Common.UserName);
        dict.Should().ContainKey(EmailTemplateContract.Common.Year);
        dict.Should().ContainKey(EmailTemplateContract.Common.SupportEmail);
    }

    #endregion

    #region Event Parameters Tests

    [Fact]
    public void RefundEmailParams_ToDictionary_ShouldIncludeEventParameters()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateRequest(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            refundReason: "User cancelled",
            requestedAt: DateTime.UtcNow
        );
        emailParams.EventDetailsUrl = "https://lankaconnect.com/events/123";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Event.EventTitle);
        dict.Should().ContainKey(EmailTemplateContract.Event.EventDateTime);
        dict.Should().ContainKey(EmailTemplateContract.Event.EventDetailsUrl);
    }

    #endregion

    #region Organizer Contact Parameters Tests

    [Fact]
    public void RefundEmailParams_WithOrganizerContact_ToDictionary_ShouldIncludeOrganizerParams()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateRequest(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            refundReason: "User cancelled",
            requestedAt: DateTime.UtcNow
        );
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "555-1234");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.HasOrganizerContact);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactName);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactEmail);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactPhone);

        dict[EmailTemplateContract.OrganizerContact.HasOrganizerContact].Should().Be(true);
    }

    [Fact]
    public void RegistrationCancellationEmailParams_WithOrganizerContact_ToDictionary_ShouldIncludeOrganizerParams()
    {
        // Arrange
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.Empty,
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "555-1234");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.HasOrganizerContact);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactName);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactEmail);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactPhone);

        dict[EmailTemplateContract.OrganizerContact.HasOrganizerContact].Should().Be(true);
    }

    #endregion

    #region Refund Parameters Tests

    [Fact]
    public void RefundEmailParams_ToDictionary_ShouldIncludeRefundParameters()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateCompleted(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            completedAt: DateTime.UtcNow,
            stripeRefundId: "re_abc123"
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Refund.RefundAmount);
        dict.Should().ContainKey(EmailTemplateContract.Refund.OriginalAmount);
        dict.Should().ContainKey(EmailTemplateContract.Refund.StripeRefundId);
        dict.Should().ContainKey(EmailTemplateContract.Refund.ReferenceId);
        dict.Should().ContainKey(EmailTemplateContract.Refund.ProcessingMethod);
    }

    #endregion

    #region Auth Parameters Tests

    [Fact]
    public void PasswordResetEmailParams_ToDictionary_ShouldIncludeAuthParameters()
    {
        // Arrange
        var emailParams = PasswordResetEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            resetToken: "abc123token",
            resetLink: "https://lankaconnect.com/reset?token=abc123",
            expiresAt: DateTime.UtcNow.AddHours(24)
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Auth.UserEmail);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ResetToken);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ResetLink);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ExpiresAt);
    }

    [Fact]
    public void PasswordChangedEmailParams_ToDictionary_ShouldIncludeAuthParameters()
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

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Auth.UserEmail);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ChangedAt);
        dict.Should().ContainKey(EmailTemplateContract.Auth.LoginUrl);
    }

    #endregion
}
