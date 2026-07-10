using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
namespace LankaConnect.Modules.Communications.Contracts.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.148.W4.D13: Tests for RefundWithdrawnEmailParams.
///
/// Same shape as the W3 D7 lifecycle params tests — pins template-name binding,
/// LineItems HTML rendering, RequestedTotal computation from line sum, organizer-
/// contact attachment, and Validate() failure modes.
/// </summary>
public class RefundWithdrawnEmailParamsTests
{
    private static RefundLineItemView SampleTicket() => new("Ticket", 50m, null, "requested");
    private static RefundLineItemView SampleAddOn() => new("Add-On", 25m, null, "requested");

    [Fact]
    public void TemplateName_ShouldBindToWithdrawnTemplate()
    {
        var sut = CreateValid();
        sut.TemplateName.Should().Be("template-refund-withdrawn");
        sut.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundWithdrawn);
    }

    [Fact]
    public void Create_ShouldComputeRequestedTotalFromLineItems()
    {
        var sut = RefundWithdrawnEmailParams.Create(
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
            withdrawnAt: DateTime.UtcNow,
            eventDetailsUrl: "https://x");

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

        var dict = sut.ToDictionary();

        dict.Should().ContainKeys(
            "LineItemsHtml", "RequestedTotal", "Currency", "WithdrawnAt",
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
        var sut = new RefundWithdrawnEmailParams(); // all defaults
        sut.Validate(out var errors).Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllRequiredFieldsPresent()
    {
        var sut = CreateValid();
        sut.Validate(out var errors).Should().BeTrue();
        errors.Should().BeEmpty();
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

    private static RefundWithdrawnEmailParams CreateValid() =>
        RefundWithdrawnEmailParams.Create(
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
            withdrawnAt: DateTime.UtcNow,
            eventDetailsUrl: "https://x");
}
