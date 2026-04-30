using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7F-C — Tier × age matrix pricing on Mode B.
///
/// Lifts the deliberate <c>AdultPrice-only</c> collapse shipped in 7E.3c
/// (see breadcrumb at <c>Event.RegistrationMode.cs:436-440</c>) so a
/// <see cref="RegistrationMode.HeadCountByAge"/> or
/// <see cref="RegistrationMode.HeadCountByAgeAndGender"/> registration can express
/// per-tier-by-age counts and pay <c>tier.AdultPrice</c> for adults +
/// <c>tier.ChildPrice</c> for children — same routing Mode A uses today via
/// <see cref="TicketTier.CalculatePriceForAttendee"/>.
///
/// Architect review iteration 1 (2026-04-30): ≥18 case floor; tests below
/// cover TierCount factory invariants (8), HeadCountBreakdown cross-axis
/// invariants (5), pricing including legacy null-axis (5), Mode A parity (1),
/// child-price tier guard (3), and JSON round-trip (1) = 23 cases.
/// </summary>
public class Phase7FCTierAgeMatrixPricingTests
{
    // ──────────────────────────────────────────────────────────────────────
    //  Helpers — mirror Phase7E3cTierCountsPricingTests' shape so the parity
    //  test can compare bills directly.
    // ──────────────────────────────────────────────────────────────────────

    private static (Event @event, TicketTier vip, TicketTier general) CreateTieredEventWithChildPricing(
        decimal vipAdult = 50m, decimal vipChild = 25m, int vipCapacity = 10,
        decimal generalAdult = 30m, decimal generalChild = 15m, int generalCapacity = 40)
    {
        var title = EventTitle.Create("7F-C tier-age test").Value;
        var description = EventDescription.Create("Tier × age matrix pricing").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: vipCapacity + generalCapacity + 50).Value;
        ev.SetPricing(Money.Create(vipAdult, Currency.USD).Value).IsSuccess.Should().BeTrue();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();

        // ChildAgeLimit triggers HasChildPricing=true on the tier when ChildPrice is also non-null.
        var vipResult = ev.AddTicketTier(
            "VIP", "VIP tier",
            Money.Create(vipAdult, Currency.USD).Value,
            Money.Create(vipChild, Currency.USD).Value,
            childAgeLimit: 12,
            capacity: vipCapacity, maxPerUser: vipCapacity, sortOrder: 1);
        vipResult.IsSuccess.Should().BeTrue($"AddTicketTier VIP failed: {vipResult.Error}");

        var generalResult = ev.AddTicketTier(
            "General", "General tier",
            Money.Create(generalAdult, Currency.USD).Value,
            Money.Create(generalChild, Currency.USD).Value,
            childAgeLimit: 12,
            capacity: generalCapacity, maxPerUser: generalCapacity, sortOrder: 2);
        generalResult.IsSuccess.Should().BeTrue();

        ev.Publish().IsSuccess.Should().BeTrue();
        return (ev, vipResult.Value, generalResult.Value);
    }

    private static (Event @event, TicketTier adultOnly) CreateTieredEventNoChildPricing(
        decimal adultPrice = 40m, int capacity = 20)
    {
        var title = EventTitle.Create("Adult-only-tier event").Value;
        var description = EventDescription.Create("Tier without ChildPrice").Value;
        var ev = Event.Create(
            title, description,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(), capacity: capacity + 50).Value;
        ev.SetPricing(Money.Create(adultPrice, Currency.USD).Value).IsSuccess.Should().BeTrue();
        ev.SetTicketingMode(TicketingMode.Tiered).IsSuccess.Should().BeTrue();

        var tierResult = ev.AddTicketTier(
            "Standard", "Standard tier (adult-price only)",
            Money.Create(adultPrice, Currency.USD).Value,
            childPrice: null, childAgeLimit: null,
            capacity: capacity, maxPerUser: capacity, sortOrder: 1);
        tierResult.IsSuccess.Should().BeTrue();
        ev.Publish().IsSuccess.Should().BeTrue();
        return (ev, tierResult.Value);
    }

    private static RegistrationContact Contact(string email = "tac@example.com") =>
        RegistrationContact.Create(email, "555-0100", null).Value;

    // ──────────────────────────────────────────────────────────────────────
    //  TierCount factory invariants — architect edits #1, #2
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TierCount_Create_BothAgeCountsSet_Succeeds_AndExposesHasAgeSplit()
    {
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdultCount.Should().Be(2);
        result.Value.ChildCount.Should().Be(1);
        result.Value.HasAgeSplit.Should().BeTrue();
    }

    [Fact]
    public void TierCount_Create_BothAgeCountsNull_LegacyPath_Succeeds()
    {
        var result = TierCount.Create(Guid.NewGuid(), "General", count: 3);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdultCount.Should().BeNull();
        result.Value.ChildCount.Should().BeNull();
        result.Value.HasAgeSplit.Should().BeFalse();
    }

    [Fact]
    public void TierCount_Create_HalfSetAdultOnly_IsRejected()
    {
        // AdultCount set, ChildCount null — half-set ambiguity.
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 3, childCount: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("both AdultCount and ChildCount must be set, or both null",
            because: "half-set ambiguity is rejected by architect edit #1");
    }

    [Fact]
    public void TierCount_Create_HalfSetChildOnly_IsRejected()
    {
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: null, childCount: 3);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("both AdultCount and ChildCount must be set, or both null");
    }

    [Fact]
    public void TierCount_Create_AgeSumMismatchesCount_IsRejected()
    {
        // 2 + 2 = 4, but Count = 3.
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("must equal Count", because: "internal sum invariant");
    }

    [Fact]
    public void TierCount_Create_NegativeAgeCount_IsRejected()
    {
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: -1, childCount: 4);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TierCount_Create_AllAdults_ChildCountZero_IsAllowed()
    {
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count: 5, adultCount: 5, childCount: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAgeSplit.Should().BeTrue();
        result.Value.ChildCount.Should().Be(0);
    }

    [Fact]
    public void TierCount_Create_AllChildren_AdultCountZero_IsAllowed()
    {
        var result = TierCount.Create(Guid.NewGuid(), "Family", count: 4, adultCount: 0, childCount: 4);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAgeSplit.Should().BeTrue();
        result.Value.AdultCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  HeadCountBreakdown cross-axis invariants — architect edit #3
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void HeadCountBreakdown_B2_TierAgeMatchesDemographics_Succeeds()
    {
        // 3 adults + 2 children = 5 total; tier age split sums to (3, 2)
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 1).Value;
        var general = TierCount.Create(Guid.NewGuid(), "General", count: 2, adultCount: 1, childCount: 1).Value;

        var result = HeadCountBreakdown.ForByAge(adults: 3, children: 2, new[] { vip, general });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void HeadCountBreakdown_B2_TierAgeMismatchesDemographics_IsRejected()
    {
        // Tier age says (5 adults, 0 children); Demographics says (3, 2)
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 5, adultCount: 5, childCount: 0).Value;

        var result = HeadCountBreakdown.ForByAge(adults: 3, children: 2, new[] { vip });

        result.IsFailure.Should().BeTrue();
        string.Join("; ", result.Errors!).Should().Contain("tier", because: "cross-axis invariant violation");
    }

    [Fact]
    public void HeadCountBreakdown_B4_TierAgeMatchesDemographics_Succeeds()
    {
        // B4: 2 AdultMales + 1 AdultFemale + 1 ChildMale + 1 ChildFemale = 5
        // Tier sums: AdultCount=3, ChildCount=2 → matches
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 5, adultCount: 3, childCount: 2).Value;

        var result = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 1, childMales: 1, childFemales: 1,
            new[] { vip });

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
    }

    [Fact]
    public void HeadCountBreakdown_B1_WithTierAgeSplit_IsRejected()
    {
        // B1 has no age axis; TierCount.AdultCount must be null.
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 1).Value;

        var result = HeadCountBreakdown.ForTotalOnly(total: 3, new[] { vip });

        result.IsFailure.Should().BeTrue();
        string.Join("; ", result.Errors!).Should().Match("*age axis*",
            because: "B1 doesn't capture age — tier age split is invalid in this mode");
    }

    [Fact]
    public void HeadCountBreakdown_B3_WithTierAgeSplit_IsRejected()
    {
        // B3 captures gender but not age.
        var vip = TierCount.Create(Guid.NewGuid(), "VIP", count: 3, adultCount: 2, childCount: 1).Value;

        var result = HeadCountBreakdown.ForByGender(males: 2, females: 1, new[] { vip });

        result.IsFailure.Should().BeTrue();
        string.Join("; ", result.Errors!).Should().Match("*age axis*");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Pricing — single-shape refactor (architect edit #5).
    //  Legacy null-axis must keep producing AdultPrice × Count for back-compat.
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Pricing_B2Tiered_AgeSplit_AdultsPayAdult_ChildrenPayChild()
    {
        var (ev, vip, _) = CreateTieredEventWithChildPricing(vipAdult: 50m, vipChild: 25m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        var tierVip = TierCount.Create(vip.Id, vip.Name, count: 3, adultCount: 2, childCount: 1).Value;
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1, new[] { tierVip }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("b2age@e.com"));

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(125m,
            "VIP × (2 adults × $50 + 1 child × $25) = $125");
    }

    [Fact]
    public void Pricing_B2Tiered_AgeSplit_MixedTiers()
    {
        // VIP × (2A,1C) + General × (3A, 0C)
        // Math: VIP = 2×50 + 1×25 = 125; General = 3×30 + 0×15 = 90; sum = 215
        var (ev, vip, general) = CreateTieredEventWithChildPricing(
            vipAdult: 50m, vipChild: 25m,
            generalAdult: 30m, generalChild: 15m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        var vipTc = TierCount.Create(vip.Id, vip.Name, count: 3, adultCount: 2, childCount: 1).Value;
        var generalTc = TierCount.Create(general.Id, general.Name, count: 3, adultCount: 3, childCount: 0).Value;
        var head = HeadCountBreakdown.ForByAge(adults: 5, children: 1, new[] { vipTc, generalTc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("mix@e.com"));

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(215m,
            "VIP (125) + General (90) = 215");
    }

    [Fact]
    public void Pricing_B4Tiered_AgeSplit()
    {
        // B4: 2 AM, 1 AF, 0 CM, 1 CF (total 4) on a single VIP tier
        // Adults = 3, Children = 1 → 3×$50 + 1×$25 = $175
        var (ev, vip, _) = CreateTieredEventWithChildPricing(vipAdult: 50m, vipChild: 25m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAgeAndGender).IsSuccess.Should().BeTrue();

        var vipTc = TierCount.Create(vip.Id, vip.Name, count: 4, adultCount: 3, childCount: 1).Value;
        var head = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 2, adultFemales: 1, childMales: 0, childFemales: 1,
            new[] { vipTc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("b4age@e.com"));

        result.IsSuccess.Should().BeTrue($"errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(175m);
    }

    [Fact]
    public void Pricing_LegacyB1Tiered_NullAxis_StillUsesAdultPriceTimesCount()
    {
        // Phase 7E.3c shipping behaviour: B1 + tiered → AdultPrice × Count.
        // Architect Q7: this path stays green indefinitely.
        var (ev, vip, _) = CreateTieredEventWithChildPricing(vipAdult: 50m, vipChild: 25m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();

        // Legacy: TierCount has no age split (both null).
        var vipTc = TierCount.Create(vip.Id, vip.Name, count: 3).Value;
        var head = HeadCountBreakdown.ForTotalOnly(3, new[] { vipTc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("b1legacy@e.com"));

        result.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(150m,
            "legacy null-axis path: AdultPrice $50 × 3 = $150 (no child discount applied even though tier has ChildPrice)");
    }

    [Fact]
    public void Pricing_LegacyB3Tiered_NullAxis_StillUsesAdultPriceTimesCount()
    {
        var (ev, vip, _) = CreateTieredEventWithChildPricing(vipAdult: 50m, vipChild: 25m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByGender).IsSuccess.Should().BeTrue();

        var vipTc = TierCount.Create(vip.Id, vip.Name, count: 4).Value;
        var head = HeadCountBreakdown.ForByGender(males: 3, females: 1, new[] { vipTc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("b3legacy@e.com"));

        result.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(200m,
            "legacy: $50 × 4 = $200");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Mode A vs Mode B parity with tier-age axis (architect anti-fork test)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parity_ModeA_vs_ModeB_WithTierAge_BillsIdentically()
    {
        // Same basket: VIP × (2 adults, 1 child) at $50/$25.
        // Mode A path
        var (modeAEvent, modeAVip, _) = CreateTieredEventWithChildPricing();
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("A1", AgeCategory.Adult, ticketTierId: modeAVip.Id, ticketTierName: modeAVip.Name).Value,
            AttendeeDetails.Create("A2", AgeCategory.Adult, ticketTierId: modeAVip.Id, ticketTierName: modeAVip.Name).Value,
            AttendeeDetails.Create("C1", AgeCategory.Child, ticketTierId: modeAVip.Id, ticketTierName: modeAVip.Name).Value,
        };
        var modeATotal = modeAEvent.CalculateTieredPriceForAttendees(attendees);
        modeATotal.IsSuccess.Should().BeTrue();

        // Mode B path with the new age-split axis
        var (modeBEvent, modeBVip, _) = CreateTieredEventWithChildPricing();
        modeBEvent.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();
        var tier = TierCount.Create(modeBVip.Id, modeBVip.Name, count: 3, adultCount: 2, childCount: 1).Value;
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1, new[] { tier }).Value;
        var modeBResult = modeBEvent.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("parity@e.com"));
        modeBResult.IsSuccess.Should().BeTrue();
        var modeBPrice = modeBEvent.Registrations.Single().TotalPrice!;

        modeBPrice.Amount.Should().Be(modeATotal.Value!.Amount,
            "Mode A and Mode B with the same basket must produce identical Money — anti-fork");
        modeBPrice.Amount.Should().Be(125m, "sanity check: 2×$50 + 1×$25 = $125");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Tier-with-no-ChildPrice guard — architect edit #8 (silent under-charge fix)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TierWithNoChildPrice_AllAdult_StillPrices()
    {
        // Tier has no ChildPrice; payload uses null axis (legacy) or all-adult split.
        var (ev, tier) = CreateTieredEventNoChildPricing(adultPrice: 40m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountOnly).IsSuccess.Should().BeTrue();

        var tc = TierCount.Create(tier.Id, tier.Name, count: 3).Value;
        var head = HeadCountBreakdown.ForTotalOnly(3, new[] { tc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("nocchild@e.com"));

        result.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(120m, "$40 × 3 = $120");
    }

    [Fact]
    public void TierWithNoChildPrice_ChildCountGreaterThanZero_IsRejected()
    {
        // Tier has no ChildPrice → AdultPrice would silently apply to children. Rejected.
        var (ev, tier) = CreateTieredEventNoChildPricing(adultPrice: 40m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        var tc = TierCount.Create(tier.Id, tier.Name, count: 3, adultCount: 2, childCount: 1).Value;
        var head = HeadCountBreakdown.ForByAge(adults: 2, children: 1, new[] { tc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("nochild2@e.com"));

        result.IsFailure.Should().BeTrue("tier has no ChildPrice — silent fallback would under-charge");
        string.Join("; ", result.Errors!).Should().Match("*child pricing*");
    }

    [Fact]
    public void TierWithNoChildPrice_ChildCountZero_IsAllowed()
    {
        // ChildCount=0 with an all-adult split — no child purchase, no rejection.
        var (ev, tier) = CreateTieredEventNoChildPricing(adultPrice: 40m);
        ev.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        var tc = TierCount.Create(tier.Id, tier.Name, count: 3, adultCount: 3, childCount: 0).Value;
        var head = HeadCountBreakdown.ForByAge(adults: 3, children: 0, new[] { tc }).Value;

        var result = ev.RegisterWithHeadCount(Guid.NewGuid(), "Lead", head, Contact("nochild3@e.com"));

        result.IsSuccess.Should().BeTrue();
        ev.Registrations.Single().TotalPrice!.Amount.Should().Be(120m, "$40 × 3 adults = $120");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  JSON deserialisation — legacy payload (no AdultCount/ChildCount fields)
    //  must still rehydrate cleanly with both null.
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TierCount_JsonDeserialise_LegacyPayload_BothAgeCountsNull()
    {
        var legacyJson =
            $$"""{"tierId":"{{Guid.NewGuid()}}","tierName":"VIP","count":3}""";

        var deserialised = System.Text.Json.JsonSerializer.Deserialize<TierCount>(
            legacyJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        deserialised.Should().NotBeNull();
        deserialised!.Count.Should().Be(3);
        deserialised.AdultCount.Should().BeNull();
        deserialised.ChildCount.Should().BeNull();
        deserialised.HasAgeSplit.Should().BeFalse();
    }
}
