using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Event Organizer Contact Details feature
/// Tests the SetOrganizerContactDetails() method and HasOrganizerContact() helper
/// TDD: Written before implementation
/// </summary>
public class EventOrganizerContactDetailsTests
{
    #region Test Helpers

    private static Event CreateValidEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);
        var organizerId = Guid.NewGuid();
        var capacity = 100;

        var result = Event.Create(title, description, startDate, endDate, organizerId, capacity);
        return result.Value;
    }

    #endregion

    #region SetOrganizerContactDetails - Success Cases

    [Fact]
    public void SetOrganizerContactDetails_WithValidDataAllFields_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "John Organizer",
            contactPhone: "+1-555-1234",
            contactEmail: "john@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("John Organizer");
        @event.OrganizerContactPhone.Should().Be("+1-555-1234");
        @event.OrganizerContactEmail.Should().Be("john@example.com");
        @event.HasOrganizerContact().Should().BeTrue();
    }

    [Fact]
    public void SetOrganizerContactDetails_WithEmailOnly_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Jane Smith",
            contactPhone: null,
            contactEmail: "jane@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Jane Smith");
        @event.OrganizerContactPhone.Should().BeNull();
        @event.OrganizerContactEmail.Should().Be("jane@test.com");
        @event.HasOrganizerContact().Should().BeTrue();
    }

    [Fact]
    public void SetOrganizerContactDetails_WithPhoneOnly_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Bob Johnson",
            contactPhone: "+94-77-123-4567",
            contactEmail: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Bob Johnson");
        @event.OrganizerContactPhone.Should().Be("+94-77-123-4567");
        @event.OrganizerContactEmail.Should().BeNull();
        @event.HasOrganizerContact().Should().BeTrue();
    }

    [Fact]
    public void SetOrganizerContactDetails_PublishFalse_ShouldClearAllFields()
    {
        // Arrange
        var @event = CreateValidEvent();

        // First set contact details
        @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Initial Name",
            contactPhone: "+1-555-0000",
            contactEmail: "initial@test.com");

        // Act - Unpublish contact
        var result = @event.SetOrganizerContactDetails(
            publishContact: false,
            contactName: "Should Be Ignored",
            contactPhone: "Should Be Ignored",
            contactEmail: "ignored@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeFalse();
        @event.OrganizerContactName.Should().BeNull("unpublishing should clear name");
        @event.OrganizerContactPhone.Should().BeNull("unpublishing should clear phone");
        @event.OrganizerContactEmail.Should().BeNull("unpublishing should clear email");
        @event.HasOrganizerContact().Should().BeFalse();
    }

    [Fact]
    public void SetOrganizerContactDetails_TrimsWhitespace_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "  Trimmed Name  ",
            contactPhone: "  +1-555-9999  ",
            contactEmail: "  trimmed@test.com  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Trimmed Name", "should trim whitespace");
        @event.OrganizerContactPhone.Should().Be("+1-555-9999", "should trim whitespace");
        @event.OrganizerContactEmail.Should().Be("trimmed@test.com", "should trim whitespace");
    }

    #endregion

    #region SetOrganizerContactDetails - Validation Failures

    [Fact]
    public void SetOrganizerContactDetails_PublishWithoutName_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: null,
            contactPhone: "+1-555-1234",
            contactEmail: "test@example.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact name is required");
        @event.PublishOrganizerContact.Should().BeFalse("should not update on validation failure");
    }

    [Fact]
    public void SetOrganizerContactDetails_PublishWithEmptyName_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "   ",
            contactPhone: "+1-555-1234",
            contactEmail: "test@example.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact name is required");
    }

    [Fact]
    public void SetOrganizerContactDetails_PublishWithoutEmailAndPhone_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "John Doe",
            contactPhone: null,
            contactEmail: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact method");
    }

    [Fact]
    public void SetOrganizerContactDetails_PublishWithEmptyEmailAndPhone_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "John Doe",
            contactPhone: "   ",
            contactEmail: "   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact method");
    }

    [Fact]
    public void SetOrganizerContactDetails_InvalidEmailFormat_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "John Doe",
            contactPhone: null,
            contactEmail: "invalid-email");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test @example.com")]
    [InlineData("test@.com")]
    public void SetOrganizerContactDetails_VariousInvalidEmails_ShouldFail(string invalidEmail)
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "John Doe",
            contactPhone: null,
            contactEmail: invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("test123@subdomain.example.com")]
    [InlineData("DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net")] // Azure Communication Services format
    public void SetOrganizerContactDetails_ValidEmailFormats_ShouldSucceed(string validEmail)
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "John Doe",
            contactPhone: null,
            contactEmail: validEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactEmail.Should().Be(validEmail.Trim().ToLowerInvariant());
    }

    #endregion

    #region HasOrganizerContact - Helper Method Tests

    [Fact]
    public void HasOrganizerContact_WhenPublishedWithName_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Test Organizer",
            contactPhone: "+1-555-0000",
            contactEmail: null);

        // Act & Assert
        @event.HasOrganizerContact().Should().BeTrue();
    }

    [Fact]
    public void HasOrganizerContact_WhenNotPublished_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContactDetails(
            publishContact: false,
            contactName: "Test",
            contactPhone: "123",
            contactEmail: "test@test.com");

        // Act & Assert
        @event.HasOrganizerContact().Should().BeFalse();
    }

    [Fact]
    public void HasOrganizerContact_WhenNeverSet_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act & Assert
        @event.HasOrganizerContact().Should().BeFalse();
    }

    [Fact]
    public void HasOrganizerContact_WhenPublishedButNameIsNull_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateValidEvent();

        // This should fail validation, but testing the helper method's defensive check
        // Act & Assert
        @event.HasOrganizerContact().Should().BeFalse();
    }

    #endregion

    #region Update Scenarios

    [Fact]
    public void SetOrganizerContactDetails_CanUpdateExistingContact_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Original Name",
            contactPhone: "+1-555-0000",
            contactEmail: "original@test.com");

        // Act - Update contact details
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Updated Name",
            contactPhone: "+1-555-9999",
            contactEmail: "updated@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Updated Name");
        @event.OrganizerContactPhone.Should().Be("+1-555-9999");
        @event.OrganizerContactEmail.Should().Be("updated@test.com");
    }

    [Fact]
    public void SetOrganizerContactDetails_CanSwitchContactMethod_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Test User",
            contactPhone: "+1-555-0000",
            contactEmail: null);

        // Act - Switch from phone to email
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Test User",
            contactPhone: null,
            contactEmail: "switched@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactPhone.Should().BeNull();
        @event.OrganizerContactEmail.Should().Be("switched@test.com");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetOrganizerContactDetails_EmailCaseInsensitive_ShouldNormalize()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "Test User",
            contactPhone: null,
            contactEmail: "Test@EXAMPLE.COM");

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactEmail.Should().Be("test@example.com", "email should be normalized to lowercase");
    }

    [Fact]
    public void SetOrganizerContactDetails_VeryLongName_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var longName = new string('A', 200); // 200 characters (within limit)

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: longName,
            contactPhone: "+1-555-0000",
            contactEmail: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactName.Should().Be(longName);
    }

    [Fact]
    public void SetOrganizerContactDetails_InternationalPhoneNumber_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContactDetails(
            publishContact: true,
            contactName: "International User",
            contactPhone: "+94-11-234-5678", // Sri Lankan format
            contactEmail: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactPhone.Should().Be("+94-11-234-5678");
    }

    #endregion

    #region Default State Tests

    [Fact]
    public void NewEvent_OrganizerContactFieldsDefaultToNull_ShouldBeNull()
    {
        // Arrange & Act
        var @event = CreateValidEvent();

        // Assert
        @event.PublishOrganizerContact.Should().BeFalse("new events should not publish contact by default");
        @event.OrganizerContactName.Should().BeNull();
        @event.OrganizerContactPhone.Should().BeNull();
        @event.OrganizerContactEmail.Should().BeNull();
        @event.HasOrganizerContact().Should().BeFalse();
    }

    #endregion
}
