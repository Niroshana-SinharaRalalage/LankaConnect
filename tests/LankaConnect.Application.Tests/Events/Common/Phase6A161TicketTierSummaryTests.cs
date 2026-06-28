using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 6A.161 — <see cref="EventAttendeeDto.TicketTierSummary"/> computes the registration-level
/// ticket-tier label shown in the Attendees tab and exports. Tier is stored per-attendee
/// (a registration may mix tiers); the summary collapses that to a single scannable string.
/// </summary>
public class Phase6A161TicketTierSummaryTests
{
    private static AttendeeDetailsDto Attendee(string name, string? tier) => new()
    {
        Name = name,
        AgeCategory = AgeCategory.Adult,
        Gender = Gender.Male,
        TicketTierName = tier
    };

    [Fact]
    public void TicketTierSummary_WhenAllAttendeesShareOneTier_ReturnsThatSingleName()
    {
        var dto = new EventAttendeeDto
        {
            Attendees = new() { Attendee("A", "VIP"), Attendee("B", "VIP") }
        };

        dto.TicketTierSummary.Should().Be("VIP");
    }

    [Fact]
    public void TicketTierSummary_WhenAttendeesSpanMultipleTiers_ReturnsDistinctNamesJoined()
    {
        var dto = new EventAttendeeDto
        {
            Attendees = new() { Attendee("A", "VIP"), Attendee("B", "General"), Attendee("C", "General") }
        };

        // Distinct, first-appearance order; "General" not repeated.
        dto.TicketTierSummary.Should().Be("VIP, General");
    }

    [Fact]
    public void TicketTierSummary_WhenNoAttendeeHasTier_ReturnsEmDash()
    {
        var dto = new EventAttendeeDto
        {
            Attendees = new() { Attendee("A", null), Attendee("B", "  ") }
        };

        dto.TicketTierSummary.Should().Be("—");
    }

    [Fact]
    public void TicketTierSummary_WhenAttendeesListEmpty_ReturnsEmDash()
    {
        // Mode B head-count registrations carry no per-attendee rows.
        var dto = new EventAttendeeDto { Attendees = new() };

        dto.TicketTierSummary.Should().Be("—");
    }

    [Fact]
    public void TicketTierSummary_WhenSomeAttendeesUntieredAndOthersTiered_IgnoresBlanks()
    {
        var dto = new EventAttendeeDto
        {
            Attendees = new() { Attendee("A", "Gold"), Attendee("B", null) }
        };

        dto.TicketTierSummary.Should().Be("Gold");
    }

    [Fact]
    public void TicketTierSummary_TrimsWhitespaceAndDedupesCaseSensitively()
    {
        var dto = new EventAttendeeDto
        {
            Attendees = new() { Attendee("A", " VIP "), Attendee("B", "VIP") }
        };

        dto.TicketTierSummary.Should().Be("VIP");
    }
}
