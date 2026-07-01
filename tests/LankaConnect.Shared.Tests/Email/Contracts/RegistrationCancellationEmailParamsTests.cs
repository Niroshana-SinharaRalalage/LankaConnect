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
        // Wave 9.h.10.5 F24: EventStartDate is no longer a required field.
        errors.Should().NotContain("EventStartDate is required");
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
    public void Validate_ShouldAcceptDefaultEventStartDateForTbdEvent()
    {
        // Wave 9.h.10.5 F24: previously this test asserted the OLD behavior
        // (validator rejected default EventStartDate). The domain now supports
        // TBD events (Phase 8YA-2) with a null aggregate StartDate; handler
        // passes `@event.StartDate.GetValueOrDefault()` which becomes default.
        // ToDictionary emits "Date TBD" / "Time TBD" as the template fallback.
        //
        // Renamed + inverted assertion so this test locks the F24 contract in
        // place (default is now valid, not rejected).
        //
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventStartDate = default;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert -- default EventStartDate must be accepted for TBD events
        isValid.Should().BeTrue("TBD events (default(DateTime) EventStartDate) must validate successfully -- ToDictionary renders 'Date TBD' fallback");
        errors.Should().NotContain(e => e.Contains("EventStartDate"), "EventStartDate is no longer a required field after F24");
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

    #region Wave 9.h.10.5 F24 -- TBD-event support (nullable EventStartDate handling)

    /// <summary>
    /// Wave 9.h.10.5 F24 regression test.
    /// Prior to the fix, Validate() hard-rejected `EventStartDate == default`
    /// even though the domain supports TBD events (Phase 8YA-2). The handler
    /// passes `@event.StartDate.GetValueOrDefault()` which becomes
    /// `DateTime.MinValue` (== default(DateTime)) for TBD events. Every
    /// cancellation email for a TBD-event registration was killed silently
    /// at the validator (Pass 1 probe evidence: 1 VALIDATION-FAIL with
    /// errors="EventStartDate is required").
    /// </summary>
    [Fact]
    public void Validate_WithDefaultEventStartDate_ShouldAcceptTbdEvent()
    {
        // Arrange -- simulate a TBD event where StartDate is null on the
        // aggregate. Handler passes GetValueOrDefault() = default(DateTime).
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test TBD Event",
            eventStartDate: default,  // Wave 9.h.10.5 F24: TBD event -> Date TBD fallback
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue("TBD events (default(DateTime) EventStartDate) must be accepted -- domain supports Phase 8YA-2 TBD events");
        errors.Should().NotContain(e => e.Contains("EventStartDate"), "EventStartDate is no longer required after Wave 9.h.10.5 F24");
    }

    [Fact]
    public void ToDictionary_WithDefaultEventStartDate_ShouldEmitTbdFallback()
    {
        // Arrange -- TBD event
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Jane Doe",
            userEmail: "jane@example.com",
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "TBD Event",
            eventStartDate: default,
            timeZoneId: "America/New_York",
            eventLocation: "TBD Location",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert -- template body renders "Date TBD" / "Time TBD" instead of
        // an unreplaced placeholder or an epoch-zero timestamp.
        dict["EventStartDate"].Should().Be("Date TBD");
        dict["EventStartTime"].Should().Be("Time TBD");
        dict["EventDateTime"].ToString().Should().Contain("Date TBD");
    }

    [Fact]
    public void ToDictionary_WithRealEventStartDate_ShouldEmitFormattedDate()
    {
        // Arrange -- real dated event
        var startDate = new DateTime(2027, 3, 15, 19, 0, 0, DateTimeKind.Utc);
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Real User",
            userEmail: "real@example.com",
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Real Event",
            eventStartDate: startDate,
            timeZoneId: "America/New_York",
            eventLocation: "Real Location",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert -- non-default StartDate renders normally (not "TBD")
        dict["EventStartDate"].ToString().Should().NotBe("Date TBD");
        dict["EventStartTime"].ToString().Should().NotBe("Time TBD");
    }

    #endregion
}
