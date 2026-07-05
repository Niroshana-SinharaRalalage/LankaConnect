using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using Xunit;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.133 Email: Tests for NewsletterEmailParams organizer contact support.
/// Verifies that event-linked newsletters correctly display organizer contacts.
/// </summary>
public class NewsletterEmailParamsTests
{
    private NewsletterEmailParams CreateStandaloneParams()
    {
        return NewsletterEmailParams.CreateStandalone(
            recipientEmail: "subscriber@example.com",
            newsletterTitle: "Monthly Newsletter",
            newsletterContent: "Newsletter content here",
            dashboardUrl: "https://lankaconnect.com/dashboard");
    }

    private NewsletterEmailParams CreateEventLinkedParams()
    {
        return NewsletterEmailParams.CreateForEvent(
            recipientEmail: "subscriber@example.com",
            newsletterTitle: "New Event Newsletter",
            newsletterContent: "Check out this event",
            dashboardUrl: "https://lankaconnect.com/dashboard",
            eventId: Guid.NewGuid(),
            eventTitle: "Community Meetup",
            eventDateTime: "March 15, 2026 at 6:00 PM EDT",
            eventDescription: "A community gathering",
            eventLocation: "123 Main St, Cleveland, OH",
            eventDetailsUrl: "https://lankaconnect.com/events/123",
            hasSignUpLists: false,
            signUpListsUrl: "");
    }

    #region Template and Basic Tests

    [Fact]
    public void TemplateName_ShouldReturnNewsletterNotification()
    {
        var emailParams = CreateStandaloneParams();

        emailParams.TemplateName.Should().Be("template-newsletter-notification");
    }

    [Fact]
    public void StandaloneNewsletter_HasOrganizerContact_DefaultsFalse()
    {
        var emailParams = CreateStandaloneParams();

        emailParams.HasOrganizerContact.Should().BeFalse();
        emailParams.OrganizerContactsHtml.Should().BeEmpty();
        emailParams.OrganizerContactHeader.Should().Be("EVENT ORGANIZER");
    }

    [Fact]
    public void EventLinkedNewsletter_HasOrganizerContact_DefaultsFalse()
    {
        var emailParams = CreateEventLinkedParams();

        emailParams.HasOrganizerContact.Should().BeFalse();
        emailParams.OrganizerContactsHtml.Should().BeEmpty();
    }

    #endregion

    #region WithOrganizerContacts Tests

    [Fact]
    public void WithOrganizerContacts_SingleContact_SetsAllProperties()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "+1234567890", true)
        };

        emailParams.WithOrganizerContacts(contacts);

        emailParams.HasOrganizerContact.Should().BeTrue();
        emailParams.OrganizerContactName.Should().Be("John Smith");
        emailParams.OrganizerContactEmail.Should().Be("john@example.com");
        emailParams.OrganizerContactPhone.Should().Be("+1234567890");
        emailParams.OrganizerContactsHtml.Should().Contain("John Smith");
        emailParams.OrganizerContactsHtml.Should().Contain("john@example.com");
        emailParams.OrganizerContactsHtml.Should().Contain("+1234567890");
        emailParams.OrganizerContactHeader.Should().Be("EVENT ORGANIZER");
    }

    [Fact]
    public void WithOrganizerContacts_MultipleContacts_SetsHeaderPlural()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", null, true),
            new("Jane Doe", "jane@example.com", "+9876543210", false)
        };

        emailParams.WithOrganizerContacts(contacts);

        emailParams.HasOrganizerContact.Should().BeTrue();
        emailParams.OrganizerContactName.Should().Be("John Smith"); // Primary contact
        emailParams.OrganizerContactsHtml.Should().Contain("John Smith");
        emailParams.OrganizerContactsHtml.Should().Contain("Jane Doe");
        emailParams.OrganizerContactHeader.Should().Be("EVENT ORGANIZERS");
    }

    [Fact]
    public void WithOrganizerContacts_EmptyList_LeavesHasOrganizerContactFalse()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>();

        emailParams.WithOrganizerContacts(contacts);

        emailParams.HasOrganizerContact.Should().BeFalse();
        emailParams.OrganizerContactsHtml.Should().BeEmpty();
        emailParams.OrganizerContactHeader.Should().Be("EVENT ORGANIZER");
    }

    [Fact]
    public void WithOrganizerContacts_NoPrimary_UseFirstContact()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>
        {
            new("First Contact", "first@example.com", null, false),
            new("Second Contact", "second@example.com", null, false)
        };

        emailParams.WithOrganizerContacts(contacts);

        emailParams.OrganizerContactName.Should().Be("First Contact");
        emailParams.OrganizerContactEmail.Should().Be("first@example.com");
    }

    [Fact]
    public void WithOrganizerContacts_ReturnsSelfForFluentChaining()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", null, true)
        };

        var result = emailParams.WithOrganizerContacts(contacts);

        result.Should().BeSameAs(emailParams);
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_ContainsAllOrganizerContactKeys()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "+1234567890", true)
        };
        emailParams.WithOrganizerContacts(contacts);

        var dict = emailParams.ToDictionary();

        dict.Should().ContainKey("HasOrganizerContact");
        dict.Should().ContainKey("OrganizerContactName");
        dict.Should().ContainKey("OrganizerContactEmail");
        dict.Should().ContainKey("OrganizerContactPhone");
        dict.Should().ContainKey("OrganizerContactsHtml");
        dict.Should().ContainKey("OrganizerContactHeader");
    }

    [Fact]
    public void ToDictionary_OrganizerContactValues_MatchProperties()
    {
        var emailParams = CreateEventLinkedParams();
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "+1234567890", true)
        };
        emailParams.WithOrganizerContacts(contacts);

        var dict = emailParams.ToDictionary();

        dict["HasOrganizerContact"].Should().Be(true);
        dict["OrganizerContactName"].Should().Be("John Smith");
        dict["OrganizerContactEmail"].Should().Be("john@example.com");
        dict["OrganizerContactPhone"].Should().Be("+1234567890");
        ((string)dict["OrganizerContactsHtml"]).Should().Contain("John Smith");
        dict["OrganizerContactHeader"].Should().Be("EVENT ORGANIZER");
    }

    [Fact]
    public void ToDictionary_StandaloneNewsletter_HasOrganizerContactIsFalse()
    {
        var emailParams = CreateStandaloneParams();

        var dict = emailParams.ToDictionary();

        dict["HasOrganizerContact"].Should().Be(false);
        dict["OrganizerContactsHtml"].Should().Be(string.Empty);
    }

    [Fact]
    public void ToDictionary_ContainsAllBaseKeys()
    {
        var emailParams = CreateEventLinkedParams();

        var dict = emailParams.ToDictionary();

        // Verify all keys are present (base + organizer = 22 keys)
        dict.Should().ContainKey("NewsletterTitle");
        dict.Should().ContainKey("NewsletterContent");
        dict.Should().ContainKey("DashboardUrl");
        dict.Should().ContainKey("EventId");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventLocation");
        dict.Should().ContainKey("UnsubscribeLink");
        dict.Should().ContainKey("HasOrganizerContact");
        dict.Should().ContainKey("OrganizerContactsHtml");
    }

    #endregion
}
