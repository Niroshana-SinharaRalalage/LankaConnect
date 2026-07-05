using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.148.D7: Tests for RefundRejectedEmailParams.
///
/// The customer-facing RejectionReason is a first-class field — Validate() fails
/// when it's empty. This guards against the 148.c body-stuffing antipattern where
/// the reason was buried inside a free-form text blob.
/// </summary>
public class RefundRejectedEmailParamsTests
{
    private static RefundLineItemView SampleLine() => new("Ticket", 50m, null, "requested");

    [Fact]
    public void TemplateName_ShouldBindToRejectedTemplate()
    {
        var sut = CreateValid();
        sut.TemplateName.Should().Be("template-refund-rejected");
        sut.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundRejected);
    }

    [Fact]
    public void ToDictionary_ShouldExposeRejectionReasonAsFirstClassField()
    {
        var sut = CreateValid();
        sut.RejectionReason = "Outside the cancellation policy window";

        var dict = sut.ToDictionary();

        dict.Should().ContainKey("RejectionReason");
        dict["RejectionReason"].Should().Be("Outside the cancellation policy window");
    }

    [Fact]
    public void ToDictionary_ShouldRenderLineItemsHtml_WithRequestedAmounts()
    {
        var sut = CreateValid();
        sut.LineItems = new[] { new RefundLineItemView("Sponsor", 100m, null, "requested") };

        var dict = sut.ToDictionary();

        var html = (string)dict["LineItemsHtml"];
        html.Should().Contain("Sponsor");
        html.Should().Contain("100.00");
    }

    [Fact]
    public void ToDictionary_ShouldExposeAllPlaceholdersUsedByTemplate()
    {
        var sut = CreateValid();

        var dict = sut.ToDictionary();

        dict.Should().ContainKeys(
            "LineItemsHtml", "RequestedTotal", "Currency", "RejectionReason", "RejectedAt",
            "UserName", "EventTitle", "EventDetailsUrl", "SupportEmail", "Year",
            "HasOrganizerContact", "OrganizerContactsHtml");
    }

    [Fact]
    public void Validate_ShouldFail_WhenRejectionReasonEmpty()
    {
        var sut = CreateValid();
        sut.RejectionReason = string.Empty;

        sut.Validate(out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RejectionReason", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldFail_WhenRejectionReasonWhitespace()
    {
        var sut = CreateValid();
        sut.RejectionReason = "   ";

        sut.Validate(out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RejectionReason", StringComparison.OrdinalIgnoreCase));
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
    public void Validate_ShouldPass_WhenAllRequiredFieldsPresent()
    {
        var sut = CreateValid();
        sut.Validate(out var errors).Should().BeTrue();
        errors.Should().BeEmpty();
    }

    private static RefundRejectedEmailParams CreateValid() =>
        RefundRejectedEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "Niro",
            userEmail: "n@example.com",
            registrationId: Guid.NewGuid(),
            refundRequestId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Cricket Match",
            eventStartDate: DateTime.UtcNow.AddDays(3),
            timeZoneId: "America/New_York",
            lineItems: new[] { SampleLine() },
            currency: "USD",
            rejectionReason: "Within event terms",
            rejectedAt: DateTime.UtcNow,
            eventDetailsUrl: "https://x");
}
