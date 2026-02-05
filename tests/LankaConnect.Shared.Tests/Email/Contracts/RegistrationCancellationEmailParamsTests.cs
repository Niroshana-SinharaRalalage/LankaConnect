using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.97: Tests for RegistrationCancellationEmailParams (TDD)
///
/// Issue #1 Fix: RegistrationId should be optional in validation because
/// the RegistrationCancelledEvent domain event doesn't include RegistrationId.
/// </summary>
public class RegistrationCancellationEmailParamsTests
{
    private const string ExpectedTemplateName = "template-event-registration-cancellation";

    #region IEmailParameters Contract Tests

    [Fact]
    public void RegistrationCancellationEmailParams_ShouldImplementIEmailParameters()
    {
        // Arrange & Act
        var emailParams = CreateValidParams();

        // Assert
        emailParams.Should().BeAssignableTo<IEmailParameters>();
    }

    [Fact]
    public void RegistrationCancellationEmailParams_TemplateName_ShouldReturnCorrectTemplate()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var templateName = emailParams.TemplateName;

        // Assert
        templateName.Should().Be(ExpectedTemplateName);
    }

    #endregion

    #region Phase 6A.97 Fix: RegistrationId Optional Validation Tests

    [Fact]
    public void Validate_WithEmptyRegistrationId_ShouldPass_BecauseItIsOptional()
    {
        // Arrange
        // Phase 6A.97: RegistrationId should be optional because RegistrationCancelledEvent
        // domain event doesn't include RegistrationId
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.Empty,  // Optional - should not fail validation
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
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue("RegistrationId is optional for cancellation emails since domain event doesn't include it");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidRegistrationId_ShouldPass()
    {
        // Arrange
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),  // Valid registration ID
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
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingRequiredFields_ShouldFail_ButNotIncludeRegistrationId()
    {
        // Arrange - Create params with missing required fields
        var emailParams = new RegistrationCancellationEmailParams
        {
            UserId = Guid.Empty,
            UserName = "",
            UserEmail = "",
            RegistrationId = Guid.Empty,  // Should be optional now
            EventId = Guid.Empty,
            EventTitle = "",
            EventStartDate = default,
            CancelledAt = default
        };

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserId is required");
        errors.Should().Contain("UserName is required");
        errors.Should().Contain("UserEmail is required");
        errors.Should().Contain("EventId is required");
        errors.Should().Contain("EventTitle is required");
        errors.Should().Contain("EventStartDate is required");
        errors.Should().Contain("CancelledAt is required");
        // KEY ASSERTION: RegistrationId should NOT be in errors
        errors.Should().NotContain("RegistrationId is required");
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_ShouldIncludeAllRequiredTemplateParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - All template parameters must be present
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventStartDate");
        dict.Should().ContainKey("EventStartTime");
        dict.Should().ContainKey("EventDateTime");
        dict.Should().ContainKey("EventLocation");
        dict.Should().ContainKey("CancellationReason");
        dict.Should().ContainKey("CancelledAt");
        dict.Should().ContainKey("CancellationDate");
        dict.Should().ContainKey("RefundStatus");
        dict.Should().ContainKey("EventDetailsUrl");
        dict.Should().ContainKey("SupportEmail");
        dict.Should().ContainKey("Year");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeYear()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("Year");
        dict["Year"].Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public void ToDictionary_ShouldIncludeEventDateTime()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - EventDateTime should be combined date + time
        dict.Should().ContainKey("EventDateTime");
        dict["EventDateTime"].ToString().Should().Contain(" at ");
    }

    #endregion

    #region Required Validation Tests

    [Fact]
    public void Validate_ShouldFailWhenUserIdIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
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
        var emailParams = CreateValidParams();
        emailParams.UserName = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserName is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenUserEmailIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.UserEmail = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserEmail is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenEventIdIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventId = Guid.Empty;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("EventId is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenEventTitleIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventTitle = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("EventTitle is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenEventStartDateIsDefault()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventStartDate = default;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("EventStartDate is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenCancelledAtIsDefault()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.CancelledAt = default;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("CancelledAt is required");
    }

    #endregion

    #region Phase 6A.97+ Fix: Organizer Contact Tests

    [Fact]
    public void RegistrationCancellationEmailParams_ShouldHaveOrganizerContactProperties()
    {
        // Arrange & Act
        var emailParams = new RegistrationCancellationEmailParams();

        // Assert - properties should exist and have default values
        emailParams.HasOrganizerContact.Should().BeFalse();
        emailParams.OrganizerContactName.Should().BeEmpty();
        emailParams.OrganizerContactEmail.Should().BeEmpty();
        emailParams.OrganizerContactPhone.Should().BeEmpty();
    }

    [Fact]
    public void WithOrganizerContact_ShouldSetHasOrganizerContactTrue()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "555-1234");

        // Assert
        emailParams.HasOrganizerContact.Should().BeTrue();
        emailParams.OrganizerContactName.Should().Be("Jane Smith");
        emailParams.OrganizerContactEmail.Should().Be("jane@example.com");
        emailParams.OrganizerContactPhone.Should().Be("555-1234");
    }

    [Fact]
    public void WithOrganizerContact_WithNullName_ShouldSetHasOrganizerContactFalse()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        emailParams.WithOrganizerContact(null, "jane@example.com");

        // Assert
        emailParams.HasOrganizerContact.Should().BeFalse();
    }

    [Fact]
    public void ToDictionary_ShouldIncludeOrganizerContactParams()
    {
        // Arrange
        var emailParams = CreateValidParams();
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
        dict["OrganizerContactPhone"].Should().Be("555-1234");
    }

    [Fact]
    public void ToDictionary_WithoutOrganizerContact_ShouldIncludeFalseHasOrganizerContact()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasOrganizerContact");
        dict["HasOrganizerContact"].Should().Be(false);
    }

    #endregion

    #region Helper Methods

    private static RegistrationCancellationEmailParams CreateValidParams()
    {
        return RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );
    }

    #endregion
}
