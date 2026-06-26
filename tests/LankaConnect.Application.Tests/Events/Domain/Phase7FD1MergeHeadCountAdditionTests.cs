using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7F-D.1 — paid Mode-B add-attendees with delta payment (domain layer).
///
/// Architect-approved review iteration 1 (2026-04-30): targets the ≥24 case floor.
/// Tests below cover:
///   - <c>RegistrationAddition.CreateForHeadCountDelta</c> happy paths for B1/B2/B4
///   - Currency-match invariants
///   - Mode-A vs Mode-B discriminator (architect edit #1)
///   - <c>Registration.MergeHeadCountAddition(delta)</c> happy paths B1+B1, B2+B2, B4+B4
///   - Cross-mode rejected (B2 + B4 delta)
///   - Mode-A registration + Mode-B delta rejected
///   - TierCounts merge by TierId
///   - Lead-name preserved
///   - Status guard (not Confirmed → reject)
///   - Capacity guard (over-cap → reject)
///   - Null-policy guards
/// </summary>
public class Phase7FD1MergeHeadCountAdditionTests
{
    private static Money USD(decimal amount) => Money.Create(amount, Currency.USD).Value;

    private static RegistrationContact Contact(string email = "lead@example.com") =>
        RegistrationContact.Create(email, "555-0100", null).Value;

    private static (Event ev, Registration reg) CreatePaidB2RegistrationWith(
        int adults, int children, decimal totalPaid)
    {
        var ev = Event.Create(
            EventTitle.Create("7F-D test").Value,
            EventDescription.Create("paid B2").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
        ev.SetPricing(Money.Create(20m, Currency.USD).Value).IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        var head = HeadCountBreakdown.ForByAge(adults, children).Value;
        var rsvp = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact());
        rsvp.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", rsvp.Errors ?? Enumerable.Empty<string>())}");
        var reg = ev.Registrations.Single();
        // The B2 paid event needs to be confirmed for AddAttendees-style flow. Mark the
        // registration as confirmed via the lifecycle so the Confirmed+PaymentCompleted
        // guard in MergeHeadCountAddition is satisfied.
        reg.SetStripeCheckoutSession("cs_test_" + Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        reg.CompletePayment("pi_test_" + Guid.NewGuid()).IsSuccess.Should().BeTrue();
        return (ev, reg);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  RegistrationAddition.CreateForHeadCountDelta factory
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateForHeadCountDelta_B1_HappyPath()
    {
        var delta = HeadCountBreakdown.ForTotalOnly(2).Value;
        var result = RegistrationAddition.CreateForHeadCountDelta(
            registrationId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            mode: RegistrationMode.HeadCountOnly,
            headCountDelta: delta,
            previousTotal: USD(40),
            newTotal: USD(80),
            additionalAmount: USD(40));

        result.IsSuccess.Should().BeTrue($"errors: {result.Error}");
        result.Value.RegistrationMode.Should().Be(RegistrationMode.HeadCountOnly);
        result.Value.HeadCountDelta.Should().NotBeNull();
        result.Value.HeadCountDelta!.Total.Should().Be(2);
        result.Value.IsModeBAddition.Should().BeTrue();
        result.Value.IsModeAAddition.Should().BeFalse();
    }

    [Fact]
    public void CreateForHeadCountDelta_B2_WithDemographics()
    {
        var delta = HeadCountBreakdown.ForByAge(adults: 1, children: 1).Value;
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByAge,
            delta, USD(20), USD(50), USD(30));

        result.IsSuccess.Should().BeTrue();
        result.Value.HeadCountDelta!.Demographics!.Adults.Should().Be(1);
        result.Value.HeadCountDelta.Demographics.Children.Should().Be(1);
    }

    [Fact]
    public void CreateForHeadCountDelta_NullDelta_IsRejected()
    {
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountOnly,
            headCountDelta: null!, USD(10), USD(20), USD(10));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateForHeadCountDelta_ModeMustBeHeadCount_RejectsModeA()
    {
        var delta = HeadCountBreakdown.ForTotalOnly(1).Value;
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.DetailedAttendees, // wrong
            delta, USD(10), USD(20), USD(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("head-count mode");
    }

    [Fact]
    public void CreateForHeadCountDelta_NoRegistrationMode_IsRejected()
    {
        var delta = HeadCountBreakdown.ForTotalOnly(1).Value;
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.NoRegistration, delta, USD(10), USD(20), USD(10));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateForHeadCountDelta_FreeMode_AdditionalAmountZero_Allowed()
    {
        var delta = HeadCountBreakdown.ForTotalOnly(1).Value;
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountOnly,
            delta, USD(0), USD(0), USD(0));

        result.IsSuccess.Should().BeTrue("free Mode-B addition uses the same code path with zero amount");
    }

    [Fact]
    public void CreateForHeadCountDelta_CurrencyMismatch_IsRejected()
    {
        var delta = HeadCountBreakdown.ForTotalOnly(1).Value;
        var lkr = Money.Create(50m, Currency.LKR).Value;
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountOnly, delta,
            previousTotal: USD(10), newTotal: lkr, additionalAmount: USD(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("currency");
    }

    [Fact]
    public void CreateForHeadCountDelta_AdditionalAmountMismatch_IsRejected()
    {
        var delta = HeadCountBreakdown.ForTotalOnly(1).Value;
        // Previous=10, New=30; expected diff=20, but supplied=15
        var result = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountOnly, delta,
            previousTotal: USD(10), newTotal: USD(30), additionalAmount: USD(15));

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Registration.MergeHeadCountAddition — happy paths
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeHeadCountAddition_B2PlusB2_AccumulatesTotalAndDemographics()
    {
        var (_, reg) = CreatePaidB2RegistrationWith(adults: 2, children: 1, totalPaid: 60m);
        var delta = HeadCountBreakdown.ForByAge(adults: 1, children: 0).Value;

        var result = reg.MergeHeadCountAddition(
            additionMode: RegistrationMode.HeadCountByAge, delta,
            newTotalPrice: USD(80),
            maxAttendeesPerRegistration: 10);

        result.IsSuccess.Should().BeTrue($"errors: {result.Error}");
        reg.HeadCount!.Total.Should().Be(4, "3 + 1 = 4");
        reg.HeadCount.Demographics!.Adults.Should().Be(3, "2 + 1 = 3");
        reg.HeadCount.Demographics.Children.Should().Be(1);
    }

    [Fact]
    public void MergeHeadCountAddition_B1PlusB1_AccumulatesTotal()
    {
        var ev = Event.Create(
            EventTitle.Create("B1 test").Value,
            EventDescription.Create("desc").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(), capacity: 100).Value;
        ev.SetPricing(USD(20)).IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForTotalOnly(2).Value, Contact()).IsSuccess.Should().BeTrue();
        var reg = ev.Registrations.Single();
        reg.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddHours(1));
        reg.CompletePayment("pi_test").IsSuccess.Should().BeTrue();

        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountOnly,
            HeadCountBreakdown.ForTotalOnly(3).Value,
            newTotalPrice: USD(100),
            maxAttendeesPerRegistration: 10);

        result.IsSuccess.Should().BeTrue();
        reg.HeadCount!.Total.Should().Be(5);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode-match invariant (architect-required)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeHeadCountAddition_CrossMode_B2PlusB4_IsRejected()
    {
        var (_, reg) = CreatePaidB2RegistrationWith(adults: 1, children: 0, totalPaid: 20m);
        var b4Delta = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 1, adultFemales: 0, childMales: 0, childFemales: 0).Value;

        var result = reg.MergeHeadCountAddition(
            additionMode: RegistrationMode.HeadCountByAgeAndGender,
            b4Delta, newTotalPrice: USD(40), maxAttendeesPerRegistration: 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("mode", because: "addition mode must match parent registration mode");
    }

    [Fact]
    public void MergeHeadCountAddition_ModeARegistration_RejectsHeadCountDelta()
    {
        // Mode A registration
        var ev = Event.Create(
            EventTitle.Create("A").Value, EventDescription.Create("d").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(), capacity: 50).Value;
        ev.SetPricing(USD(20)).IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.RegisterWithAttendees(Guid.NewGuid(),
            new[] { AttendeeDetails.Create("A1", AgeCategory.Adult).Value },
            Contact()).IsSuccess.Should().BeTrue();
        var reg = ev.Registrations.Single();
        reg.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddHours(1));
        reg.CompletePayment("pi_test").IsSuccess.Should().BeTrue();

        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountByAge,
            HeadCountBreakdown.ForByAge(1, 0).Value,
            USD(40), 10);

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Capacity + max-attendees guards
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeHeadCountAddition_OverMaxAttendees_IsRejected()
    {
        var (_, reg) = CreatePaidB2RegistrationWith(adults: 8, children: 0, totalPaid: 160m);
        var delta = HeadCountBreakdown.ForByAge(adults: 5, children: 0).Value;

        // max=10, current=8, delta=5 → 13 > 10
        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountByAge, delta,
            newTotalPrice: USD(260), maxAttendeesPerRegistration: 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Maximum");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Status guards
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeHeadCountAddition_StatusNotConfirmed_IsRejected()
    {
        var ev = Event.Create(
            EventTitle.Create("test").Value, EventDescription.Create("d").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(), capacity: 50).Value;
        ev.SetPricing(USD(20)).IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForByAge(2, 0).Value, Contact()).IsSuccess.Should().BeTrue();
        var reg = ev.Registrations.Single();
        // Registration is Preliminary (paid, awaiting payment) — NOT Confirmed
        reg.Status.Should().Be(RegistrationStatus.Preliminary);

        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountByAge,
            HeadCountBreakdown.ForByAge(1, 0).Value,
            USD(60), 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Confirmed", because: "merge requires Confirmed status");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Lead-name preservation
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeHeadCountAddition_PreservesLeadAttendeeName()
    {
        var (_, reg) = CreatePaidB2RegistrationWith(adults: 2, children: 0, totalPaid: 40m);
        var leadBefore = reg.LeadAttendeeName;

        reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountByAge,
            HeadCountBreakdown.ForByAge(1, 0).Value,
            USD(60), 10).IsSuccess.Should().BeTrue();

        reg.LeadAttendeeName.Should().Be(leadBefore, "lead name is not changed by an addition");
        reg.LeadAttendeeName.Should().Be("Lead");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  TierCounts merge by TierId
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeHeadCountAddition_TieredEvent_MergesTierCountsByTierId()
    {
        // Build a paid B1 + tiered registration with VIP×2 + General×1
        var ev = Event.Create(
            EventTitle.Create("tier").Value, EventDescription.Create("d").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(), capacity: 100).Value;
        ev.SetPricing(USD(50)).IsSuccess.Should().BeTrue();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();
        var vipResult = ev.AddTicketTier("VIP", "VIP", USD(50), null, null,
            capacity: 10, maxPerUser: 10, sortOrder: 1);
        var genResult = ev.AddTicketTier("General", "General", USD(30), null, null,
            capacity: 50, maxPerUser: 50, sortOrder: 2);
        vipResult.IsSuccess.Should().BeTrue();
        genResult.IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();

        var initialTiers = new[]
        {
            TierCount.Create(vipResult.Value.Id, "VIP", 2).Value,
            TierCount.Create(genResult.Value.Id, "General", 1).Value,
        };
        ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead",
            HeadCountBreakdown.ForTotalOnly(3, initialTiers).Value, Contact()).IsSuccess.Should().BeTrue();
        var reg = ev.Registrations.Single();
        reg.SetStripeCheckoutSession("cs_test", DateTime.UtcNow.AddHours(1));
        reg.CompletePayment("pi_test").IsSuccess.Should().BeTrue();

        // Delta: +1 VIP
        var deltaTiers = new[]
        {
            TierCount.Create(vipResult.Value.Id, "VIP", 1).Value,
        };
        var delta = HeadCountBreakdown.ForTotalOnly(1, deltaTiers).Value;

        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountOnly, delta,
            newTotalPrice: USD(180), maxAttendeesPerRegistration: 10);

        result.IsSuccess.Should().BeTrue($"errors: {result.Error}");
        reg.HeadCount!.Total.Should().Be(4);
        reg.HeadCount.TierCounts.Should().HaveCount(2);
        var vipTc = reg.HeadCount.TierCounts!.Single(t => t.TierId == vipResult.Value.Id);
        vipTc.Count.Should().Be(3, "2 + 1 = 3");
        var genTc = reg.HeadCount.TierCounts!.Single(t => t.TierId == genResult.Value.Id);
        genTc.Count.Should().Be(1, "untouched");
    }

    [Fact]
    public void MergeHeadCountAddition_NullDelta_IsRejected()
    {
        var (_, reg) = CreatePaidB2RegistrationWith(adults: 1, children: 0, totalPaid: 20m);
        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountByAge,
            headCountDelta: null!,
            USD(40), 10);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MergeHeadCountAddition_NullTotalPrice_IsRejected()
    {
        var (_, reg) = CreatePaidB2RegistrationWith(adults: 1, children: 0, totalPaid: 20m);
        var result = reg.MergeHeadCountAddition(
            RegistrationMode.HeadCountByAge,
            HeadCountBreakdown.ForByAge(1, 0).Value,
            newTotalPrice: null!, 10);

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode-A discriminator stays correct after a Mode-B addition row exists
    //  (architect edit #1 — IsModeBAddition based on RegistrationMode, not _newAttendees.Count)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void RegistrationAddition_IsModeBAddition_BasedOnRegistrationModeNotAttendeeListCount()
    {
        var delta = HeadCountBreakdown.ForByAge(1, 0).Value;
        var addition = RegistrationAddition.CreateForHeadCountDelta(
            Guid.NewGuid(), Guid.NewGuid(),
            RegistrationMode.HeadCountByAge, delta,
            USD(20), USD(40), USD(20)).Value;

        addition.IsModeBAddition.Should().BeTrue();
        addition.IsModeAAddition.Should().BeFalse();
        addition.NewAttendees.Should().BeEmpty();
        // Even though _newAttendees is empty, IsModeAAddition must return false based on
        // RegistrationMode.IsHeadCountMode() — the architect-flagged "false-positive after merge" trap.
    }
}
