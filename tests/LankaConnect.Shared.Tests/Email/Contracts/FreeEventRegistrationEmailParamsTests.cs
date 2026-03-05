using LankaConnect.Shared.Email.Contracts;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 4: Tests for FreeEventRegistrationEmailParams.
/// Follows TDD approach - tests written first, implementation follows.
/// </summary>
public class FreeEventRegistrationEmailParamsTests
{
    private FreeEventRegistrationEmailParams CreateValidParams()
    {
        return FreeEventRegistrationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john.doe@example.com",
            eventTitle: "Community Meetup",
            eventStartDate: new DateTime(2026, 2, 15),
            eventStartTime: "6:00 PM",
            eventLocation: "123 Main St, Colombo",
            eventDetailsUrl: "https://lankaconnect.com/events/123",
            registrationDate: new DateTime(2026, 1, 28, 10, 30, 0));
    }

    #region Basic Properties Tests

    [Fact]
    public void TemplateName_ShouldReturnFreeEventRegistrationTemplate()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var templateName = emailParams.TemplateName;

        // Assert
        templateName.Should().Be("template-free-event-registration-confirmation");
    }

    [Fact]
    public void RecipientEmail_ShouldReturnUserEmail()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var recipientEmail = emailParams.RecipientEmail;

        // Assert
        recipientEmail.Should().Be("john.doe@example.com");
    }

    [Fact]
    public void RecipientName_ShouldReturnUserName()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var recipientName = emailParams.RecipientName;

        // Assert
        recipientName.Should().Be("John Doe");
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

        // Assert
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventStartDate");
        dict.Should().ContainKey("EventStartTime");
        dict.Should().ContainKey("EventLocation");
        dict.Should().ContainKey("RegistrationDate");
        dict.Should().ContainKey("EventDetailsUrl");
    }

    [Fact]
    public void ToDictionary_ShouldFormatDateCorrectly()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict["EventStartDate"].Should().Be("February 15, 2026");
    }

    [Fact]
    public void ToDictionary_ShouldContainAttendeeParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.WithAttendees("<p>Attendee 1</p><p>Attendee 2</p>");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasAttendeeDetails");
        dict.Should().ContainKey("Attendees");
        dict["HasAttendeeDetails"].Should().Be(true);
        dict["Attendees"].Should().Be("<p>Attendee 1</p><p>Attendee 2</p>");
    }

    [Fact]
    public void ToDictionary_ShouldContainEventImageParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.WithEventImage("https://storage.example.com/images/event.jpg");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasEventImage");
        dict.Should().ContainKey("EventImageUrl");
        dict["HasEventImage"].Should().Be(true);
        dict["EventImageUrl"].Should().Be("https://storage.example.com/images/event.jpg");
    }

    [Fact]
    public void ToDictionary_ShouldContainOrganizerContactParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "+94 77 123 4567");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasOrganizerContact");
        dict.Should().ContainKey("OrganizerContactName");
        dict.Should().ContainKey("OrganizerContactEmail");
        dict.Should().ContainKey("OrganizerContactPhone");
        dict["HasOrganizerContact"].Should().Be(true);
        dict["OrganizerContactName"].Should().Be("Jane Smith");
    }

    [Fact]
    public void ToDictionary_ShouldContainContactInfoParameters()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.WithContactInfo("john@contact.com", "+94 77 999 8888");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasContactInfo");
        dict.Should().ContainKey("ContactEmail");
        dict.Should().ContainKey("ContactPhone");
        dict["HasContactInfo"].Should().Be(true);
        dict["ContactEmail"].Should().Be("john@contact.com");
        dict["ContactPhone"].Should().Be("+94 77 999 8888");
    }

    [Fact]
    public void ToDictionary_ShouldContainSignUpListsUrl()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.WithSignUpLists("https://lankaconnect.com/events/123#sign-ups");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("SignUpListsUrl");
        dict["SignUpListsUrl"].Should().Be("https://lankaconnect.com/events/123#sign-ups");
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
        errors.Should().Contain(e => e.Contains("UserName"));
    }

    [Fact]
    public void Validate_WithEmptyUserEmail_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.UserEmail = "";

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("UserEmail"));
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
        errors.Should().Contain(e => e.Contains("EventTitle"));
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
        errors.Should().Contain(e => e.Contains("EventDetailsUrl"));
    }

    [Fact]
    public void Validate_WithEmptyEventId_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.EventId = Guid.Empty;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("EventId"));
    }

    [Fact]
    public void Validate_WithEmptyRegistrationId_ShouldReturnError()
    {
        // Arrange
        var emailParams = CreateValidParams();
        emailParams.RegistrationId = Guid.Empty;

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RegistrationId"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var emailParams = new FreeEventRegistrationEmailParams
        {
            EventId = Guid.Empty,
            RegistrationId = Guid.Empty,
            UserName = "",
            UserEmail = "",
            EventTitle = "",
            EventDetailsUrl = ""
        };

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCountGreaterThan(3);
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
        errors.Should().Contain(e => e.Contains("OrganizerContactName"));
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Create_ShouldSetRequiredProperties()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var userName = "Test User";
        var userEmail = "test@example.com";
        var eventTitle = "Test Event";
        var eventStartDate = new DateTime(2026, 3, 1);
        var eventStartTime = "7:30 PM";
        var eventLocation = "Test Location";
        var eventDetailsUrl = "https://example.com/events/123";
        var registrationDate = new DateTime(2026, 1, 28);

        // Act
        var emailParams = FreeEventRegistrationEmailParams.Create(
            eventId, registrationId, userName, userEmail, eventTitle,
            eventStartDate, eventStartTime, eventLocation, eventDetailsUrl, registrationDate);

        // Assert
        emailParams.EventId.Should().Be(eventId);
        emailParams.RegistrationId.Should().Be(registrationId);
        emailParams.UserName.Should().Be(userName);
        emailParams.UserEmail.Should().Be(userEmail);
        emailParams.EventTitle.Should().Be(eventTitle);
        emailParams.EventStartDate.Should().Be(eventStartDate);
        emailParams.EventStartTime.Should().Be(eventStartTime);
        emailParams.EventLocation.Should().Be(eventLocation);
        emailParams.EventDetailsUrl.Should().Be(eventDetailsUrl);
        emailParams.RegistrationDate.Should().Be(registrationDate);
    }

    #endregion

    #region Fluent Builder Tests

    [Fact]
    public void WithAttendees_ShouldSetAttendeeProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();
        var attendeesHtml = "<p>John</p><p>Jane</p>";

        // Act
        var result = emailParams.WithAttendees(attendeesHtml);

        // Assert
        result.HasAttendeeDetails.Should().BeTrue();
        result.AttendeesHtml.Should().Be(attendeesHtml);
        result.Should().BeSameAs(emailParams); // Fluent return
    }

    [Fact]
    public void WithOrganizerContact_ShouldSetOrganizerProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var result = emailParams.WithOrganizerContact("Organizer Name", "org@example.com", "+94 11 111 1111");

        // Assert
        result.HasOrganizerContact.Should().BeTrue();
        result.OrganizerContactName.Should().Be("Organizer Name");
        result.OrganizerContactEmail.Should().Be("org@example.com");
        result.OrganizerContactPhone.Should().Be("+94 11 111 1111");
        result.Should().BeSameAs(emailParams); // Fluent return
    }

    [Fact]
    public void WithEventImage_ShouldSetImageProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();
        var imageUrl = "https://example.com/image.jpg";

        // Act
        var result = emailParams.WithEventImage(imageUrl);

        // Assert
        result.HasEventImage.Should().BeTrue();
        result.EventImageUrl.Should().Be(imageUrl);
        result.Should().BeSameAs(emailParams); // Fluent return
    }

    [Fact]
    public void WithEventImage_WithEmptyUrl_ShouldSetHasEventImageFalse()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var result = emailParams.WithEventImage("");

        // Assert
        result.HasEventImage.Should().BeFalse();
        result.EventImageUrl.Should().BeEmpty();
    }

    [Fact]
    public void WithContactInfo_ShouldSetContactProperties()
    {
        // Arrange
        var emailParams = CreateValidParams();

        // Act
        var result = emailParams.WithContactInfo("contact@example.com", "+94 77 777 7777");

        // Assert
        result.HasContactInfo.Should().BeTrue();
        result.ContactEmail.Should().Be("contact@example.com");
        result.ContactPhone.Should().Be("+94 77 777 7777");
        result.Should().BeSameAs(emailParams); // Fluent return
    }

    [Fact]
    public void WithSignUpLists_ShouldSetSignUpListsUrl()
    {
        // Arrange
        var emailParams = CreateValidParams();
        var signUpListsUrl = "https://example.com/events/123#sign-ups";

        // Act
        var result = emailParams.WithSignUpLists(signUpListsUrl);

        // Assert
        result.SignUpListsUrl.Should().Be(signUpListsUrl);
        result.Should().BeSameAs(emailParams); // Fluent return
    }

    #endregion
}
