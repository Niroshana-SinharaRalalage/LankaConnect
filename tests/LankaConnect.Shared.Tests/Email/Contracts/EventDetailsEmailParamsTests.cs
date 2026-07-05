using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Tests for EventDetailsEmailParams, specifically the HasLocation conditional logic.
/// </summary>
public class EventDetailsEmailParamsTests
{
    [Fact]
    public void ToDictionary_WhenHasLocationTrue_IncludesHasLocationAsTrue()
    {
        // Arrange
        var emailParams = EventDetailsEmailParams.Create(
            recipientEmail: "test@example.com",
            userName: "Test User",
            eventTitle: "Test Event",
            eventDate: "March 4, 2026",
            eventStartDate: "March 4, 2026",
            eventStartTime: "7:00 PM",
            eventDateTime: "March 4, 2026 at 7:00 PM",
            eventLocation: "123 Main St, Cleveland, OH",
            eventCity: "Cleveland",
            eventState: "OH",
            eventDescription: "Description",
            eventDetailsUrl: "https://lankaconnect.app/events/123",
            isFree: true,
            pricingDetails: "Free",
            ticketPrice: "Free",
            hasSignUpLists: false,
            signUpListsUrl: "",
            hasOrganizerContact: false,
            organizerContactName: "",
            organizerContactEmail: null,
            organizerContactPhone: null,
            subjectPrefix: "New Event:");
        emailParams.HasLocation = true;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasLocation");
        dict["HasLocation"].Should().Be(true);
        dict["EventCity"].Should().Be("Cleveland");
        dict["EventState"].Should().Be("OH");
    }

    [Fact]
    public void ToDictionary_WhenHasLocationFalse_IncludesHasLocationAsFalse()
    {
        // Arrange
        var emailParams = EventDetailsEmailParams.Create(
            recipientEmail: "test@example.com",
            userName: "Test User",
            eventTitle: "Test Event",
            eventDate: "March 4, 2026",
            eventStartDate: "March 4, 2026",
            eventStartTime: "7:00 PM",
            eventDateTime: "March 4, 2026 at 7:00 PM",
            eventLocation: "",
            eventCity: "",
            eventState: "",
            eventDescription: "Description",
            eventDetailsUrl: "https://lankaconnect.app/events/123",
            isFree: true,
            pricingDetails: "Free",
            ticketPrice: "Free",
            hasSignUpLists: false,
            signUpListsUrl: "",
            hasOrganizerContact: false,
            organizerContactName: "",
            organizerContactEmail: null,
            organizerContactPhone: null,
            subjectPrefix: "New Event:");
        emailParams.HasLocation = false;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasLocation");
        dict["HasLocation"].Should().Be(false);
    }

    [Fact]
    public void DefaultValues_ShouldNotContainTBA()
    {
        // Arrange & Act
        var emailParams = new EventDetailsEmailParams();

        // Assert
        emailParams.EventCity.Should().Be(string.Empty);
        emailParams.EventState.Should().Be(string.Empty);
        emailParams.HasLocation.Should().BeFalse();
    }
}
