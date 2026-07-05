using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using Xunit;

namespace LankaConnect.Shared.Tests.Email.Helpers;

/// <summary>
/// Phase 6A.133 Email: Tests for OrganizerContactHtmlBuilder.
/// Verifies HTML table generation for multi-contact email display.
/// </summary>
public class OrganizerContactHtmlBuilderTests
{
    #region BuildHeaderText Tests

    [Fact]
    public void BuildHeaderText_SingleContact_ReturnsEventOrganizer()
    {
        var result = OrganizerContactHtmlBuilder.BuildHeaderText(1);

        result.Should().Be("EVENT ORGANIZER");
    }

    [Fact]
    public void BuildHeaderText_MultipleContacts_ReturnsEventOrganizers()
    {
        var result = OrganizerContactHtmlBuilder.BuildHeaderText(3);

        result.Should().Be("EVENT ORGANIZERS");
    }

    [Fact]
    public void BuildHeaderText_ZeroContacts_ReturnsEventOrganizer()
    {
        var result = OrganizerContactHtmlBuilder.BuildHeaderText(0);

        result.Should().Be("EVENT ORGANIZER");
    }

    #endregion

    #region BuildContactListHtml Tests

    [Fact]
    public void BuildContactListHtml_SingleContact_ReturnsTableWithHeaders()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "+1234567890", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        // Should be a table with header row
        result.Should().Contain("<table");
        result.Should().Contain("</table>");
        result.Should().Contain(">Name</td>");
        result.Should().Contain(">Email</td>");
        result.Should().Contain(">Phone</td>");
        // Contact data
        result.Should().Contain("John Smith");
        result.Should().Contain("john@example.com");
        result.Should().Contain("+1234567890");
        result.Should().Contain("mailto:john@example.com");
    }

    [Fact]
    public void BuildContactListHtml_MultipleContacts_ReturnsAllInTable()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "+1111111111", false),
            new("Jane Doe", "jane@example.com", "+2222222222", false),
            new("Bob Wilson", "bob@example.com", null, false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().Contain("Jane Doe");
        result.Should().Contain("Bob Wilson");
        result.Should().Contain("john@example.com");
        result.Should().Contain("jane@example.com");
        result.Should().Contain("bob@example.com");
        // Row dividers between contacts
        result.Should().Contain("border-bottom");
    }

    [Fact]
    public void BuildContactListHtml_PrimaryContact_ShowsPrimaryBadge()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", null, true),
            new("Jane Doe", "jane@example.com", null, false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("Primary");
        result.Should().Contain("#9f1239");
    }

    [Fact]
    public void BuildContactListHtml_NoPrimaryContacts_NoPrimaryBadge()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", null, false),
            new("Jane Doe", "jane@example.com", null, false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().NotContain("Primary");
    }

    [Fact]
    public void BuildContactListHtml_NoEmail_ShowsDash()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", null, "+1234567890", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().Contain("+1234567890");
        result.Should().NotContain("mailto:");
        // Should show em-dash for missing email
        result.Should().Contain("&mdash;");
    }

    [Fact]
    public void BuildContactListHtml_NoPhone_ShowsDash()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", null, false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().Contain("john@example.com");
        result.Should().NotContain("tel:");
        // Should show em-dash for missing phone
        result.Should().Contain("&mdash;");
    }

    [Fact]
    public void BuildContactListHtml_EmptyEmail_ShowsDash()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "", "+1234567890", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().NotContain("mailto:");
        result.Should().Contain("&mdash;");
    }

    [Fact]
    public void BuildContactListHtml_EmptyPhone_ShowsDash()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().NotContain("tel:");
        result.Should().Contain("&mdash;");
    }

    [Fact]
    public void BuildContactListHtml_EmptyList_ReturnsEmptyString()
    {
        var contacts = new List<OrganizerContactInfo>();

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildContactListHtml_ContactWithOnlyName_ShowsDashesForEmailAndPhone()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", null, null, false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().NotContain("mailto:");
        result.Should().NotContain("tel:");
    }

    #endregion
}
