using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Tests for RefundPendingReviewEmailParams.
/// Architect-defined TDD list adapted to project convention (Validate() returning errors,
/// not throwing constructors — matches existing <see cref="RefundEmailParams"/> pattern).
/// </summary>
public class RefundPendingReviewEmailParamsTests
{
    private static RefundLineItemView SampleTicket() => new("Ticket", 50m, null, "requested");
    private static RefundLineItemView SampleAddOn() => new("Add-On", 25m, null, "requested");

    [Fact]
    public void TemplateName_ShouldBindToPendingReviewTemplate()
    {
        var sut = CreateValid();
        sut.TemplateName.Should().Be("template-refund-pending-review");
    }

    [Fact]
    public void TemplateName_ShouldMatchEmailTemplateContractConstant()
    {
        var sut = CreateValid();
        sut.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundPendingReview);
    }

    [Fact]
    public void Create_ShouldComputeRequestedTotalFromLineItems()
    {
        var sut = RefundPendingReviewEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Niro",
            userEmail: "n@example.com",
            registrationId: Guid.NewGuid(),
            refundRequestId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Cricket Match",
            eventStartDate: DateTime.UtcNow.AddDays(3),
            timeZoneId: "America/New_York",
            lineItems: new[] { SampleTicket(), SampleAddOn() },
            currency: "USD",
            requesterReason: null,
            requestedAt: DateTime.UtcNow,
            eventDetailsUrl: "https://lankaconnect.com/events/abc");

        sut.RequestedTotal.Should().Be(75m); // 50 + 25
    }

    [Fact]
    public void ToDictionary_ShouldRenderLineItemsHtml_WithEachLineRow()
    {
        var sut = CreateValid();
        sut.LineItems = new[] { SampleTicket(), SampleAddOn() };

        var dict = sut.ToDictionary();

        dict.Should().ContainKey("LineItemsHtml");
        var html = (string)dict["LineItemsHtml"];
        html.Should().Contain("Ticket");
        html.Should().Contain("Add-On");
        html.Should().Contain("50.00");
        html.Should().Contain("25.00");
    }

    [Fact]
    public void ToDictionary_ShouldExposeAllPlaceholdersUsedByTemplate()
    {
        var sut = CreateValid();
        sut.RequesterReason = "Cannot attend";

        var dict = sut.ToDictionary();

        // Lifecycle-specific placeholders
        dict.Should().ContainKey("LineItemsHtml");
        dict.Should().ContainKey("RequestedTotal");
        dict.Should().ContainKey("Currency");
        dict.Should().ContainKey("RequesterReason");
        dict.Should().ContainKey("HasRequesterReason");
        dict.Should().ContainKey("RequestedAt");

        // Common placeholders
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("EventDetailsUrl");
        dict.Should().ContainKey("SupportEmail");
        dict.Should().ContainKey("Year");

        // Organizer-contact placeholders (renders empty when not set)
        dict.Should().ContainKey("HasOrganizerContact");
        dict.Should().ContainKey("OrganizerContactsHtml");
    }

    [Fact]
    public void ToDictionary_HasRequesterReason_ShouldBeFalse_WhenReasonEmpty()
    {
        var sut = CreateValid();
        sut.RequesterReason = string.Empty;

        var dict = sut.ToDictionary();

        dict["HasRequesterReason"].Should().Be(false);
    }

    [Fact]
    public void ToDictionary_HasRequesterReason_ShouldBeTrue_WhenReasonNonEmpty()
    {
        var sut = CreateValid();
        sut.RequesterReason = "Cannot attend";

        var dict = sut.ToDictionary();

        dict["HasRequesterReason"].Should().Be(true);
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
        var sut = new RefundPendingReviewEmailParams(); // all defaults

        sut.Validate(out var errors).Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void WithOrganizerContacts_ShouldRenderContactsHtml()
    {
        var sut = CreateValid();
        var contacts = new[]
        {
            new OrganizerContactInfo("Bob Organizer", "bob@example.com", "555-1234", true)
        };

        sut.WithOrganizerContacts(contacts);

        sut.HasOrganizerContact.Should().BeTrue();
        sut.OrganizerContactName.Should().Be("Bob Organizer");
        sut.OrganizerContactsHtml.Should().NotBeNullOrEmpty();
    }

    private static RefundPendingReviewEmailParams CreateValid() =>
        RefundPendingReviewEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Niro",
            userEmail: "n@example.com",
            registrationId: Guid.NewGuid(),
            refundRequestId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Cricket Match",
            eventStartDate: DateTime.UtcNow.AddDays(3),
            timeZoneId: "America/New_York",
            lineItems: new[] { SampleTicket() },
            currency: "USD",
            requesterReason: null,
            requestedAt: DateTime.UtcNow,
            eventDetailsUrl: "https://lankaconnect.com/events/abc");
}
