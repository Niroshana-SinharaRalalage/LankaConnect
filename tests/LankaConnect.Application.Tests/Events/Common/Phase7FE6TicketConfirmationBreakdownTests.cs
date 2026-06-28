using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Shared.Email.Contracts;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 7F-E.6.B (architect-approved 2026-05-04): the paid-event template body
/// migration in 7F-E.3 added <c>{{{RegistrationBreakdownHtml}}}</c> to
/// <c>template-paid-event-registration-confirmation-with-ticket.html</c>, but
/// <see cref="TicketConfirmationEmailParams"/> never gained the matching field —
/// so the producer side never populated it and the token rendered as a literal
/// string in the user's inbox. Operator caught this on staging 2026-05-04.
///
/// Architect-approved fix: add <c>RegistrationBreakdownHtml</c> field +
/// <c>WithRegistrationBreakdownHtml(string?)</c> fluent setter. The setter takes
/// a pre-rendered HTML fragment because <c>LankaConnect.Shared</c> can't reference
/// <c>LankaConnect.Application</c> without inverting the project graph — the
/// <see cref="RegistrationBreakdownEmailRenderer.Render"/> call lives in the
/// handler / command-handler layer.
/// </summary>
public class Phase7FE6TicketConfirmationBreakdownTests
{
    private static RegistrationBreakdown SampleBreakdown() =>
        new(
            Rows: new[] {
                new RegistrationBreakdownRow(
                    TierName: "VIP", Count: 4,
                    Age: BreakdownPair.NotCaptured("Adult", "Child"),
                    Gender: BreakdownPair.NotCaptured("Male", "Female")),
                new RegistrationBreakdownRow(
                    TierName: "Standard", Count: 4,
                    Age: BreakdownPair.NotCaptured("Adult", "Child"),
                    Gender: BreakdownPair.NotCaptured("Male", "Female")),
            },
            TotalAttendees: 8,
            Mode: RegistrationMode.HeadCountByAgeAndGender,
            IsTiered: true,
            Totals: new RegistrationBreakdownTotals(
                Age: BreakdownPair.CapturedAge(adults: 4, children: 4),
                Gender: BreakdownPair.CapturedGender(males: 4, females: 4)));

    private static TicketConfirmationEmailParams BuildParams() =>
        TicketConfirmationEmailParams.Create(
            eventId: Guid.NewGuid(),
            registrationId: Guid.NewGuid(),
            userName: "Niro",
            contactEmail: "test@example.com",
            eventTitle: "Test event",
            eventStartDate: new DateTime(2026, 5, 14, 18, 0, 0, DateTimeKind.Utc),
            eventStartTime: "2:00 PM",
            eventLocation: "Test Hall",
            eventDetailsUrl: "https://example.com/events/x",
            amountPaid: 320m,
            paymentIntentId: "pi_test",
            paymentDate: new DateTime(2026, 5, 4, 16, 0, 0, DateTimeKind.Utc),
            quantity: 8);

    [Fact]
    public void WithRegistrationBreakdownHtml_StoresPreRenderedFragment()
    {
        var p = BuildParams();
        var html = RegistrationBreakdownEmailRenderer.Render(SampleBreakdown(), "Niro");

        p.WithRegistrationBreakdownHtml(html);

        p.RegistrationBreakdownHtml.Should().NotBeNullOrEmpty();
        p.RegistrationBreakdownHtml.Should().Contain("Total attendees");
        p.RegistrationBreakdownHtml.Should().Contain("Adult/Child");
        p.RegistrationBreakdownHtml.Should().Contain("Male/Female");
    }

    [Fact]
    public void WithRegistrationBreakdownHtml_NullInput_SetsEmptyStringDefensive()
    {
        var p = BuildParams();

        p.WithRegistrationBreakdownHtml(null);

        p.RegistrationBreakdownHtml.Should().Be(string.Empty,
            "null input means no card to render — empty-string keeps the Handlebars " +
            "triple-stache from emitting 'null' literally");
    }

    [Fact]
    public void ToDictionary_IncludesRegistrationBreakdownHtmlKey()
    {
        var p = BuildParams();
        var html = RegistrationBreakdownEmailRenderer.Render(SampleBreakdown(), "Niro");
        p.WithRegistrationBreakdownHtml(html);

        var dict = p.ToDictionary();

        dict.Should().ContainKey("RegistrationBreakdownHtml");
        dict["RegistrationBreakdownHtml"].ToString().Should().Contain("Total attendees");
    }
}
