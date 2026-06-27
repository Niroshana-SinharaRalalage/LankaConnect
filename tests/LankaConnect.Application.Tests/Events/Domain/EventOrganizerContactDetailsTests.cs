using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Event Organizer Contacts feature (multiple contacts per event).
/// Tests SetOrganizerContacts(), GetPrimaryContact(), and HasOrganizerContact().
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

    #region SetOrganizerContacts - Success Cases

    [Fact]
    public void SetOrganizerContacts_WithSingleContact_AllFields_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("John Organizer", "john@example.com", "+1-555-1234")
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(1);
        @event.OrganizerContacts[0].ContactName.Should().Be("John Organizer");
        @event.OrganizerContacts[0].ContactEmail.Should().Be("john@example.com");
        @event.OrganizerContacts[0].ContactPhone.Should().Be("+1-555-1234");
        @event.OrganizerContacts[0].IsPrimary.Should().BeFalse("no forced primary — backward-compatible overload passes isPrimary=false");
        @event.HasOrganizerContact().Should().BeTrue();
    }

    [Fact]
    public void SetOrganizerContacts_WithMultipleContacts_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Primary Contact", "primary@example.com", "+1-555-0001"),
                ("Secondary Contact", "secondary@example.com", null),
                ("Tertiary Contact", null, "+1-555-0003")
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(3);
        @event.OrganizerContacts[0].IsPrimary.Should().BeFalse("no forced primary — backward-compatible overload passes isPrimary=false");
        @event.OrganizerContacts[1].IsPrimary.Should().BeFalse();
        @event.OrganizerContacts[2].IsPrimary.Should().BeFalse();
        @event.OrganizerContacts[0].SortOrder.Should().Be(0);
        @event.OrganizerContacts[1].SortOrder.Should().Be(1);
        @event.OrganizerContacts[2].SortOrder.Should().Be(2);
    }

    [Fact]
    public void SetOrganizerContacts_WithEmailOnly_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Jane Smith", "jane@test.com", null)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(1);
        @event.OrganizerContacts[0].ContactName.Should().Be("Jane Smith");
        @event.OrganizerContacts[0].ContactPhone.Should().BeNull();
        @event.OrganizerContacts[0].ContactEmail.Should().Be("jane@test.com");
    }

    [Fact]
    public void SetOrganizerContacts_WithPhoneOnly_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Bob Johnson", null, "+94-77-123-4567")
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(1);
        @event.OrganizerContacts[0].ContactName.Should().Be("Bob Johnson");
        @event.OrganizerContacts[0].ContactPhone.Should().Be("+94-77-123-4567");
        @event.OrganizerContacts[0].ContactEmail.Should().BeNull();
    }

    [Fact]
    public void SetOrganizerContacts_PublishFalse_ShouldClearAllContacts()
    {
        // Arrange
        var @event = CreateValidEvent();

        // First set contacts
        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Initial Name", "initial@test.com", "+1-555-0000")
            });

        // Act - Unpublish contact
        var result = @event.SetOrganizerContacts(
            publishContact: false,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Should Be Ignored", "ignored@test.com", "Should Be Ignored")
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeFalse();
        @event.OrganizerContacts.Should().BeEmpty("unpublishing should clear all contacts");
        @event.HasOrganizerContact().Should().BeFalse();
    }

    #endregion

    #region SetOrganizerContacts - Validation Failures

    [Fact]
    public void SetOrganizerContacts_PublishWithEmptyList_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one organizer contact is required");
    }

    [Fact]
    public void SetOrganizerContacts_PublishWithBlankName_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("   ", "test@example.com", "+1-555-1234")
            });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact name is required");
    }

    [Fact]
    public void SetOrganizerContacts_PublishWithoutEmailAndPhone_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("John Doe", null, null)
            });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact method");
    }

    [Fact]
    public void SetOrganizerContacts_PublishWithEmptyEmailAndPhone_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("John Doe", "   ", "   ")
            });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact method");
    }

    [Fact]
    public void SetOrganizerContacts_InvalidEmailFormat_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("John Doe", "invalid-email", null)
            });

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
    public void SetOrganizerContacts_VariousInvalidEmails_ShouldFail(string invalidEmail)
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("John Doe", invalidEmail, null)
            });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("test123@subdomain.example.com")]
    [InlineData("DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net")]
    public void SetOrganizerContacts_ValidEmailFormats_ShouldSucceed(string validEmail)
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("John Doe", validEmail, null)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].ContactEmail.Should().Be(validEmail.Trim().ToLowerInvariant());
    }

    #endregion

    #region Backward-Compatible Computed Properties

    [Fact]
    public void OrganizerContactName_ShouldReturnPrimaryContactName()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Primary User", "primary@test.com", null),
                ("Secondary User", "secondary@test.com", null)
            });

        // Act & Assert
        @event.OrganizerContactName.Should().Be("Primary User");
        @event.OrganizerContactEmail.Should().Be("primary@test.com");
    }

    [Fact]
    public void OrganizerContactName_WhenNoContacts_ShouldReturnNull()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act & Assert
        @event.OrganizerContactName.Should().BeNull();
        @event.OrganizerContactEmail.Should().BeNull();
        @event.OrganizerContactPhone.Should().BeNull();
    }

    #endregion

    #region GetPrimaryContact

    [Fact]
    public void GetPrimaryContact_WithMultipleContacts_NoPrimarySet_ShouldFallbackToFirst()
    {
        // Arrange - backward-compatible overload sets isPrimary=false for all
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("First Contact", "first@test.com", null),
                ("Second Contact", "second@test.com", null)
            });

        // Act
        var primary = @event.GetPrimaryContact();

        // Assert - GetPrimaryContact falls back to first contact for email compatibility
        primary.Should().NotBeNull();
        primary!.ContactName.Should().Be("First Contact");
        primary.IsPrimary.Should().BeFalse("no contact is explicitly marked primary");
    }

    [Fact]
    public void GetPrimaryContact_WhenNoContacts_ShouldReturnNull()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var primary = @event.GetPrimaryContact();

        // Assert
        primary.Should().BeNull();
    }

    #endregion

    #region HasOrganizerContact

    [Fact]
    public void HasOrganizerContact_WhenPublishedWithContacts_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Test Organizer", null, "+1-555-0000")
            });

        // Act & Assert
        @event.HasOrganizerContact().Should().BeTrue();
    }

    [Fact]
    public void HasOrganizerContact_WhenNotPublished_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(
            publishContact: false,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Test", "test@test.com", "123")
            });

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

    #endregion

    #region Update Scenarios

    [Fact]
    public void SetOrganizerContacts_CanReplaceExistingContacts_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Original Name", "original@test.com", "+1-555-0000")
            });

        // Act - Replace with new contacts
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Updated Name", "updated@test.com", "+1-555-9999"),
                ("New Second Contact", "second@test.com", null)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(2);
        @event.OrganizerContacts[0].ContactName.Should().Be("Updated Name");
        @event.OrganizerContacts[1].ContactName.Should().Be("New Second Contact");
    }

    [Fact]
    public void SetOrganizerContacts_CanSwitchContactMethod_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Test User", null, "+1-555-0000")
            });

        // Act - Switch from phone to email
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Test User", "switched@test.com", null)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].ContactPhone.Should().BeNull();
        @event.OrganizerContacts[0].ContactEmail.Should().Be("switched@test.com");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetOrganizerContacts_EmailCaseInsensitive_ShouldNormalize()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Test User", "Test@EXAMPLE.COM", null)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].ContactEmail.Should().Be("test@example.com", "email should be normalized to lowercase");
    }

    [Fact]
    public void SetOrganizerContacts_VeryLongName_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var longName = new string('A', 200);

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                (longName, null, "+1-555-0000")
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].ContactName.Should().Be(longName);
    }

    [Fact]
    public void SetOrganizerContacts_InternationalPhoneNumber_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("International User", null, "+94-11-234-5678")
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].ContactPhone.Should().Be("+94-11-234-5678");
    }

    #endregion

    #region Default State Tests

    [Fact]
    public void NewEvent_OrganizerContactFieldsDefaultToEmpty_ShouldBeEmpty()
    {
        // Arrange & Act
        var @event = CreateValidEvent();

        // Assert
        @event.PublishOrganizerContact.Should().BeFalse("new events should not publish contact by default");
        @event.OrganizerContacts.Should().BeEmpty();
        @event.OrganizerContactName.Should().BeNull();
        @event.OrganizerContactPhone.Should().BeNull();
        @event.OrganizerContactEmail.Should().BeNull();
        @event.HasOrganizerContact().Should().BeFalse();
    }

    #endregion

    #region EventOrganizerContact Entity Tests

    [Fact]
    public void EventOrganizerContact_Create_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var result = EventOrganizerContact.Create(
            Guid.NewGuid(), "Test Contact", "test@example.com", "+1-555-0000", true, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ContactName.Should().Be("Test Contact");
        result.Value.ContactEmail.Should().Be("test@example.com");
        result.Value.ContactPhone.Should().Be("+1-555-0000");
        result.Value.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void EventOrganizerContact_Create_WithEmptyEventId_ShouldFail()
    {
        // Act
        var result = EventOrganizerContact.Create(
            Guid.Empty, "Test", "test@example.com", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    [Fact]
    public void EventOrganizerContact_Create_WithEmptyName_ShouldFail()
    {
        // Act
        var result = EventOrganizerContact.Create(
            Guid.NewGuid(), "", "test@example.com", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact name is required");
    }

    [Fact]
    public void EventOrganizerContact_Create_WithNoContactMethod_ShouldFail()
    {
        // Act
        var result = EventOrganizerContact.Create(
            Guid.NewGuid(), "Test", null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact method");
    }

    [Fact]
    public void EventOrganizerContact_Update_ShouldModifyFields()
    {
        // Arrange
        var contact = EventOrganizerContact.Create(
            Guid.NewGuid(), "Original", "original@test.com", null).Value;

        // Act
        var result = contact.Update("Updated", "updated@test.com", "+1-555-0000");

        // Assert
        result.IsSuccess.Should().BeTrue();
        contact.ContactName.Should().Be("Updated");
        contact.ContactEmail.Should().Be("updated@test.com");
        contact.ContactPhone.Should().Be("+1-555-0000");
    }

    [Fact]
    public void EventOrganizerContact_NameTooLong_ShouldFail()
    {
        // Act
        var result = EventOrganizerContact.Create(
            Guid.NewGuid(), new string('A', 201), "test@example.com", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("less than 200 characters");
    }

    #endregion

    #region Max Contacts Limit (Phase 6A.132)

    [Fact]
    public void SetOrganizerContacts_WithElevenContacts_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateValidEvent();
        var contacts = Enumerable.Range(1, 11)
            .Select(i => ($"Contact {i}", (string?)$"c{i}@example.com", (string?)null))
            .ToList();

        // Act
        var result = @event.SetOrganizerContacts(publishContact: true, contacts);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Maximum");
        result.Error.Should().Contain("organizer contacts allowed");
    }

    [Fact]
    public void SetOrganizerContacts_WithTenContacts_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var contacts = Enumerable.Range(1, 10)
            .Select(i => ($"Contact {i}", (string?)$"c{i}@example.com", (string?)null))
            .ToList();

        // Act
        var result = @event.SetOrganizerContacts(publishContact: true, contacts);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(10);
        @event.OrganizerContacts.First().IsPrimary.Should().BeFalse("backward-compatible overload passes isPrimary=false");
    }

    #endregion

    #region Pre-Linked Co-Organizer (Phase 6A.133 UX Fix)

    [Fact]
    public void SetOrganizerContacts_WithLinkedUserId_ShouldPreLinkContact()
    {
        // Arrange
        var @event = CreateValidEvent();
        var coOrgUserId = Guid.NewGuid();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)>
            {
                ("Primary Organizer", "primary@test.com", null, (Guid?)null, true),
                ("Co-Organizer", "coorg@test.com", null, coOrgUserId, false)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(2);
        @event.OrganizerContacts[0].LinkedUserId.Should().BeNull("primary contact has no linked user");
        @event.OrganizerContacts[1].LinkedUserId.Should().Be(coOrgUserId, "co-organizer should be pre-linked");
    }

    [Fact]
    public void SetOrganizerContacts_WithLinkedUserId_IsOrganizerReturnsTrue()
    {
        // Arrange
        var @event = CreateValidEvent();
        var coOrgUserId = Guid.NewGuid();

        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)>
            {
                ("Co-Organizer", "coorg@test.com", null, coOrgUserId, false)
            });

        // Act & Assert
        @event.IsOrganizer(coOrgUserId).Should().BeTrue("pre-linked user should be recognized as organizer");
    }

    [Fact]
    public void SetOrganizerContacts_WithEmptyGuidLinkedUserId_ShouldNotLink()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)>
            {
                ("Test Contact", "test@test.com", null, Guid.Empty, false)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].LinkedUserId.Should().BeNull("empty GUID should not be stored");
    }

    [Fact]
    public void EventOrganizerContact_Create_WithLinkedUserId_ShouldSetProperty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = EventOrganizerContact.Create(
            Guid.NewGuid(), "Test Contact", "test@example.com", null,
            isPrimary: false, sortOrder: 0, linkedUserId: userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedUserId.Should().Be(userId);
    }

    [Fact]
    public void EventOrganizerContact_Create_WithoutLinkedUserId_ShouldBeNull()
    {
        // Act
        var result = EventOrganizerContact.Create(
            Guid.NewGuid(), "Test Contact", "test@example.com", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedUserId.Should().BeNull();
    }

    [Fact]
    public void SetOrganizerContacts_BackwardCompatibleOverload_ShouldWork()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act - using the 3-element tuple (backward-compatible overload)
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone)>
            {
                ("Test Contact", "test@test.com", null)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].LinkedUserId.Should().BeNull("backward-compatible overload should not link");
    }

    [Fact]
    public void SetOrganizerContacts_PrimaryFlagRespected_NotOverriddenByPosition()
    {
        // Arrange - primary organizer is at index 1, not index 0
        var @event = CreateValidEvent();
        var coOrgUserId = Guid.NewGuid();

        // Act - co-organizer first, primary organizer second
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)>
            {
                ("Co-Organizer", "coorg@test.com", null, coOrgUserId, false),
                ("Primary Organizer", "primary@test.com", null, (Guid?)null, true)
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(2);
        @event.OrganizerContacts[0].IsPrimary.Should().BeFalse("co-organizer should NOT become primary just because it's first");
        @event.OrganizerContacts[1].IsPrimary.Should().BeTrue("explicit primary flag should be respected");
    }

    [Fact]
    public void SetOrganizerContacts_NoPrimarySpecified_AllContactsHaveNoPrimary()
    {
        // Arrange - no contact marked as primary
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)>
            {
                ("Contact A", "a@test.com", null, (Guid?)null, false),
                ("Contact B", "b@test.com", null, (Guid?)null, false)
            });

        // Assert - zero primaries allowed: all organizers are equal
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[0].IsPrimary.Should().BeFalse("no forced primary when none specified");
        @event.OrganizerContacts[1].IsPrimary.Should().BeFalse("no forced primary when none specified");
    }

    [Fact]
    public void SetOrganizerContacts_AllContactsNotPrimary_GetPrimaryContactFallsBackToFirst()
    {
        // Arrange - no contact marked as primary
        var @event = CreateValidEvent();

        @event.SetOrganizerContacts(
            publishContact: true,
            contacts: new List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)>
            {
                ("Contact A", "a@test.com", null, (Guid?)null, false),
                ("Contact B", "b@test.com", null, (Guid?)null, false)
            });

        // Act & Assert - GetPrimaryContact still returns first contact as fallback for emails
        var primaryContact = @event.GetPrimaryContact();
        primaryContact.Should().NotBeNull("GetPrimaryContact falls back to first contact for email compatibility");
        primaryContact!.ContactName.Should().Be("Contact A");
        primaryContact.IsPrimary.Should().BeFalse("fallback contact is NOT marked primary in the data");
    }

    #endregion
}
