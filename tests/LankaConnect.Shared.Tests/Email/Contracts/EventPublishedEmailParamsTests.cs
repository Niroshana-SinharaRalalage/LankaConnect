using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Tests for EventPublishedEmailParams, specifically the HasLocation conditional logic
/// that prevents "TBA, TBA" from appearing in email subjects.
/// </summary>
public class EventPublishedEmailParamsTests
{
    [Fact]
    public void ToDictionary_WhenHasLocationTrue_IncludesHasLocationAsTrue()
    {
        // Arrange
        var emailParams = EventPublishedEmailParams.Create(
            recipientEmail: "test@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventDescription: "Description",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St, Cleveland, OH",
            eventCity: "Cleveland",
            eventState: "OH",
            isFree: true,
            ticketPrice: "Free",
            eventUrl: "https://lankaconnect.app/events/123");
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
        var emailParams = EventPublishedEmailParams.Create(
            recipientEmail: "test@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventDescription: "Description",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: "America/New_York",
            eventLocation: "",
            eventCity: "",
            eventState: "",
            isFree: true,
            ticketPrice: "Free",
            eventUrl: "https://lankaconnect.app/events/123");
        emailParams.HasLocation = false;

        // Act
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("HasLocation");
        dict["HasLocation"].Should().Be(false);
        dict["EventCity"].Should().Be("");
        dict["EventState"].Should().Be("");
    }

    [Fact]
    public void DefaultValues_ShouldNotContainTBA()
    {
        // Arrange & Act
        var emailParams = new EventPublishedEmailParams();

        // Assert - Ensure TBA defaults have been removed
        emailParams.EventCity.Should().Be(string.Empty);
        emailParams.EventState.Should().Be(string.Empty);
        emailParams.TicketPrice.Should().Be("See Event Details");
        emailParams.HasLocation.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithRequiredFields_ReturnsTrue()
    {
        // Arrange
        var emailParams = EventPublishedEmailParams.Create(
            recipientEmail: "test@example.com",
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventDescription: "Description",
            eventStartDate: DateTime.UtcNow,
            timeZoneId: null,
            eventLocation: "",
            eventCity: "",
            eventState: "",
            isFree: true,
            ticketPrice: "Free",
            eventUrl: "https://lankaconnect.app/events/123");

        // Act
        var isValid = emailParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }
}
