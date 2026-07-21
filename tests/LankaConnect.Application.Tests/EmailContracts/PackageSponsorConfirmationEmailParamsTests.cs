using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using Xunit;

// Note: nested under .EmailContracts (NOT .Email) â€” a top-level Email namespace
// here would shadow the LankaConnect.Modules.Communications.Domain.ValueObjects.Email VO used
// across the test project (Email.Create(...) calls in other test files).
namespace LankaConnect.Application.Tests.EmailContracts;

/// <summary>
/// Phase 6A.157 â€” coverage for the forked email params. 6 cases per
/// architect lock: constructor + factory population, ToDictionary mapping,
/// validation, conditional rendering of included-tickets and perks blocks.
/// </summary>
public class PackageSponsorConfirmationEmailParamsTests
{
    private static PackageSponsorConfirmationEmailParams Make(
        int includedTickets = 3,
        IReadOnlyList<string>? perks = null,
        string? tier = "Gold")
    {
        return PackageSponsorConfirmationEmailParams.Create(
            sponsorName: "John Doe",
            sponsorEmail: "john@example.com",
            sponsorOrganization: "Acme Corp",
            eventTitle: "Annual Conference",
            packageNameSnapshot: "Gold Sponsor",
            packageTierSnapshot: tier,
            amountPaid: 500m,
            currency: "USD",
            paymentDate: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            paymentIntentId: "pi_test_pkg",
            includedTicketCount: includedTickets,
            perks: perks ?? new List<string> { "Logo on banner", "Booth at venue" },
            eventDetailsUrl: "https://example.com/events/abc"
        );
    }

    [Fact]
    public void Create_PopulatesAllFieldsFromArguments()
    {
        var p = Make();

        p.SponsorName.Should().Be("John Doe");
        p.SponsorEmail.Should().Be("john@example.com");
        p.SponsorOrganization.Should().Be("Acme Corp");
        p.EventTitle.Should().Be("Annual Conference");
        p.PackageNameSnapshot.Should().Be("Gold Sponsor");
        p.PackageTierSnapshot.Should().Be("Gold");
        p.AmountPaid.Should().Be(500m);
        p.Currency.Should().Be("USD");
        p.PaymentIntentId.Should().Be("pi_test_pkg");
        p.IncludedTicketCount.Should().Be(3);
        p.HasIncludedTickets.Should().BeTrue();
        p.HasPerks.Should().BeTrue();
        p.PerksHtml.Should().Contain("<li").And.Contain("Logo on banner").And.Contain("Booth at venue");
        p.EventDetailsUrl.Should().Be("https://example.com/events/abc");
        p.TemplateName.Should().Be("template-package-sponsor-confirmation");
        p.RecipientEmail.Should().Be("john@example.com");
        p.RecipientName.Should().Be("John Doe");
    }

    [Fact]
    public void ToDictionary_MapsAllFieldsForHandlebarsRendering()
    {
        var p = Make();
        var dict = p.ToDictionary();

        dict["SponsorName"].Should().Be("John Doe");
        dict["EventTitle"].Should().Be("Annual Conference");
        dict["PackageNameSnapshot"].Should().Be("Gold Sponsor");
        dict["PackageTierSnapshot"].Should().Be("Gold");
        dict["HasTier"].Should().Be(true);
        dict["AmountPaid"].Should().Be("500.00");
        dict["Currency"].Should().Be("USD");
        dict["PaymentDate"].Should().Be("June 1, 2026");
        dict["IncludedTicketCount"].Should().Be(3);
        dict["HasIncludedTickets"].Should().Be(true);
        dict["HasPerks"].Should().Be(true);
        dict["PerksHtml"].ToString().Should().Contain("<li");
        dict["HasOrganization"].Should().Be(true);
    }

    [Fact]
    public void HasIncludedTickets_FalseWhenCountIsZero()
    {
        var p = Make(includedTickets: 0);

        p.HasIncludedTickets.Should().BeFalse();
        p.IncludedTicketCount.Should().Be(0);
        p.ToDictionary()["HasIncludedTickets"].Should().Be(false);
    }

    [Fact]
    public void PerksHtml_EmptyWhenNoPerks()
    {
        var p = Make(perks: new List<string>());

        p.HasPerks.Should().BeFalse();
        p.PerksHtml.Should().BeEmpty();
        p.ToDictionary()["HasPerks"].Should().Be(false);
        p.ToDictionary()["PerksHtml"].Should().Be(string.Empty);
    }

    [Fact]
    public void PerksHtml_HtmlEncodesPotentiallyDangerousPerkContent()
    {
        // Defensive â€” perks come from organizer input; XSS prevention matters
        var p = Make(perks: new List<string> { "<script>alert(1)</script>", "Normal perk" });

        p.PerksHtml.Should().NotContain("<script>alert(1)</script>");
        p.PerksHtml.Should().Contain("&lt;script&gt;");
        p.PerksHtml.Should().Contain("Normal perk");
    }

    [Fact]
    public void Validate_ReturnsErrorsForMissingRequiredFields()
    {
        var p = new PackageSponsorConfirmationEmailParams
        {
            SponsorName = "",
            SponsorEmail = "",
            EventTitle = "",
            PackageNameSnapshot = "",
            AmountPaid = -1m,
            IncludedTicketCount = -1
        };

        var ok = p.Validate(out var errors);

        ok.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("SponsorName"));
        errors.Should().Contain(e => e.Contains("SponsorEmail"));
        errors.Should().Contain(e => e.Contains("EventTitle"));
        errors.Should().Contain(e => e.Contains("PackageNameSnapshot"));
        errors.Should().Contain(e => e.Contains("AmountPaid"));
        errors.Should().Contain(e => e.Contains("IncludedTicketCount"));
    }
}
