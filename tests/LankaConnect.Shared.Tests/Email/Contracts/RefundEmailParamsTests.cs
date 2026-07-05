using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.97: Tests for RefundEmailParams (TDD)
///
/// Issue #2 Fix: Add EventDetailsUrl property for "View Event Details" button
/// in refund email templates.
/// </summary>
public class RefundEmailParamsTests
{
    #region IEmailParameters Contract Tests

    [Fact]
    public void RefundEmailParams_ShouldImplementIEmailParameters()
    {
        // Arrange & Act
        var emailParams = CreateValidRequestParams();

        // Assert
        emailParams.Should().BeAssignableTo<IEmailParameters>();
    }

    [Fact]
    public void RefundEmailParams_AsRequest_ShouldSetTemplateToRefundRequested()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();

        // Act
        emailParams.AsRequest();

        // Assert
        emailParams.TemplateName.Should().Be("template-refund-requested");
    }

    [Fact]
    public void RefundEmailParams_AsCompleted_ShouldSetTemplateToRefundCompleted()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();

        // Act
        emailParams.AsCompleted();

        // Assert
        emailParams.TemplateName.Should().Be("template-refund-completed");
    }

    #endregion

    #region Phase 6A.97 Fix: EventDetailsUrl Property Tests

    [Fact]
    public void RefundEmailParams_ShouldHaveEventDetailsUrlProperty()
    {
        // Arrange
        // Phase 6A.97: EventDetailsUrl is needed for "View Event Details" button
        var emailParams = new RefundEmailParams();

        // Act
        emailParams.EventDetailsUrl = "https://lankaconnect.com/events/123";

        // Assert
        emailParams.EventDetailsUrl.Should().Be("https://lankaconnect.com/events/123");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeEventDetailsUrl()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();
        emailParams.EventDetailsUrl = "https://lankaconnect.com/events/abc-123";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("EventDetailsUrl");
        dict["EventDetailsUrl"].Should().Be("https://lankaconnect.com/events/abc-123");
    }

    [Fact]
    public void CreateRequest_ShouldSetDefaultEventDetailsUrlToEmpty()
    {
        // Arrange & Act
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

        // Assert
        emailParams.EventDetailsUrl.Should().BeEmpty();
    }

    [Fact]
    public void CreateCompleted_ShouldSetDefaultEventDetailsUrlToEmpty()
    {
        // Arrange & Act
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

        // Assert
        emailParams.EventDetailsUrl.Should().BeEmpty();
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventStartDate");
        dict.Should().ContainKey("EventStartTime");
        dict.Should().ContainKey("EventDateTime");
        dict.Should().ContainKey("RefundAmount");
        dict.Should().ContainKey("OriginalAmount");
        dict.Should().ContainKey("RefundReason");
        dict.Should().ContainKey("RequestedAt");
        dict.Should().ContainKey("SupportEmail");
        dict.Should().ContainKey("Year");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeOrganizerContactParams()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "555-1234");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasOrganizerContact");
        dict.Should().ContainKey("OrganizerContactName");
        dict.Should().ContainKey("OrganizerContactEmail");
        dict.Should().ContainKey("OrganizerContactPhone");
        dict["HasOrganizerContact"].Should().Be(true);
        dict["OrganizerContactName"].Should().Be("Jane Smith");
        dict["OrganizerContactEmail"].Should().Be("jane@example.com");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeStripeRefundId()
    {
        // Arrange
        var emailParams = CreateValidCompletedParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("StripeRefundId");
        dict["StripeRefundId"].Should().Be("re_test123");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeReferenceId()
    {
        // Arrange
        var emailParams = CreateValidCompletedParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("ReferenceId");
        // ReferenceId should be StripeRefundId when available
        dict["ReferenceId"].Should().Be("re_test123");
    }

    [Fact]
    public void ToDictionary_RefundAmount_ShouldBeFormattedWithoutDollarSign()
    {
        // Arrange - Phase 6A.87++ Fix: No $ symbol since templates have it hardcoded
        var emailParams = CreateValidRequestParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict["RefundAmount"].ToString().Should().NotStartWith("$");
        dict["RefundAmount"].Should().Be("50.00");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_ShouldPassWithValidParams()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFailWhenUserIdIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();
        emailParams.UserId = Guid.Empty;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserId is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenUserNameIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();
        emailParams.UserName = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserName is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenRefundAmountIsZero()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();
        emailParams.RefundAmount = 0;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("RefundAmount must be greater than 0");
    }

    #endregion

    #region Fluent Setter Tests

    [Fact]
    public void WithOrganizerContact_ShouldSetHasOrganizerContactTrue()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();

        // Act
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com");

        // Assert
        emailParams.HasOrganizerContact.Should().BeTrue();
        emailParams.OrganizerContactName.Should().Be("Jane Smith");
        emailParams.OrganizerContactEmail.Should().Be("jane@example.com");
    }

    [Fact]
    public void WithOrganizerContact_WithNullName_ShouldSetHasOrganizerContactFalse()
    {
        // Arrange
        var emailParams = CreateValidRequestParams();

        // Act
        emailParams.WithOrganizerContact(null, "jane@example.com");

        // Assert
        emailParams.HasOrganizerContact.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static RefundEmailParams CreateValidRequestParams()
    {
        return RefundEmailParams.CreateRequest(
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
    }

    private static RefundEmailParams CreateValidCompletedParams()
    {
        return RefundEmailParams.CreateCompleted(
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
            stripeRefundId: "re_test123"
        );
    }

    #endregion
}
