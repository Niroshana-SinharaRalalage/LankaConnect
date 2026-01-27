using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.87 Day 2: Tests for base parameter contracts (TDD - RED phase)
/// Tests UserEmailParams, EventEmailParams, and OrganizerEmailParams
/// </summary>
public class BaseParameterContractsTests
{
    #region UserEmailParams Tests

    [Fact]
    public void UserEmailParams_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var userParams = new UserEmailParams
        {
            UserId = Guid.NewGuid(),
            UserName = "John Doe",
            UserEmail = "john@example.com"
        };

        // Assert
        userParams.UserId.Should().NotBeEmpty();
        userParams.UserName.Should().Be("John Doe");
        userParams.UserEmail.Should().Be("john@example.com");
    }

    [Fact]
    public void UserEmailParams_ToDictionary_ShouldIncludeAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userParams = new UserEmailParams
        {
            UserId = userId,
            UserName = "John Doe",
            UserEmail = "john@example.com"
        };

        // Act
        var dict = userParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("UserId");
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("UserEmail");
        dict["UserId"].Should().Be(userId.ToString());
        dict["UserName"].Should().Be("John Doe");
        dict["UserEmail"].Should().Be("john@example.com");
    }

    [Fact]
    public void UserEmailParams_Validate_ShouldFailWhenUserIdIsEmpty()
    {
        // Arrange
        var userParams = new UserEmailParams
        {
            UserId = Guid.Empty,
            UserName = "John Doe",
            UserEmail = "john@example.com"
        };

        // Act
        var isValid = userParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserId is required");
    }

    [Fact]
    public void UserEmailParams_Validate_ShouldFailWhenUserNameIsEmpty()
    {
        // Arrange
        var userParams = new UserEmailParams
        {
            UserId = Guid.NewGuid(),
            UserName = "",
            UserEmail = "john@example.com"
        };

        // Act
        var isValid = userParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserName is required");
    }

    [Fact]
    public void UserEmailParams_Validate_ShouldFailWhenUserEmailIsEmpty()
    {
        // Arrange
        var userParams = new UserEmailParams
        {
            UserId = Guid.NewGuid(),
            UserName = "John Doe",
            UserEmail = ""
        };

        // Act
        var isValid = userParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("UserEmail is required");
    }

    [Fact]
    public void UserEmailParams_Validate_ShouldSucceedWhenAllFieldsProvided()
    {
        // Arrange
        var userParams = new UserEmailParams
        {
            UserId = Guid.NewGuid(),
            UserName = "John Doe",
            UserEmail = "john@example.com"
        };

        // Act
        var isValid = userParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region EventEmailParams Tests

    [Fact]
    public void EventEmailParams_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var eventParams = new EventEmailParams
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St, Boston, MA",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://lankaconnect.com/events/abc123"
        };

        // Assert
        eventParams.EventId.Should().NotBeEmpty();
        eventParams.EventTitle.Should().Be("Community Meetup");
        eventParams.EventLocation.Should().Be("123 Main St, Boston, MA");
        eventParams.EventStartDate.Should().Be(new DateTime(2026, 2, 15));
        eventParams.EventStartTime.Should().Be("10:00 AM");
        eventParams.EventDetailsUrl.Should().Be("https://lankaconnect.com/events/abc123");
    }

    [Fact]
    public void EventEmailParams_ToDictionary_ShouldIncludeAllProperties()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventParams = new EventEmailParams
        {
            EventId = eventId,
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St, Boston, MA",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://lankaconnect.com/events/abc123"
        };

        // Act
        var dict = eventParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("EventId");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventLocation");
        dict.Should().ContainKey("EventStartDate");
        dict.Should().ContainKey("EventStartTime");
        dict.Should().ContainKey("EventDetailsUrl");
        dict["EventId"].Should().Be(eventId.ToString());
        dict["EventTitle"].Should().Be("Community Meetup");
        dict["EventLocation"].Should().Be("123 Main St, Boston, MA");
        dict["EventStartTime"].Should().Be("10:00 AM");
    }

    [Fact]
    public void EventEmailParams_ToDictionary_ShouldFormatDateCorrectly()
    {
        // Arrange
        var eventParams = new EventEmailParams
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://example.com"
        };

        // Act
        var dict = eventParams.ToDictionary();

        // Assert
        dict["EventStartDate"].Should().Be("February 15, 2026");
    }

    [Fact]
    public void EventEmailParams_ShouldHaveEventDateTime_CombinedProperty()
    {
        // Arrange
        var eventParams = new EventEmailParams
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://example.com"
        };

        // Act
        var dict = eventParams.ToDictionary();

        // Assert - EventDateTime should be combined date + time for templates that expect it
        dict.Should().ContainKey("EventDateTime");
        dict["EventDateTime"].Should().Be("February 15, 2026 at 10:00 AM");
    }

    [Fact]
    public void EventEmailParams_Validate_ShouldFailWhenEventIdIsEmpty()
    {
        // Arrange
        var eventParams = new EventEmailParams
        {
            EventId = Guid.Empty,
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://example.com"
        };

        // Act
        var isValid = eventParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("EventId is required");
    }

    [Fact]
    public void EventEmailParams_Validate_ShouldFailWhenEventTitleIsEmpty()
    {
        // Arrange
        var eventParams = new EventEmailParams
        {
            EventId = Guid.NewGuid(),
            EventTitle = "",
            EventLocation = "123 Main St",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://example.com"
        };

        // Act
        var isValid = eventParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("EventTitle is required");
    }

    [Fact]
    public void EventEmailParams_Validate_ShouldSucceedWhenAllFieldsProvided()
    {
        // Arrange
        var eventParams = new EventEmailParams
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://example.com"
        };

        // Act
        var isValid = eventParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region OrganizerEmailParams Tests

    [Fact]
    public void OrganizerEmailParams_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = true,
            OrganizerContactName = "Jane Smith",
            OrganizerContactEmail = "jane@example.com",
            OrganizerContactPhone = "555-123-4567"
        };

        // Assert
        organizerParams.HasOrganizerContact.Should().BeTrue();
        organizerParams.OrganizerContactName.Should().Be("Jane Smith");
        organizerParams.OrganizerContactEmail.Should().Be("jane@example.com");
        organizerParams.OrganizerContactPhone.Should().Be("555-123-4567");
    }

    [Fact]
    public void OrganizerEmailParams_ToDictionary_ShouldIncludeAllProperties()
    {
        // Arrange
        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = true,
            OrganizerContactName = "Jane Smith",
            OrganizerContactEmail = "jane@example.com",
            OrganizerContactPhone = "555-123-4567"
        };

        // Act
        var dict = organizerParams.ToDictionary();

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
    public void OrganizerEmailParams_WhenNoContact_ShouldHaveEmptyValues()
    {
        // Arrange
        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = false,
            OrganizerContactName = "",
            OrganizerContactEmail = "",
            OrganizerContactPhone = ""
        };

        // Act
        var dict = organizerParams.ToDictionary();

        // Assert
        dict["HasOrganizerContact"].Should().Be(false);
        dict["OrganizerContactName"].Should().Be("");
        dict["OrganizerContactEmail"].Should().Be("");
        dict["OrganizerContactPhone"].Should().Be("");
    }

    [Fact]
    public void OrganizerEmailParams_Validate_ShouldFailWhenHasContactButNameIsEmpty()
    {
        // Arrange
        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = true,
            OrganizerContactName = "",
            OrganizerContactEmail = "jane@example.com",
            OrganizerContactPhone = "555-123-4567"
        };

        // Act
        var isValid = organizerParams.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("OrganizerContactName is required when HasOrganizerContact is true");
    }

    [Fact]
    public void OrganizerEmailParams_Validate_ShouldSucceedWhenNoContact()
    {
        // Arrange - When HasOrganizerContact is false, other fields can be empty
        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = false,
            OrganizerContactName = "",
            OrganizerContactEmail = "",
            OrganizerContactPhone = ""
        };

        // Act
        var isValid = organizerParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void OrganizerEmailParams_Validate_ShouldSucceedWhenHasContactWithAllFields()
    {
        // Arrange
        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = true,
            OrganizerContactName = "Jane Smith",
            OrganizerContactEmail = "jane@example.com",
            OrganizerContactPhone = "555-123-4567"
        };

        // Act
        var isValid = organizerParams.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void OrganizerEmailParams_ShouldProvideDefaultEmptyStrings()
    {
        // Arrange & Act
        var organizerParams = new OrganizerEmailParams();

        // Assert - Default values should be empty strings, not null
        organizerParams.OrganizerContactName.Should().Be("");
        organizerParams.OrganizerContactEmail.Should().Be("");
        organizerParams.OrganizerContactPhone.Should().Be("");
        organizerParams.HasOrganizerContact.Should().BeFalse();
    }

    #endregion

    #region Combined Usage Tests

    [Fact]
    public void AllBaseParams_CanBeCombinedIntoSingleDictionary()
    {
        // Arrange
        var userParams = new UserEmailParams
        {
            UserId = Guid.NewGuid(),
            UserName = "John Doe",
            UserEmail = "john@example.com"
        };

        var eventParams = new EventEmailParams
        {
            EventId = Guid.NewGuid(),
            EventTitle = "Community Meetup",
            EventLocation = "123 Main St",
            EventStartDate = new DateTime(2026, 2, 15),
            EventStartTime = "10:00 AM",
            EventDetailsUrl = "https://example.com"
        };

        var organizerParams = new OrganizerEmailParams
        {
            HasOrganizerContact = true,
            OrganizerContactName = "Jane Smith",
            OrganizerContactEmail = "jane@example.com",
            OrganizerContactPhone = "555-123-4567"
        };

        // Act - Combine all parameters into one dictionary
        var combined = new Dictionary<string, object>();
        foreach (var kvp in userParams.ToDictionary())
            combined[kvp.Key] = kvp.Value;
        foreach (var kvp in eventParams.ToDictionary())
            combined[kvp.Key] = kvp.Value;
        foreach (var kvp in organizerParams.ToDictionary())
            combined[kvp.Key] = kvp.Value;

        // Assert - Should have all parameters from all three contracts
        combined.Should().ContainKey("UserId");
        combined.Should().ContainKey("UserName");
        combined.Should().ContainKey("EventId");
        combined.Should().ContainKey("EventTitle");
        combined.Should().ContainKey("EventDateTime");
        combined.Should().ContainKey("OrganizerContactName");
        combined.Should().ContainKey("OrganizerContactEmail");
        combined.Should().ContainKey("HasOrganizerContact");
    }

    #endregion
}
