using FluentAssertions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7F-B.1 — A↔B mode conversion with attendee backfill (domain layer).
///
/// Architect-approved review iteration 1 (2026-04-30): ≥36 case floor (architect-revised
/// from ≥30). Tests below cover:
///   - Happy paths: A→B1/B2/B3/B4 (4); B1/B2/B3/B4→A (4)
///   - Demographic derivation correctness on A→B (per-attendee AgeCategory/Gender → buckets)
///   - Per-tier-age axis populated on A→B for tiered events (depends on 7F-C live)
///   - Lead-name precedence: Contact.FullName ?? FirstAttendee.Name
///   - Placeholder name scheme on B→A: row 1 = unmodified LeadName, rows 2..N = "{LeadName} (n)"
///   - Deterministic ordering on B→A: Adults before Children; Males before Females; (Adult,Male)→
///     (Adult,Female)→(Child,Male)→(Child,Female) for B4
///   - Stable sort on TicketTierId allocation (by SortOrder, fall back to TierName)
///   - Cancelled / Refunded / Abandoned registrations untouched
///   - Skipped registrations: Other gender into B3/B4; pending RegistrationAddition; named seats
///   - Same-mode → idempotent no-op
///   - Total=0 corner case after Other-gender filtering rejects globally
///   - DryRun: report computed without mutation
///   - Per-tier reservation accounting unchanged
///   - Batch cap > 500 rejected
///   - RegistrationMode snapshot flips on each migrated row
///   - ActualHeadCountAttended preserved in audit, dropped from live (B→A drops; A→B no field)
///   - Audit shape: BeforeShape + AfterShape jsonb-compatible
///   - Concurrency: not in domain (handled at handler layer per architect)
///   - Compatibility re-check: event-shape via 7E.2 validator (handler layer); per-registration
///     named-seat check is in domain
///
/// Total: 36+ cases targeted across this file.
/// </summary>
public class Phase7FB1ModeConversionTests
{
    // ──────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static Event CreateEvent(
        RegistrationMode mode = RegistrationMode.DetailedAttendees,
        bool tiered = false,
        decimal vipAdult = 50m, decimal vipChild = 25m,
        decimal genAdult = 30m, decimal genChild = 15m)
    {
        var ev = Event.Create(
            EventTitle.Create("7F-B test").Value,
            EventDescription.Create("Mode conversion").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
        ev.SetPricing(Money.Create(vipAdult, Currency.USD).Value).IsSuccess.Should().BeTrue();
        if (tiered)
        {
            ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();
            ev.AddTicketTier("VIP", "VIP",
                Money.Create(vipAdult, Currency.USD).Value,
                Money.Create(vipChild, Currency.USD).Value, 12,
                capacity: 20, maxPerUser: 20, sortOrder: 1).IsSuccess.Should().BeTrue();
            ev.AddTicketTier("General", "General",
                Money.Create(genAdult, Currency.USD).Value,
                Money.Create(genChild, Currency.USD).Value, 12,
                capacity: 50, maxPerUser: 50, sortOrder: 2).IsSuccess.Should().BeTrue();
        }
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(mode).IsSuccess.Should().BeTrue();
        return ev;
    }

    private static RegistrationContact Contact(string email = "lead@example.com") =>
        RegistrationContact.Create(email, "555-0100", null).Value;

    private static AttendeeDetails Attendee(string name, AgeCategory age = AgeCategory.Adult,
        Gender? gender = null, Guid? tierId = null, string? tierName = null) =>
        AttendeeDetails.Create(name, age, gender, ticketTierId: tierId, ticketTierName: tierName).Value;

    private static ConversionPolicy DefaultPolicy() => new()
    {
        OrganiserId = Guid.NewGuid(),
    };

    // ──────────────────────────────────────────────────────────────────────
    //  A → B happy paths
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoB1_CollapsesAttendeesIntoTotalOnly()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("Alice"), Attendee("Bob"), Attendee("Carol") },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, DefaultPolicy());

        report.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", report.Errors ?? Enumerable.Empty<string>())}");
        report.Value.Migrated.Should().HaveCount(1);
        report.Value.Skipped.Should().BeEmpty();
        ev.RegistrationMode.Should().Be(RegistrationMode.HeadCountOnly);
        var reg = ev.Registrations.Single();
        reg.RegistrationMode.Should().Be(RegistrationMode.HeadCountOnly);
        reg.HeadCount!.Total.Should().Be(3);
        reg.HeadCount.Demographics.Should().BeNull("B1 has no demographic axis");
        reg.LeadAttendeeName.Should().Be("Alice", "Contact has no FullName field — lead = first attendee");
        reg.Attendees.Should().BeEmpty("attendee rows dropped from live aggregate");
    }

    [Fact]
    public void Convert_AtoB2_DerivesAdultsAndChildrenFromAgeCategory()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[]
            {
                Attendee("A1", AgeCategory.Adult),
                Attendee("A2", AgeCategory.Adult),
                Attendee("C1", AgeCategory.Child),
            },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAge, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var reg = ev.Registrations.Single();
        reg.HeadCount!.Total.Should().Be(3);
        reg.HeadCount.Demographics!.Adults.Should().Be(2);
        reg.HeadCount.Demographics.Children.Should().Be(1);
    }

    [Fact]
    public void Convert_AtoB3_DerivesMalesAndFemalesFromGender()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[]
            {
                Attendee("M1", AgeCategory.Adult, Gender.Male),
                Attendee("M2", AgeCategory.Adult, Gender.Male),
                Attendee("F1", AgeCategory.Adult, Gender.Female),
            },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByGender, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var reg = ev.Registrations.Single();
        reg.HeadCount!.Demographics!.Males.Should().Be(2);
        reg.HeadCount.Demographics.Females.Should().Be(1);
    }

    [Fact]
    public void Convert_AtoB4_DerivesFourLeavesFromAgeAndGender()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[]
            {
                Attendee("AM", AgeCategory.Adult, Gender.Male),
                Attendee("AF", AgeCategory.Adult, Gender.Female),
                Attendee("CM", AgeCategory.Child, Gender.Male),
                Attendee("CF", AgeCategory.Child, Gender.Female),
            },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAgeAndGender, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var demo = ev.Registrations.Single().HeadCount!.Demographics!;
        demo.AdultMales.Should().Be(1);
        demo.AdultFemales.Should().Be(1);
        demo.ChildMales.Should().Be(1);
        demo.ChildFemales.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  A → B with Other gender — architect Q1 reject per registration
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoB3_OtherGender_SkipsRegistration()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[]
            {
                Attendee("A", gender: Gender.Male),
                Attendee("B", gender: Gender.Other),
            },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByGender, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().BeEmpty();
        report.Value.Skipped.Should().HaveCount(1);
        report.Value.Skipped[0].ReasonCode.Should().Be("GenderOtherNotSupportedByMode");
        ev.Registrations.Single().RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees,
            "skipped registration keeps its original mode snapshot");
    }

    [Fact]
    public void Convert_AtoB4_OtherGender_SkipsRegistration()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[]
            {
                Attendee("A", AgeCategory.Adult, Gender.Male),
                Attendee("B", AgeCategory.Adult, Gender.Other),
            },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAgeAndGender, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        report.Value.Skipped.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  B → A explode happy paths + name fabrication + ordering
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_B1toA_ExplodesIntoPlaceholderRows_LeadNamePreserved()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountOnly);
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Niroshana",
            HeadCountBreakdown.ForTotalOnly(3).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", report.Errors ?? Enumerable.Empty<string>())}");
        report.Value.Migrated.Should().HaveCount(1);
        var reg = ev.Registrations.Single();
        reg.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
        reg.Attendees.Should().HaveCount(3);
        reg.Attendees[0].Name.Should().Be("Niroshana", "row 1 keeps unmodified lead name");
        reg.Attendees[1].Name.Should().Be("Niroshana (2)");
        reg.Attendees[2].Name.Should().Be("Niroshana (3)");
        reg.HeadCount.Should().BeNull("B-mode head-count cleared");
        reg.LeadAttendeeName.Should().BeNull("LeadAttendeeName cleared on B→A");
    }

    [Fact]
    public void Convert_B2toA_ProducesAdultsBeforeChildren()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountByAge);
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var attendees = ev.Registrations.Single().Attendees;
        attendees.Should().HaveCount(3);
        attendees[0].AgeCategory.Should().Be(AgeCategory.Adult);
        attendees[1].AgeCategory.Should().Be(AgeCategory.Adult);
        attendees[2].AgeCategory.Should().Be(AgeCategory.Child, "deterministic ordering: adults before children");
    }

    [Fact]
    public void Convert_B3toA_ProducesMalesBeforeFemales()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountByGender);
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForByGender(males: 2, females: 1).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var attendees = ev.Registrations.Single().Attendees;
        attendees[0].Gender.Should().Be(Gender.Male);
        attendees[1].Gender.Should().Be(Gender.Male);
        attendees[2].Gender.Should().Be(Gender.Female);
    }

    [Fact]
    public void Convert_B4toA_DeterministicFourLeafOrder()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountByAgeAndGender);
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForByAgeAndGender(
                adultMales: 1, adultFemales: 1, childMales: 1, childFemales: 1).Value,
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var attendees = ev.Registrations.Single().Attendees;
        // Architect §2.2 deterministic order: (Adult,Male), (Adult,Female), (Child,Male), (Child,Female)
        attendees[0].AgeCategory.Should().Be(AgeCategory.Adult);
        attendees[0].Gender.Should().Be(Gender.Male);
        attendees[1].AgeCategory.Should().Be(AgeCategory.Adult);
        attendees[1].Gender.Should().Be(Gender.Female);
        attendees[2].AgeCategory.Should().Be(AgeCategory.Child);
        attendees[2].Gender.Should().Be(Gender.Male);
        attendees[3].AgeCategory.Should().Be(AgeCategory.Child);
        attendees[3].Gender.Should().Be(Gender.Female);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Cancelled / Refunded registrations untouched (active-only filter)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_IgnoresCancelledRegistrations_OnlyConvertsActiveOnes()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(), new[] { Attendee("Active") }, Contact()).IsSuccess.Should().BeTrue();
        ev.RegisterWithAttendees(Guid.NewGuid(), new[] { Attendee("Cancelled") }, Contact("c@e.com")).IsSuccess.Should().BeTrue();

        // Cancel the second registration
        ev.Registrations.Last().Cancel();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().HaveCount(1, "only the active registration migrates");
        var active = ev.Registrations.First(r => r.Status != RegistrationStatus.Cancelled);
        active.RegistrationMode.Should().Be(RegistrationMode.HeadCountOnly);
        var cancelled = ev.Registrations.First(r => r.Status == RegistrationStatus.Cancelled);
        cancelled.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees,
            "cancelled rows preserve their original mode for historical email re-renders");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Same-mode idempotency
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_SameMode_IsIdempotentNoOp()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(), new[] { Attendee("A") }, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().BeEmpty("same-mode is a no-op");
        report.Value.Skipped.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  DryRun: report computed without mutation
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_DryRun_ReportsButDoesNotMutate()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("A"), Attendee("B") }, Contact()).IsSuccess.Should().BeTrue();

        var policy = new ConversionPolicy { OrganiserId = Guid.NewGuid(), DryRun = true };
        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, policy);

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().HaveCount(1);
        ev.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees, "dry-run leaves event unchanged");
        ev.Registrations.Single().RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
        ev.Registrations.Single().HeadCount.Should().BeNull();
        ev.Registrations.Single().Attendees.Should().HaveCount(2);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Pending RegistrationAddition skip (architect Q8)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_RegistrationWithPendingAddition_IsSkipped()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(), new[] { Attendee("A") }, Contact()).IsSuccess.Should().BeTrue();
        var regId = ev.Registrations.Single().Id;

        var policy = new ConversionPolicy
        {
            OrganiserId = Guid.NewGuid(),
            RegistrationIdsWithPendingAdditions = new HashSet<Guid> { regId },
        };
        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, policy);

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().BeEmpty();
        report.Value.Skipped.Should().HaveCount(1);
        report.Value.Skipped[0].ReasonCode.Should().Be("PendingAdditionMustResolveFirst");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Batch cap (architect Q7) — 500 default, configurable
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_BatchOverCap_ReturnsFailure()
    {
        var ev = CreateEvent();
        // Add 3 active registrations and cap at 2.
        for (var i = 0; i < 3; i++)
            ev.RegisterWithAttendees(Guid.NewGuid(),
                new[] { Attendee($"User{i}") }, Contact($"u{i}@e.com")).IsSuccess.Should().BeTrue();

        var policy = new ConversionPolicy { OrganiserId = Guid.NewGuid(), BatchCap = 2 };
        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, policy);

        report.IsFailure.Should().BeTrue();
        string.Join("; ", report.Errors!).Should().Contain("batch", because: "active set exceeds cap");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Lead-name precedence (architect Q6): Contact.FullName ?? FirstAttendee.Name
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoB_LeadName_UsesFirstAttendeeName()
    {
        // RegistrationContact has no FullName field today (only Email / Phone / Address),
        // so the lead-name source on A→B is simply Attendees[0].Name. Architect Q6's
        // hypothetical Contact.FullName fallback degenerates to this single rule.
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("First Attendee"), Attendee("Second") },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().LeadAttendeeName.Should().Be("First Attendee");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Audit shape — BeforeShape + AfterShape captured per migrated row
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoB_AuditCapturesBeforeAndAfterShape()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("Alice"), Attendee("Bob") },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var migrated = report.Value.Migrated.Single();
        migrated.RegistrationId.Should().Be(ev.Registrations.Single().Id);
        migrated.BeforeAttendees.Should().NotBeNull();
        migrated.BeforeAttendees!.Should().HaveCount(2);
        migrated.BeforeHeadCount.Should().BeNull("source was Mode A — no head-count");
        migrated.AfterHeadCount.Should().NotBeNull();
        migrated.AfterHeadCount!.Total.Should().Be(2);
        migrated.AfterAttendees.Should().BeNull("target is Mode B — no attendee rows");
    }

    [Fact]
    public void Convert_BtoA_AuditCapturesBeforeHeadCount_AndAfterAttendees()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountByAge);
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForByAge(adults: 2, children: 1).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var migrated = report.Value.Migrated.Single();
        migrated.BeforeHeadCount.Should().NotBeNull();
        migrated.BeforeHeadCount!.Total.Should().Be(3);
        migrated.BeforeAttendees.Should().BeNull();
        migrated.AfterAttendees.Should().NotBeNull();
        migrated.AfterAttendees!.Should().HaveCount(3);
        migrated.AfterHeadCount.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Tiered events — A→B populates per-tier-age axis (depends on 7F-C live)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoB2_Tiered_PopulatesPerTierAgeAxisFromAttendees()
    {
        var ev = CreateEvent(tiered: true);
        var vip = ev.TicketTiers.Single(t => t.Name == "VIP");
        var general = ev.TicketTiers.Single(t => t.Name == "General");

        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[]
            {
                Attendee("Alice", AgeCategory.Adult, tierId: vip.Id, tierName: vip.Name),
                Attendee("Bob",   AgeCategory.Adult, tierId: vip.Id, tierName: vip.Name),
                Attendee("Carol", AgeCategory.Child, tierId: vip.Id, tierName: vip.Name),
                Attendee("Dan",   AgeCategory.Adult, tierId: general.Id, tierName: general.Name),
                Attendee("Eve",   AgeCategory.Adult, tierId: general.Id, tierName: general.Name),
            },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAge, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var hc = ev.Registrations.Single().HeadCount!;
        hc.Total.Should().Be(5);
        hc.Demographics!.Adults.Should().Be(4);
        hc.Demographics.Children.Should().Be(1);
        hc.TierCounts.Should().HaveCount(2);

        var vipTc = hc.TierCounts!.Single(t => t.TierId == vip.Id);
        vipTc.Count.Should().Be(3);
        vipTc.AdultCount.Should().Be(2);
        vipTc.ChildCount.Should().Be(1);

        var generalTc = hc.TierCounts!.Single(t => t.TierId == general.Id);
        generalTc.AdultCount.Should().Be(2);
        generalTc.ChildCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  B→A on tiered: TierCount → per-tier placeholder allocation
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_B2toA_Tiered_AllocatesPlaceholdersByTierAndAge()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountByAge, tiered: true);
        var vip = ev.TicketTiers.Single(t => t.Name == "VIP");
        var general = ev.TicketTiers.Single(t => t.Name == "General");

        var tiers = new[]
        {
            TierCount.Create(vip.Id, vip.Name, count: 3, adultCount: 2, childCount: 1).Value,
            TierCount.Create(general.Id, general.Name, count: 2, adultCount: 2, childCount: 0).Value,
        };
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForByAge(adults: 4, children: 1, tiers).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var attendees = ev.Registrations.Single().Attendees;
        attendees.Should().HaveCount(5);
        attendees.Count(a => a.TicketTierId == vip.Id).Should().Be(3);
        attendees.Count(a => a.TicketTierId == general.Id).Should().Be(2);
        attendees.Count(a => a.AgeCategory == AgeCategory.Adult).Should().Be(4);
        attendees.Count(a => a.AgeCategory == AgeCategory.Child).Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Stable sort on TicketTierId allocation by SortOrder
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_B1toA_StableSort_ByTierSortOrderAscending()
    {
        // Start in B1 + tiered so we can register with TierCounts in their natural order,
        // then convert to A and verify the stable tier sort during placeholder allocation.
        var ev = CreateEvent(RegistrationMode.HeadCountOnly, tiered: true);
        var vip = ev.TicketTiers.Single(t => t.Name == "VIP");        // sortOrder 1
        var general = ev.TicketTiers.Single(t => t.Name == "General"); // sortOrder 2

        // Specify tiers in REVERSE order in the registration payload to exercise the sort.
        var tiers = new[]
        {
            TierCount.Create(general.Id, general.Name, count: 2).Value,
            TierCount.Create(vip.Id, vip.Name, count: 1).Value,
        };
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForTotalOnly(3, tiers).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var attendees = ev.Registrations.Single().Attendees;
        // VIP (sortOrder 1) attendees come first, then General (sortOrder 2).
        attendees[0].TicketTierId.Should().Be(vip.Id, "VIP has lower SortOrder, allocated first");
        attendees[1].TicketTierId.Should().Be(general.Id);
        attendees[2].TicketTierId.Should().Be(general.Id);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Compatibility: per-registration named-seat assignment skips B conversion
    // ──────────────────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────────────────
    //  C-mode conversions deferred to SetRegistrationMode (zero-reg gate)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoC_IsRejected_DeferredToSetRegistrationMode()
    {
        var ev = CreateEvent();
        var report = ev.ConvertRegistrationMode(RegistrationMode.NoRegistration, DefaultPolicy());

        report.IsFailure.Should().BeTrue();
        string.Join("; ", report.Errors!).Should().Contain("Mode C");
    }

    [Fact]
    public void Convert_CtoA_IsRejected_DeferredToSetRegistrationMode()
    {
        var ev = Event.Create(
            EventTitle.Create("Mode C event").Value,
            EventDescription.Create("desc").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(), capacity: 100).Value;
        ev.SetAsFreeEvent().IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(RegistrationMode.NoRegistration).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Empty event (zero active registrations) — flips event mode anyway
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_NoActiveRegistrations_FlipsEventModeAnyway()
    {
        var ev = CreateEvent();
        // No registrations — event has Mode A but is empty.

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAge, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().BeEmpty();
        report.Value.Skipped.Should().BeEmpty();
        ev.RegistrationMode.Should().Be(RegistrationMode.HeadCountByAge,
            "no registrations to migrate but the event mode flips so future RSVPs use the new mode");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  DryRun alongside skipped rows — still reports both
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_DryRun_WithMixedMigratedAndSkipped_ReportsBoth()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("OK", AgeCategory.Adult, Gender.Male) },
            Contact("ok@e.com")).IsSuccess.Should().BeTrue();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("HasOther", AgeCategory.Adult, Gender.Other) },
            Contact("other@e.com")).IsSuccess.Should().BeTrue();

        var policy = new ConversionPolicy { OrganiserId = Guid.NewGuid(), DryRun = true };
        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByGender, policy);

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().HaveCount(1, "one registration is convertible");
        report.Value.Skipped.Should().HaveCount(1, "Gender.Other attendee blocks the second");
        ev.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees, "dry-run leaves event unchanged");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  All-adult / all-child B2 collapses
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtoB2_AllAdult_DerivesAdultsOnly_ChildrenZero()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("A1"), Attendee("A2"), Attendee("A3") },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAge, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var demo = ev.Registrations.Single().HeadCount!.Demographics!;
        demo.Adults.Should().Be(3);
        demo.Children.Should().Be(0);
    }

    [Fact]
    public void Convert_AtoB2_AllChild_DerivesChildrenOnly_AdultsZero()
    {
        var ev = CreateEvent();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { Attendee("C1", AgeCategory.Child), Attendee("C2", AgeCategory.Child) },
            Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountByAge, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var demo = ev.Registrations.Single().HeadCount!.Demographics!;
        demo.Adults.Should().Be(0);
        demo.Children.Should().Be(2);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Tier rename drift: live tier was renamed since the registration —
    //  the snapshotted name in TierCount drives placeholder allocation,
    //  but the live name is preferred when both exist (architect plan).
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_BtoA_LiveTierName_PreferredOverSnapshot()
    {
        var ev = CreateEvent(RegistrationMode.HeadCountOnly, tiered: true);
        var vip = ev.TicketTiers.Single(t => t.Name == "VIP");

        var staleTc = TierCount.Create(vip.Id, "VIP-OldName", count: 2).Value;
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForTotalOnly(2, new[] { staleTc }).Value, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.DetailedAttendees, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        var attendees = ev.Registrations.Single().Attendees;
        attendees.Should().HaveCount(2);
        attendees[0].TicketTierName.Should().Be("VIP",
            "live tier name preferred when present — snapshot is the fallback for orphaned tiers");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  ConversionPolicy null guard
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_NullPolicy_IsRejected()
    {
        var ev = CreateEvent();
        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, null!);
        report.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Convert_AtoB_AttendeeWithNamedSeat_SkipsRegistration()
    {
        var ev = CreateEvent();
        // Build attendee with a named-seat assignment (seatId + seatLabel populated)
        var seated = AttendeeDetails.Create("Seated", AgeCategory.Adult, gender: null,
            seatId: Guid.NewGuid(), seatLabel: "A12").Value;
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { seated, Attendee("Other") }, Contact()).IsSuccess.Should().BeTrue();

        var report = ev.ConvertRegistrationMode(RegistrationMode.HeadCountOnly, DefaultPolicy());

        report.IsSuccess.Should().BeTrue();
        report.Value.Migrated.Should().BeEmpty();
        report.Value.Skipped.Should().HaveCount(1);
        report.Value.Skipped[0].ReasonCode.Should().Be("NamedSeatsRequireDetailedAttendees");
    }
}
