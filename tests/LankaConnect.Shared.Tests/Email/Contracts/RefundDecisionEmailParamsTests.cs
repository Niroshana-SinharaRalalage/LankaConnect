using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Tests for RefundDecisionEmailParams.
///
/// Operator UAT (E3) showed that without a per-line decision breakdown in the email,
/// attendees who got mixed approvals/rejections (e.g., 2 sponsors approved + 4 add-ons
/// declined) couldn't tell what the organizer actually decided. These tests assert the
/// LineItemsHtml carries that breakdown.
/// </summary>
public class RefundDecisionEmailParamsTests
{
    private static RefundLineItemView Approved(string type, decimal req, decimal app) =>
        new(type, req, app, "approved");
    private static RefundLineItemView Rejected(string type, decimal req) =>
        new(type, req, 0m, "rejected");

    [Fact]
    public void TemplateName_ShouldBindToDecisionTemplate()
    {
        var sut = CreateValid();
        sut.TemplateName.Should().Be("template-refund-decision");
        sut.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundDecision);
    }

    [Fact]
    public void Create_ShouldComputeApprovedAndRequestedTotalsFromLineItems()
    {
        var lines = new[]
        {
            Approved("Sponsor", 125m, 125m),
            Approved("Sponsor", 100m, 100m),
            Approved("Ticket", 15m, 15m),
            Approved("Add-On", 15m, 15m),
            Rejected("Add-On", 7m),
            Rejected("Add-On", 7m),
        };

        var sut = RefundDecisionEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Niro",
            userEmail: "n@example.com",
            registrationId: Guid.NewGuid(),
            refundRequestId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Cricket Match",
            eventStartDate: DateTime.UtcNow.AddDays(3),
            timeZoneId: "America/New_York",
            lineItems: lines,
            currency: "USD",
            isOrganizerInitiated: false,
            decidedAt: DateTime.UtcNow,
            eventDetailsUrl: "https://x");

        sut.RequestedTotal.Should().Be(269m); // operator's UAT scenario
        sut.ApprovedTotal.Should().Be(255m); // operator's UAT scenario — caught E3
    }

    [Fact]
    public void ToDictionary_ShouldRenderLineItemsHtml_WithBothApprovedAndRejectedRows()
    {
        var sut = CreateValid();
        sut.LineItems = new[]
        {
            Approved("Sponsor", 125m, 125m),
            Rejected("Add-On", 7m)
        };

        var dict = sut.ToDictionary();

        var html = (string)dict["LineItemsHtml"];
        html.Should().Contain("Sponsor");
        html.Should().Contain("Add-On");
        html.Should().Contain("125.00");
        // Approved row carries the approved amount; rejected row shows "Declined"
        html.Should().Contain("Approved", because: "approved lines should be labeled");
        html.Should().Contain("Declined", because: "rejected lines should be labeled");
    }

    [Fact]
    public void ToDictionary_IsOrganizerInitiated_ShouldFlowThrough()
    {
        var sut = CreateValid();
        sut.IsOrganizerInitiated = true;

        var dict = sut.ToDictionary();

        dict.Should().ContainKey("IsOrganizerInitiated");
        dict["IsOrganizerInitiated"].Should().Be(true);
    }

    [Fact]
    public void ToDictionary_ShouldExposeAllPlaceholdersUsedByTemplate()
    {
        var sut = CreateValid();

        var dict = sut.ToDictionary();

        dict.Should().ContainKeys(
            "LineItemsHtml", "ApprovedTotal", "RequestedTotal", "Currency",
            "IsOrganizerInitiated", "DecidedAt",
            "UserName", "EventTitle", "EventDetailsUrl", "SupportEmail", "Year",
            "HasOrganizerContact", "OrganizerContactsHtml");
    }

    [Fact]
    public void Validate_ShouldFail_WhenLineItemsEmpty()
    {
        var sut = CreateValid();
        sut.LineItems = Array.Empty<RefundLineItemView>();

        sut.Validate(out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("LineItems", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldFail_WhenRequiredCoreFieldsMissing()
    {
        var sut = new RefundDecisionEmailParams();

        sut.Validate(out var errors).Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenApprovedTotalIsZero_AndRequestedTotalPositive()
    {
        // All-rejected request can still legitimately fire a decision email (edge case)
        // — but in practice, the handler will route to the rejected handler. Defensive
        // assertion: ApprovedTotal == 0 is allowed by Validate (not a domain invariant).
        var sut = CreateValid();
        sut.ApprovedTotal = 0m;
        sut.RequestedTotal = 50m;

        sut.Validate(out var errors).Should().BeTrue();
    }

    private static RefundDecisionEmailParams CreateValid() =>
        RefundDecisionEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Niro",
            userEmail: "n@example.com",
            registrationId: Guid.NewGuid(),
            refundRequestId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Cricket Match",
            eventStartDate: DateTime.UtcNow.AddDays(3),
            timeZoneId: "America/New_York",
            lineItems: new[] { Approved("Sponsor", 125m, 125m) },
            currency: "USD",
            isOrganizerInitiated: false,
            decidedAt: DateTime.UtcNow,
            eventDetailsUrl: "https://x");
}
