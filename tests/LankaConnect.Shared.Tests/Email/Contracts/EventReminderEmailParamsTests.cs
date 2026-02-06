using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 2: Tests for EventReminderEmailParams (TDD - RED phase)
/// Template-specific typed parameters for template-event-reminder
/// </summary>
public class EventReminderEmailParamsTests
{
    private const string ExpectedTemplateName = "template-event-reminder";

    #region IEmailParameters Contract Tests

    [Fact]
    public void EventReminderEmailParams_ShouldImplementIEmailParameters()
    {
        // Arrange & Act
        var emailParams = CreateValidParams();

        // Assert
        emailParams.Should().BeAssignableTo<IEmailParameters>();
    }

    [Fact]
    public void EventReminderEmailParams_TemplateName_ShouldReturnCorrectTemplate()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var templateName = emailParams.TemplateName;

        // Assert
        templateName.Should().Be(ExpectedTemplateName);
    }

    [Fact]
    public void EventReminderEmailParams_RecipientEmail_ShouldReturnAttendeeEmail()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AttendeeEmail = "attendee@example.com";

        // Act & Assert
        emailParams.RecipientEmail.Should().Be("attendee@example.com");
    }

    [Fact]
    public void EventReminderEmailParams_RecipientName_ShouldReturnAttendeeName()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AttendeeName = "John Doe";

        // Act & Assert
        emailParams.RecipientName.Should().Be("John Doe");
    }

    #endregion

    #region Required Properties Tests

    [Fact]
    public void EventReminderEmailParams_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var startDate = new DateTime(2026, 2, 15, 10, 0, 0);

        // Act
        var emailParams = new EventReminderEmailParams
        {
            EventId = eventId,
            RegistrationId = registrationId,
            AttendeeName = "John Doe",
            AttendeeEmail = "john@example.com",
            EventTitle = "Community Meetup",
            EventStartDate = startDate,
            EventStartTime = "10:00 AM",
            Location = "123 Main St, Boston, MA",
            Quantity = 2,
            HoursUntilEvent = 24.5,
            ReminderTimeframe = "tomorrow",
            ReminderMessage = "Your event is tomorrow!",
            EventDetailsUrl = "https://lankaconnect.com/events/abc123"
        };

        // Assert
        emailParams.EventId.Should().Be(eventId);
        emailParams.RegistrationId.Should().Be(registrationId);
        emailParams.AttendeeName.Should().Be("John Doe");
        emailParams.AttendeeEmail.Should().Be("john@example.com");
        emailParams.EventTitle.Should().Be("Community Meetup");
        emailParams.EventStartDate.Should().Be(startDate);
        emailParams.EventStartTime.Should().Be("10:00 AM");
        emailParams.Location.Should().Be("123 Main St, Boston, MA");
        emailParams.Quantity.Should().Be(2);
        emailParams.HoursUntilEvent.Should().Be(24.5);
        emailParams.ReminderTimeframe.Should().Be("tomorrow");
        emailParams.ReminderMessage.Should().Be("Your event is tomorrow!");
        emailParams.EventDetailsUrl.Should().Be("https://lankaconnect.com/events/abc123");
    }

    [Fact]
    public void EventReminderEmailParams_ShouldHaveOrganizerContactProperties()
    {
        // Arrange & Act
        var emailParams = CreateValidParams();
        emailParams.HasOrganizerContact = true;
        emailParams.OrganizerContactName = "Jane Smith";
        emailParams.OrganizerContactEmail = "jane@example.com";
        emailParams.OrganizerContactPhone = "555-123-4567";

        // Assert
        emailParams.HasOrganizerContact.Should().BeTrue();
        emailParams.OrganizerContactName.Should().Be("Jane Smith");
        emailParams.OrganizerContactEmail.Should().Be("jane@example.com");
        emailParams.OrganizerContactPhone.Should().Be("555-123-4567");
    }

    [Fact]
    public void EventReminderEmailParams_ShouldHaveTicketProperties()
    {
        // Arrange & Act
        var emailParams = CreateValidParams();
        emailParams.HasTicket = true;
        emailParams.TicketCode = "TKT-ABC123";
        emailParams.TicketExpiryDate = "February 15, 2026";

        // Assert
        emailParams.HasTicket.Should().BeTrue();
        emailParams.TicketCode.Should().Be("TKT-ABC123");
        emailParams.TicketExpiryDate.Should().Be("February 15, 2026");
    }

    [Fact]
    public void EventReminderEmailParams_ShouldHaveDefaultEmptyStringsForOptionalFields()
    {
        // Arrange & Act
        var emailParams = new EventReminderEmailParams();

        // Assert - Optional fields should default to empty strings, not null
        emailParams.OrganizerContactName.Should().Be("");
        emailParams.OrganizerContactEmail.Should().Be("");
        emailParams.OrganizerContactPhone.Should().Be("");
        emailParams.TicketCode.Should().Be("");
        emailParams.TicketExpiryDate.Should().Be("");
        emailParams.HasOrganizerContact.Should().BeFalse();
        emailParams.HasTicket.Should().BeFalse();
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void EventReminderEmailParams_ToDictionary_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var emailParams = CreateValidParams();
        emailParams.EventId = eventId;
        emailParams.RegistrationId = registrationId;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - All required template parameters
        dict.Should().ContainKey("AttendeeName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventStartDate");
        dict.Should().ContainKey("EventStartTime");
        dict.Should().ContainKey("Location");
        dict.Should().ContainKey("Quantity");
        dict.Should().ContainKey("HoursUntilEvent");
        dict.Should().ContainKey("ReminderTimeframe");
        dict.Should().ContainKey("ReminderMessage");
        dict.Should().ContainKey("EventDetailsUrl");
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_ShouldIncludeOrganizerContactParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasOrganizerContact = true;
        emailParams.OrganizerContactName = "Jane Smith";
        emailParams.OrganizerContactEmail = "jane@example.com";
        emailParams.OrganizerContactPhone = "555-123-4567";

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
        dict["OrganizerContactPhone"].Should().Be("555-123-4567");
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_ShouldIncludeTicketParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasTicket = true;
        emailParams.TicketCode = "TKT-ABC123";
        emailParams.TicketExpiryDate = "February 15, 2026";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasTicket");
        dict.Should().ContainKey("TicketCode");
        dict.Should().ContainKey("TicketExpiryDate");
        dict["HasTicket"].Should().Be(true);
        dict["TicketCode"].Should().Be("TKT-ABC123");
        dict["TicketExpiryDate"].Should().Be("February 15, 2026");
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_HasTicketShouldBeFalseByDefault()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - HasTicket should be false when not set
        dict.Should().ContainKey("HasTicket");
        dict["HasTicket"].Should().Be(false);
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_ShouldFormatDateCorrectly()
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
    public void EventReminderEmailParams_ToDictionary_QuantityShouldBeInteger()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.Quantity = 3;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict["Quantity"].Should().Be(3);
    }

    [Fact]
    public void EventReminderEmailParams_ToDictionary_HoursUntilEventShouldBeDouble()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HoursUntilEvent = 24.5;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict["HoursUntilEvent"].Should().Be(24.5);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldFailWhenEventIdIsEmpty()
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
    public void EventReminderEmailParams_Validate_ShouldFailWhenAttendeeNameIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AttendeeName = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("AttendeeName is required");
    }

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldFailWhenAttendeeEmailIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.AttendeeEmail = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("AttendeeEmail is required");
    }

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldFailWhenEventTitleIsEmpty()
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
    public void EventReminderEmailParams_Validate_ShouldFailWhenEventDetailsUrlIsEmpty()
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
    public void EventReminderEmailParams_Validate_ShouldFailWhenReminderTimeframeIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.ReminderTimeframe = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("ReminderTimeframe is required");
    }

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldFailWhenReminderMessageIsEmpty()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.ReminderMessage = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("ReminderMessage is required");
    }

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldFailWhenHasOrganizerContactButNameIsEmpty()
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

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldSucceedWhenAllRequiredFieldsProvided()
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
    public void EventReminderEmailParams_Validate_ShouldSucceedWithOrganizerContact()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.HasOrganizerContact = true;
        emailParams.OrganizerContactName = "Jane Smith";
        emailParams.OrganizerContactEmail = "jane@example.com";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void EventReminderEmailParams_Validate_ShouldReportMultipleErrors()
    {
        // Arrange
        var emailParams = new EventReminderEmailParams
        {
            EventId = Guid.Empty,
            AttendeeName = "",
            AttendeeEmail = "",
            EventTitle = "",
            ReminderTimeframe = "tomorrow",
            ReminderMessage = "Your event is tomorrow!"
        };

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Count.Should().BeGreaterThan(1);
    }

    #endregion

    #region Factory Methods Tests

    [Fact]
    public void EventReminderEmailParams_FromEvent_ShouldCreateValidParams()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        // Act
        var emailParams = EventReminderEmailParams.Create(
            eventId: eventId,
            registrationId: registrationId,
            attendeeName: "John Doe",
            attendeeEmail: "john@example.com",
            eventTitle: "Community Meetup",
            eventStartDate: new DateTime(2026, 2, 15),
            eventStartTime: "10:00 AM",
            location: "123 Main St, Boston, MA",
            quantity: 2,
            hoursUntilEvent: 24.5,
            reminderTimeframe: "tomorrow",
            reminderMessage: "Your event is tomorrow!",
            eventDetailsUrl: "https://lankaconnect.com/events/abc123"
        );

        // Assert
        emailParams.EventId.Should().Be(eventId);
        emailParams.RegistrationId.Should().Be(registrationId);
        emailParams.AttendeeName.Should().Be("John Doe");
        emailParams.EventTitle.Should().Be("Community Meetup");
        emailParams.Validate(out _).Should().BeTrue();
    }

    [Fact]
    public void EventReminderEmailParams_WithOrganizerContact_ShouldSetOrganizerFields()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var withOrganizer = emailParams.WithOrganizerContact(
            name: "Jane Smith",
            email: "jane@example.com",
            phone: "555-123-4567"
        );

        // Assert
        withOrganizer.HasOrganizerContact.Should().BeTrue();
        withOrganizer.OrganizerContactName.Should().Be("Jane Smith");
        withOrganizer.OrganizerContactEmail.Should().Be("jane@example.com");
        withOrganizer.OrganizerContactPhone.Should().Be("555-123-4567");
    }

    [Fact]
    public void EventReminderEmailParams_WithTicket_ShouldSetTicketFields()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var withTicket = emailParams.WithTicket(
            ticketCode: "TKT-ABC123",
            expiryDate: "February 15, 2026"
        );

        // Assert - WithTicket should set HasTicket = true
        withTicket.HasTicket.Should().BeTrue();
        withTicket.TicketCode.Should().Be("TKT-ABC123");
        withTicket.TicketExpiryDate.Should().Be("February 15, 2026");
    }

    #endregion

    #region Helper Methods

    private static EventReminderEmailParams CreateValidParams()
    {
        return new EventReminderEmailParams
        {
            EventId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            AttendeeName = "John Doe",
            AttendeeEmail = "john@example.com",
            EventTitle = "Community Meetup",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            Location = "123 Main St, Boston, MA",
            Quantity = 2,
            HoursUntilEvent = 24.5,
            ReminderTimeframe = "tomorrow",
            ReminderMessage = "Your event is tomorrow!",
            EventDetailsUrl = "https://lankaconnect.com/events/abc123"
        };
    }

    #endregion
}
