using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 4: Tests for TicketConfirmationEmailParams (TDD - RED phase)
/// Template: template-paid-event-registration-confirmation-with-ticket
/// </summary>
public class TicketConfirmationEmailParamsTests
{
    #region Basic Properties Tests

    [Fact]
    public void TemplateName_ShouldReturnPaidEventRegistrationTemplate()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Assert
        emailParams.TemplateName.Should().Be("template-paid-event-registration-confirmation-with-ticket");
    }

    [Fact]
    public void RecipientEmail_ShouldReturnContactEmail()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.ContactEmail = "john@example.com";

        // Assert
        emailParams.RecipientEmail.Should().Be("john@example.com");
    }

    [Fact]
    public void RecipientName_ShouldReturnUserName()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.UserName = "John Doe";

        // Assert
        emailParams.RecipientName.Should().Be("John Doe");
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_ShouldContainAllRequiredParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - Core event parameters
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventStartDate");
        dict.Should().ContainKey("EventStartTime");
        dict.Should().ContainKey("EventLocation");
        dict.Should().ContainKey("EventDetailsUrl");
    }

    [Fact]
    public void ToDictionary_ShouldContainPaymentParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - Payment parameters
        dict.Should().ContainKey("AmountPaid");
        dict.Should().ContainKey("TotalAmount");
        dict.Should().ContainKey("PaymentIntentId");
        dict.Should().ContainKey("PaymentDate");
        dict.Should().ContainKey("OrderNumber");
    }

    [Fact]
    public void ToDictionary_ShouldContainAttendeeParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AttendeesHtml = "<p>John</p><p>Jane</p>";
        emailParams.HasAttendeeDetails = true;
        emailParams.Quantity = 2;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("Attendees");
        dict.Should().ContainKey("HasAttendeeDetails");
        dict.Should().ContainKey("Quantity");
        dict["Attendees"].Should().Be("<p>John</p><p>Jane</p>");
        dict["HasAttendeeDetails"].Should().Be(true);
        dict["Quantity"].Should().Be(2);
    }

    [Fact]
    public void ToDictionary_ShouldContainTicketParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasTicket = true;
        emailParams.TicketCode = "TKT-123456";
        emailParams.TicketExpiryDate = "January 15, 2026";
        emailParams.TicketUrl = "https://example.com/tickets/123";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasTicket");
        dict.Should().ContainKey("TicketCode");
        dict.Should().ContainKey("TicketExpiryDate");
        dict.Should().ContainKey("TicketUrl");
        dict["HasTicket"].Should().Be(true);
        dict["TicketCode"].Should().Be("TKT-123456");
    }

    [Fact]
    public void ToDictionary_ShouldContainOrganizerContactParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasOrganizerContact = true;
        emailParams.OrganizerContactName = "Jane Organizer";
        emailParams.OrganizerContactEmail = "organizer@example.com";
        emailParams.OrganizerContactPhone = "555-0100";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasOrganizerContact");
        dict.Should().ContainKey("OrganizerContactName");
        dict.Should().ContainKey("OrganizerContactEmail");
        dict.Should().ContainKey("OrganizerContactPhone");
        dict["HasOrganizerContact"].Should().Be(true);
    }

    [Fact]
    public void ToDictionary_ShouldContainContactInfoParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasContactInfo = true;
        emailParams.RegistrantEmail = "registrant@example.com";
        emailParams.RegistrantPhone = "555-0200";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasContactInfo");
        dict.Should().ContainKey("ContactEmail");
        dict.Should().ContainKey("ContactPhone");
        dict["HasContactInfo"].Should().Be(true);
    }

    [Fact]
    public void ToDictionary_ShouldContainEventImageParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasEventImage = true;
        emailParams.EventImageUrl = "https://example.com/image.jpg";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasEventImage");
        dict.Should().ContainKey("EventImageUrl");
        dict["HasEventImage"].Should().Be(true);
        dict["EventImageUrl"].Should().Be("https://example.com/image.jpg");
    }

    // W1.0b SKIP: Timezone-dependent date formatting (CI UTC vs test local-tz assumption).
    // See docs/operations/W1-test-triage.md; W1.0c follow-up.
    [Fact(Skip = "Timezone-dependent; W1.0c follow-up")]
    public void ToDictionary_ShouldFormatDateCorrectly()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventStartDate = new DateTime(2026, 2, 15);

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict["EventStartDate"].Should().Be("February 15, 2026");
    }

    [Fact]
    public void ToDictionary_ShouldFormatAmountWithCurrency()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AmountPaid = 25.50m;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - Should be formatted as USD currency
        dict["AmountPaid"].ToString().Should().Contain("25.50");
        dict["TotalAmount"].ToString().Should().Contain("25.50");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithValidParams_ShouldReturnTrue()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyUserName_ShouldReturnError()
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
    public void Validate_WithEmptyContactEmail_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.ContactEmail = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("ContactEmail is required");
    }

    [Fact]
    public void Validate_WithEmptyEventTitle_ShouldReturnError()
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
    public void Validate_WithEmptyEventDetailsUrl_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventDetailsUrl = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("EventDetailsUrl is required");
    }

    [Fact]
    public void Validate_WithEmptyPaymentIntentId_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.PaymentIntentId = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("PaymentIntentId is required");
    }

    [Fact]
    public void Validate_WithZeroAmount_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AmountPaid = 0;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("AmountPaid must be greater than zero");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var emailParams = new TicketConfirmationEmailParams();

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Validate_WithHasTicketTrue_AndEmptyTicketCode_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasTicket = true;
        emailParams.TicketCode = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("TicketCode is required when HasTicket is true");
    }

    [Fact]
    public void Validate_WithHasOrganizerContactTrue_AndEmptyOrganizerName_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasOrganizerContact = true;
        emailParams.OrganizerContactName = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("OrganizerContactName is required when HasOrganizerContact is true");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Create_ShouldSetRequiredProperties()
    {
        // Act
        var emailParams = TicketConfirmationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "John Doe",
            contactEmail: "john@example.com",
            eventTitle: "Test Event",
            eventStartDate: new DateTime(2026, 2, 15),
            eventStartTime: "10:00 AM",
            eventLocation: "123 Main St, City",
            eventDetailsUrl: "https://example.com/events/123",
            amountPaid: 50.00m,
            paymentIntentId: "pi_123456",
            paymentDate: DateTime.UtcNow,
            quantity: 2);

        // Assert
        emailParams.Should().NotBeNull();
        emailParams.UserName.Should().Be("John Doe");
        emailParams.ContactEmail.Should().Be("john@example.com");
        emailParams.EventTitle.Should().Be("Test Event");
        emailParams.AmountPaid.Should().Be(50.00m);
        emailParams.Quantity.Should().Be(2);
    }

    [Fact]
    public void WithTicket_ShouldSetTicketProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        emailParams.WithTicket("TKT-123", "January 20, 2026", "https://example.com/tickets/123");

        // Assert
        emailParams.HasTicket.Should().BeTrue();
        emailParams.TicketCode.Should().Be("TKT-123");
        emailParams.TicketExpiryDate.Should().Be("January 20, 2026");
        emailParams.TicketUrl.Should().Be("https://example.com/tickets/123");
    }

    [Fact]
    public void WithOrganizerContact_ShouldSetOrganizerProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        emailParams.WithOrganizerContact("Jane Organizer", "jane@example.com", "555-0100");

        // Assert
        emailParams.HasOrganizerContact.Should().BeTrue();
        emailParams.OrganizerContactName.Should().Be("Jane Organizer");
        emailParams.OrganizerContactEmail.Should().Be("jane@example.com");
        emailParams.OrganizerContactPhone.Should().Be("555-0100");
    }

    [Fact]
    public void WithAttendees_ShouldSetAttendeeProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();
        var attendeesHtml = "<p>John</p><p>Jane</p>";

        // Act
        emailParams.WithAttendees(attendeesHtml);

        // Assert
        emailParams.HasAttendeeDetails.Should().BeTrue();
        emailParams.AttendeesHtml.Should().Be(attendeesHtml);
    }

    [Fact]
    public void WithEventImage_ShouldSetImageProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        emailParams.WithEventImage("https://example.com/image.jpg");

        // Assert
        emailParams.HasEventImage.Should().BeTrue();
        emailParams.EventImageUrl.Should().Be("https://example.com/image.jpg");
    }

    [Fact]
    public void WithContactInfo_ShouldSetContactProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        emailParams.WithContactInfo("contact@example.com", "555-0300");

        // Assert
        emailParams.HasContactInfo.Should().BeTrue();
        emailParams.RegistrantEmail.Should().Be("contact@example.com");
        emailParams.RegistrantPhone.Should().Be("555-0300");
    }

    #endregion

    #region Helper Methods

    private static TicketConfirmationEmailParams CreateValidParams()
    {
        return TicketConfirmationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "John Doe",
            contactEmail: "john@example.com",
            eventTitle: "Test Event",
            eventStartDate: new DateTime(2026, 2, 15),
            eventStartTime: "10:00 AM",
            eventLocation: "123 Main St, City",
            eventDetailsUrl: "https://example.com/events/123",
            amountPaid: 50.00m,
            paymentIntentId: "pi_123456",
            paymentDate: DateTime.UtcNow,
            quantity: 2);
    }

    #endregion
}
