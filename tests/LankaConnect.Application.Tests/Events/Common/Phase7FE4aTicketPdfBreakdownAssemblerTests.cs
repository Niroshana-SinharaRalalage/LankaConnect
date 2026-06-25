using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 7F-E.4a (architect-approved 2026-05-01): the PDF ticket renderer must show
/// the same per-tier × demographic breakdown that the email and event-detail card now
/// show. This test pins the dispatch logic in
/// <see cref="TicketPdfRegistrationBreakdownAssembler"/> — pure data-assembly so it's
/// testable in isolation; the rendered PDF is verified by staging API smoke.
///
/// Coverage:
///   - Mode A (DetailedAttendees) → FromAttendees, single row, no tier
///   - Mode A tiered → FromAttendees, one row per tier
///   - Mode B1 (HeadCountOnly) → FromHeadCount, single row, both axes NotCaptured
///   - Mode B2 (HeadCountByAge) → FromHeadCount, age captured, gender NotCaptured
///   - Mode B3 (HeadCountByGender) → FromHeadCount, gender captured, age NotCaptured
///   - Mode B4 (HeadCountByAgeAndGender) → FromHeadCount, both captured
///   - Defensive: null registration → null
/// </summary>
public class Phase7FE4aTicketPdfBreakdownAssemblerTests
{
    private static RegistrationContact Contact() =>
        RegistrationContact.Create("test@example.com", "555-0100", null).Value;

    private static AttendeeDetails Attendee(string name, AgeCategory age = AgeCategory.Adult,
        Gender? gender = null, Guid? tierId = null, string? tierName = null) =>
        AttendeeDetails.Create(name, age, gender, ticketTierId: tierId, ticketTierName: tierName).Value;

    private static Money Zero() => new(0, Currency.USD);

    // ──────────────────────────────────────────────────────────────────────
    //  Mode A — DetailedAttendees
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void NullRegistration_ReturnsNull()
    {
        TicketPdfRegistrationBreakdownAssembler.Build(null).Should().BeNull();
    }

    [Fact]
    public void ModeA_NonTiered_ReturnsBreakdownFromAttendees()
    {
        var attendees = new[]
        {
            Attendee("Alice", AgeCategory.Adult, Gender.Female),
            Attendee("Bob",   AgeCategory.Adult, Gender.Male),
            Attendee("Cara",  AgeCategory.Child, Gender.Female),
        };
        var reg = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), attendees, Contact(), Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.Mode.Should().Be(RegistrationMode.DetailedAttendees);
        bd.IsTiered.Should().BeFalse();
        bd.TotalAttendees.Should().Be(3);
        bd.Rows.Should().ContainSingle();
        bd.Rows[0].Count.Should().Be(3);
        bd.Rows[0].Age.Captured.Should().BeTrue();
        bd.Rows[0].Age.Left.Should().Be(2);   // 2 Adults
        bd.Rows[0].Age.Right.Should().Be(1);  // 1 Child
        bd.Rows[0].Gender.Captured.Should().BeTrue();
        bd.Rows[0].Gender.Left.Should().Be(1);   // 1 Male
        bd.Rows[0].Gender.Right.Should().Be(2);  // 2 Females
    }

    [Fact]
    public void ModeA_Tiered_ReturnsOneRowPerTier()
    {
        var vipId = Guid.NewGuid();
        var generalId = Guid.NewGuid();
        var attendees = new[]
        {
            Attendee("Alice", AgeCategory.Adult, Gender.Female, vipId, "VIP"),
            Attendee("Bob",   AgeCategory.Adult, Gender.Male,   vipId, "VIP"),
            Attendee("Cara",  AgeCategory.Child, Gender.Female, generalId, "General"),
        };
        var reg = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), attendees, Contact(), Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.IsTiered.Should().BeTrue();
        bd.Rows.Should().HaveCount(2);
        bd.Rows.Select(r => r.TierName).Should().BeEquivalentTo(new[] { "VIP", "General" });
        bd.Rows.First(r => r.TierName == "VIP").Count.Should().Be(2);
        bd.Rows.First(r => r.TierName == "General").Count.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode B1 — HeadCountOnly (no demographic data)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ModeB1_HeadCountOnly_ReturnsBreakdownWithBothAxesNotCaptured()
    {
        var hc = HeadCountBreakdown.ForTotalOnly(5).Value;
        var reg = Registration.CreateWithHeadCount(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountOnly,
            "Lead Person",
            hc,
            Contact(),
            Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.Mode.Should().Be(RegistrationMode.HeadCountOnly);
        bd.TotalAttendees.Should().Be(5);
        bd.Rows.Should().ContainSingle();
        bd.Rows[0].Count.Should().Be(5);
        bd.Rows[0].Age.Captured.Should().BeFalse();
        bd.Rows[0].Gender.Captured.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode B2 — HeadCountByAge
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ModeB2_HeadCountByAge_AgeCaptured_GenderNotCaptured()
    {
        var hc = HeadCountBreakdown.ForByAge(adults: 3, children: 2).Value;
        var reg = Registration.CreateWithHeadCount(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByAge,
            "Lead Person",
            hc,
            Contact(),
            Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.Mode.Should().Be(RegistrationMode.HeadCountByAge);
        bd.TotalAttendees.Should().Be(5);
        bd.Rows[0].Age.Captured.Should().BeTrue();
        bd.Rows[0].Age.Left.Should().Be(3);
        bd.Rows[0].Age.Right.Should().Be(2);
        bd.Rows[0].Gender.Captured.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode B3 — HeadCountByGender
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ModeB3_HeadCountByGender_GenderCaptured_AgeNotCaptured()
    {
        var hc = HeadCountBreakdown.ForByGender(males: 4, females: 3).Value;
        var reg = Registration.CreateWithHeadCount(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByGender,
            "Lead Person",
            hc,
            Contact(),
            Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.Mode.Should().Be(RegistrationMode.HeadCountByGender);
        bd.TotalAttendees.Should().Be(7);
        bd.Rows[0].Age.Captured.Should().BeFalse();
        bd.Rows[0].Gender.Captured.Should().BeTrue();
        bd.Rows[0].Gender.Left.Should().Be(4);
        bd.Rows[0].Gender.Right.Should().Be(3);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode B4 — HeadCountByAgeAndGender
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ModeB4_HeadCountByAgeAndGender_BothAxesCaptured()
    {
        var hc = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 1, childMales: 0, childFemales: 1).Value;
        var reg = Registration.CreateWithHeadCount(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByAgeAndGender,
            "Lead Person",
            hc,
            Contact(),
            Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.Mode.Should().Be(RegistrationMode.HeadCountByAgeAndGender);
        bd.TotalAttendees.Should().Be(4);
        bd.Rows[0].Age.Captured.Should().BeTrue();
        bd.Rows[0].Age.Left.Should().Be(3);   // 2 AM + 1 AF = 3 adults
        bd.Rows[0].Age.Right.Should().Be(1);  // 0 CM + 1 CF = 1 child
        bd.Rows[0].Gender.Captured.Should().BeTrue();
        bd.Rows[0].Gender.Left.Should().Be(2);   // 2 AM + 0 CM = 2 males
        bd.Rows[0].Gender.Right.Should().Be(2);  // 1 AF + 1 CF = 2 females
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Tiered head-count
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ModeB2_Tiered_ReturnsOneRowPerTier()
    {
        var vipId = Guid.NewGuid();
        var generalId = Guid.NewGuid();
        var tierCounts = new List<TierCount>
        {
            TierCount.Create(vipId, "VIP", 2).Value,
            TierCount.Create(generalId, "General", 3).Value,
        };
        var hc = HeadCountBreakdown.ForByAge(adults: 4, children: 1, tierCounts: tierCounts).Value;
        var reg = Registration.CreateWithHeadCount(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByAge,
            "Lead Person",
            hc,
            Contact(),
            Zero()).Value;

        var bd = TicketPdfRegistrationBreakdownAssembler.Build(reg);

        bd.Should().NotBeNull();
        bd!.IsTiered.Should().BeTrue();
        bd.Rows.Should().HaveCount(2);
        bd.Rows.Select(r => r.TierName).Should().BeEquivalentTo(new[] { "VIP", "General" });
        bd.TotalAttendees.Should().Be(5);
    }
}
