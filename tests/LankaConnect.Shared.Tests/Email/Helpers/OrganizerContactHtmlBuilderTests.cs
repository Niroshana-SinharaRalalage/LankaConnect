using FluentAssertions;
using LankaConnect.Shared.Email.Helpers;
using Xunit;

namespace LankaConnect.Shared.Tests.Email.Helpers;

/// <summary>
/// Phase 6A.133 Email: Tests for OrganizerContactHtmlBuilder.
/// Verifies HTML generation for multi-contact email display.
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
    public void BuildContactListHtml_SingleContact_ReturnsFormattedHtml()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "+1234567890", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().Contain("john@example.com");
        result.Should().Contain("+1234567890");
        result.Should().Contain("mailto:john@example.com");
        // Single contact should NOT have a divider
        result.Should().NotContain("border-bottom");
    }

    [Fact]
    public void BuildContactListHtml_MultipleContacts_ReturnsAllWithDividers()
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
        // Should have dividers between contacts (2 dividers for 3 contacts)
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
        // Primary badge should use crimson color
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
    public void BuildContactListHtml_NoEmail_OmitsEmailLine()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", null, "+1234567890", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().Contain("+1234567890");
        result.Should().NotContain("mailto:");
    }

    [Fact]
    public void BuildContactListHtml_NoPhone_OmitsPhoneLine()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", null, false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().Contain("John Smith");
        result.Should().Contain("john@example.com");
        result.Should().NotContain("tel:");
    }

    [Fact]
    public void BuildContactListHtml_EmptyEmail_OmitsEmailLine()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "", "+1234567890", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().NotContain("mailto:");
    }

    [Fact]
    public void BuildContactListHtml_EmptyPhone_OmitsPhoneLine()
    {
        var contacts = new List<OrganizerContactInfo>
        {
            new("John Smith", "john@example.com", "", false)
        };

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().NotContain("tel:");
    }

    [Fact]
    public void BuildContactListHtml_EmptyList_ReturnsEmptyString()
    {
        var contacts = new List<OrganizerContactInfo>();

        var result = OrganizerContactHtmlBuilder.BuildContactListHtml(contacts);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildContactListHtml_ContactWithOnlyName_ReturnsNameOnly()
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
