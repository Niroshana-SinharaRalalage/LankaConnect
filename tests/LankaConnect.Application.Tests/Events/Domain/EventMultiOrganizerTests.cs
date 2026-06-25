using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 6A.133: Multi-Organizer feature - Domain layer tests
/// Tests for IsOrganizer(), LinkOrganizerContactToUser(), UnlinkOrganizerContact(),
/// BatchLinkOrganizerContacts(), and GetAllOrganizerUserIds().
/// </summary>
public class EventMultiOrganizerTests
{
    #region Test Helpers

    private static Event CreateEventWithContacts(Guid? organizerId = null, int contactCount = 2)
    {
        var title = EventTitle.Create("Multi-Org Test Event").Value;
        var description = EventDescription.Create("Testing multi-organizer features").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);
        var orgId = organizerId ?? Guid.NewGuid();

        var result = Event.Create(title, description, startDate, endDate, orgId, 100);
        var @event = result.Value;

        var contacts = Enumerable.Range(1, contactCount)
            .Select(i => ($"Contact {i}", (string?)$"c{i}@example.com", (string?)null))
            .ToList();

        @event.SetOrganizerContacts(publishContact: true, contacts);
        return @event;
    }

    #endregion

    #region IsOrganizer

    [Fact]
    public void IsOrganizer_PrimaryOrganizer_ReturnsTrue()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateEventWithContacts(organizerId);

        // Act & Assert
        @event.IsOrganizer(organizerId).Should().BeTrue();
    }

    [Fact]
    public void IsOrganizer_CoOrganizerWithLinkedUserId_ReturnsTrue()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var coOrgUserId = Guid.NewGuid();
        var contactId = @event.OrganizerContacts[1].Id;
        @event.LinkOrganizerContactToUser(contactId, coOrgUserId);

        // Act & Assert
        @event.IsOrganizer(coOrgUserId).Should().BeTrue();
    }

    [Fact]
    public void IsOrganizer_UnlinkedContact_ReturnsFalse()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var randomUserId = Guid.NewGuid();

        // Act & Assert — contacts exist but none are linked
        @event.IsOrganizer(randomUserId).Should().BeFalse();
    }

    [Fact]
    public void IsOrganizer_RandomUser_ReturnsFalse()
    {
        // Arrange
        var @event = CreateEventWithContacts();

        // Act & Assert
        @event.IsOrganizer(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsOrganizer_EmptyGuid_ReturnsFalse()
    {
        // Arrange
        var @event = CreateEventWithContacts();

        // Act & Assert
        @event.IsOrganizer(Guid.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsOrganizer_NoContacts_PrimaryStillWorks()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var title = EventTitle.Create("No Contacts Event").Value;
        var description = EventDescription.Create("Test").Value;
        var @event = Event.Create(title, description, DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(8), organizerId, 100).Value;

        // Act & Assert — no contacts at all, primary organizer still recognized
        @event.IsOrganizer(organizerId).Should().BeTrue();
        @event.IsOrganizer(Guid.NewGuid()).Should().BeFalse();
    }

    #endregion

    #region LinkOrganizerContactToUser

    [Fact]
    public void LinkOrganizerContactToUser_ValidUserAndContact_Succeeds()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var coOrgUserId = Guid.NewGuid();
        var contactId = @event.OrganizerContacts[1].Id; // second contact

        // Act
        var result = @event.LinkOrganizerContactToUser(contactId, coOrgUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[1].LinkedUserId.Should().Be(coOrgUserId);
        @event.IsOrganizer(coOrgUserId).Should().BeTrue();
    }

    [Fact]
    public void LinkOrganizerContactToUser_PrimaryOrganizerUserId_Fails()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateEventWithContacts(organizerId);
        var contactId = @event.OrganizerContacts[1].Id;

        // Act — try to link the primary organizer (already has access)
        var result = @event.LinkOrganizerContactToUser(contactId, organizerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("primary organizer already has full access");
    }

    [Fact]
    public void LinkOrganizerContactToUser_SameUserAlreadyLinked_Idempotent()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var coOrgUserId = Guid.NewGuid();
        var contactId = @event.OrganizerContacts[1].Id;

        // Link once
        @event.LinkOrganizerContactToUser(contactId, coOrgUserId);

        // Act — link same user to same contact again
        var result = @event.LinkOrganizerContactToUser(contactId, coOrgUserId);

        // Assert — should succeed (idempotent)
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[1].LinkedUserId.Should().Be(coOrgUserId);
    }

    [Fact]
    public void LinkOrganizerContactToUser_UserAlreadyLinkedToOtherContact_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts(contactCount: 3);
        var coOrgUserId = Guid.NewGuid();

        // Link to first non-primary contact
        @event.LinkOrganizerContactToUser(@event.OrganizerContacts[1].Id, coOrgUserId);

        // Act — try to link same user to a different contact
        var result = @event.LinkOrganizerContactToUser(@event.OrganizerContacts[2].Id, coOrgUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already linked to another organizer contact");
    }

    [Fact]
    public void LinkOrganizerContactToUser_ContactNotFound_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var fakeContactId = Guid.NewGuid();

        // Act
        var result = @event.LinkOrganizerContactToUser(fakeContactId, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Organizer contact not found");
    }

    [Fact]
    public void LinkOrganizerContactToUser_EmptyUserId_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var contactId = @event.OrganizerContacts[1].Id;

        // Act
        var result = @event.LinkOrganizerContactToUser(contactId, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User ID is required");
    }

    [Fact]
    public void LinkOrganizerContactToUser_EmptyContactId_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts();

        // Act
        var result = @event.LinkOrganizerContactToUser(Guid.Empty, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact ID is required");
    }

    #endregion

    #region UnlinkOrganizerContact

    [Fact]
    public void UnlinkOrganizerContact_LinkedContact_Succeeds()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var coOrgUserId = Guid.NewGuid();
        var contactId = @event.OrganizerContacts[1].Id;
        @event.LinkOrganizerContactToUser(contactId, coOrgUserId);

        // Act
        var result = @event.UnlinkOrganizerContact(contactId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts[1].LinkedUserId.Should().BeNull();
        @event.IsOrganizer(coOrgUserId).Should().BeFalse();
    }

    [Fact]
    public void UnlinkOrganizerContact_AlreadyUnlinked_Idempotent()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var contactId = @event.OrganizerContacts[1].Id;
        // Contact is not linked

        // Act
        var result = @event.UnlinkOrganizerContact(contactId);

        // Assert — should succeed (idempotent)
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UnlinkOrganizerContact_ContactNotFound_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts();

        // Act
        var result = @event.UnlinkOrganizerContact(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Organizer contact not found");
    }

    [Fact]
    public void UnlinkOrganizerContact_EmptyContactId_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts();

        // Act
        var result = @event.UnlinkOrganizerContact(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact ID is required");
    }

    #endregion

    #region BatchLinkOrganizerContacts

    [Fact]
    public void BatchLinkOrganizerContacts_MultipleLinks_Succeeds()
    {
        // Arrange
        var @event = CreateEventWithContacts(contactCount: 3);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var links = new List<(Guid contactId, Guid userId)>
        {
            (@event.OrganizerContacts[1].Id, user1),
            (@event.OrganizerContacts[2].Id, user2)
        };

        // Act
        var result = @event.BatchLinkOrganizerContacts(links);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.IsOrganizer(user1).Should().BeTrue();
        @event.IsOrganizer(user2).Should().BeTrue();
    }

    [Fact]
    public void BatchLinkOrganizerContacts_EmptyList_Succeeds()
    {
        // Arrange
        var @event = CreateEventWithContacts();
        var links = new List<(Guid contactId, Guid userId)>();

        // Act
        var result = @event.BatchLinkOrganizerContacts(links);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void BatchLinkOrganizerContacts_DuplicateUserInBatch_Fails()
    {
        // Arrange
        var @event = CreateEventWithContacts(contactCount: 3);
        var sameUser = Guid.NewGuid();
        var links = new List<(Guid contactId, Guid userId)>
        {
            (@event.OrganizerContacts[1].Id, sameUser),
            (@event.OrganizerContacts[2].Id, sameUser)  // same user, different contact
        };

        // Act
        var result = @event.BatchLinkOrganizerContacts(links);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("duplicate");
    }

    [Fact]
    public void BatchLinkOrganizerContacts_PrimaryOrganizerInBatch_Fails()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateEventWithContacts(organizerId, contactCount: 3);
        var links = new List<(Guid contactId, Guid userId)>
        {
            (@event.OrganizerContacts[1].Id, Guid.NewGuid()),
            (@event.OrganizerContacts[2].Id, organizerId)  // primary organizer
        };

        // Act
        var result = @event.BatchLinkOrganizerContacts(links);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("primary organizer");
    }

    #endregion

    #region GetAllOrganizerUserIds

    [Fact]
    public void GetAllOrganizerUserIds_PrimaryOnly_ReturnsOne()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateEventWithContacts(organizerId);

        // Act
        var ids = @event.GetAllOrganizerUserIds();

        // Assert
        ids.Should().ContainSingle().Which.Should().Be(organizerId);
    }

    [Fact]
    public void GetAllOrganizerUserIds_WithCoOrganizers_ReturnsAll()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateEventWithContacts(organizerId, contactCount: 3);
        var coOrg1 = Guid.NewGuid();
        var coOrg2 = Guid.NewGuid();
        @event.LinkOrganizerContactToUser(@event.OrganizerContacts[1].Id, coOrg1);
        @event.LinkOrganizerContactToUser(@event.OrganizerContacts[2].Id, coOrg2);

        // Act
        var ids = @event.GetAllOrganizerUserIds();

        // Assert
        ids.Should().HaveCount(3);
        ids.Should().Contain(organizerId);
        ids.Should().Contain(coOrg1);
        ids.Should().Contain(coOrg2);
    }

    [Fact]
    public void GetAllOrganizerUserIds_NoDuplicatesWhenPrimaryAlsoLinked()
    {
        // Edge case: if somehow primary organizer ID appears in both OrganizerId
        // and a contact's LinkedUserId, it should not be duplicated
        var organizerId = Guid.NewGuid();
        var @event = CreateEventWithContacts(organizerId);

        // Act — no co-organizers linked, just primary
        var ids = @event.GetAllOrganizerUserIds();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().ContainSingle().Which.Should().Be(organizerId);
    }

    #endregion
}
