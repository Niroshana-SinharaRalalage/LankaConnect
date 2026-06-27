using System.Text.Json;
using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 7E.1 — Domain tests for RegistrationMode + composite HeadCountBreakdown VO.
///
/// Coverage:
/// 1. RegistrationMode enum default behaviour on Event.
/// 2. Event.SetRegistrationMode rules (idempotent, blocked once registrations exist).
/// 3. TierCount factory validation.
/// 4. HeadCountBreakdown factories (ForTotalOnly / ForByAge / ForByGender / ForByAgeAndGender) —
///    Total auto-derivation from leaves; tier-count sum invariant.
/// 5. Registration mode snapshot at construction.
/// 6. Mutual exclusion of Attendees vs HeadCount.
/// 7. GetAttendeeCount() — the single canonical mutation point that makes capacity aggregators
///    automatically Mode-B aware.
/// 8. JSON round-trip (architect-required defensive test) — exercises the custom ValueConverter
///    pipeline that backs the head_count JSONB column.
/// </summary>
public class Phase7E1RegistrationModeTests
{
    #region Helpers

    private static Event CreateEvent(int capacity = 100)
    {
        var title = EventTitle.Create("Phase 7E Test Event").Value;
        var description = EventDescription.Create("Mode flexibility tests").Value;
        var start = DateTime.UtcNow.AddDays(7);
        var end = DateTime.UtcNow.AddDays(8);
        return Event.Create(title, description, start, end, Guid.NewGuid(), capacity).Value;
    }

    private static RegistrationContact CreateContact() =>
        RegistrationContact.Create("test@example.com", "555-0100", null).Value;

    private static AttendeeDetails CreateAttendee(string name = "Niroshana") =>
        AttendeeDetails.Create(name, AgeCategory.Adult).Value;

    #endregion

    #region 1. RegistrationMode default

    [Fact]
    public void Event_RegistrationMode_DefaultsTo_DetailedAttendees_OnCreation()
    {
        var @event = CreateEvent();
        @event.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
    }

    [Fact]
    public void Registration_RegistrationMode_DefaultsTo_DetailedAttendees_ForAttendeeBasedRegistration()
    {
        var attendees = new[] { CreateAttendee() };
        var price = Money.Create(0m, Currency.USD).Value;

        var result = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), attendees, CreateContact(), price);

        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
        result.Value.HeadCount.Should().BeNull();
        result.Value.LeadAttendeeName.Should().BeNull();
    }

    #endregion

    #region 2. Event.SetRegistrationMode rules

    [Fact]
    public void Event_SetRegistrationMode_Succeeds_WhenNoRegistrationsExist()
    {
        var @event = CreateEvent();

        var result = @event.SetRegistrationMode(RegistrationMode.HeadCountByAge);

        result.IsSuccess.Should().BeTrue();
        @event.RegistrationMode.Should().Be(RegistrationMode.HeadCountByAge);
    }

    [Fact]
    public void Event_SetRegistrationMode_Idempotent_WhenSameMode()
    {
        var @event = CreateEvent();

        var first = @event.SetRegistrationMode(RegistrationMode.HeadCountOnly);
        var second = @event.SetRegistrationMode(RegistrationMode.HeadCountOnly);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        @event.RegistrationMode.Should().Be(RegistrationMode.HeadCountOnly);
    }

    [Fact]
    public void Event_SetRegistrationMode_Fails_WhenRegistrationsExist()
    {
        var @event = CreateEvent();
        @event.Publish();
        // Add a registration through the public API so _registrations is non-empty.
        @event.Register(Guid.NewGuid(), 1).IsSuccess.Should().BeTrue();

        var result = @event.SetRegistrationMode(RegistrationMode.HeadCountByAge);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cannot change registration mode"));
        @event.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees);
    }

    /// <summary>
    /// Phase 7E follow-up bug fix: SetRegistrationMode should ignore Cancelled / Refunded /
    /// Abandoned registrations because they are historical-only — they don't consume capacity
    /// and don't need attendee backfill on A↔B conversion. The dashboard's CurrentRegistrations
    /// already excludes them; the mode-change guard must use the same definition or organisers
    /// see "Cannot change mode (30 existing)" while the UI shows "0 registered".
    /// </summary>
    [Fact]
    public void Event_SetRegistrationMode_Succeeds_WhenAllRegistrationsAreCancelled()
    {
        var @event = CreateEvent();
        @event.Publish();
        var userId = Guid.NewGuid();
        @event.Register(userId, 1).IsSuccess.Should().BeTrue();

        // Cancel the only registration. Dashboard CurrentRegistrations should be 0 now.
        var registration = @event.Registrations.Single();
        registration.Cancel();

        @event.CurrentRegistrations.Should().Be(0,
            "sanity check — once cancelled, the registration should not count toward active");

        var result = @event.SetRegistrationMode(RegistrationMode.HeadCountByAge);

        result.IsSuccess.Should().BeTrue(
            $"a Cancelled registration leaves no active claim that needs backfill. Errors: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        @event.RegistrationMode.Should().Be(RegistrationMode.HeadCountByAge);
    }

    #endregion

    #region 3. TierCount factory

    [Fact]
    public void TierCount_Create_Succeeds_WithValidInputs()
    {
        var tierId = Guid.NewGuid();
        var result = TierCount.Create(tierId, "VIP", 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.TierId.Should().Be(tierId);
        result.Value.TierName.Should().Be("VIP");
        result.Value.Count.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TierCount_Create_Fails_WithMissingTierName(string? tierName)
    {
        var result = TierCount.Create(Guid.NewGuid(), tierName, 1);
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void TierCount_Create_Fails_WithNonPositiveCount(int count)
    {
        var result = TierCount.Create(Guid.NewGuid(), "VIP", count);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TierCount_Create_Fails_WithEmptyTierId()
    {
        var result = TierCount.Create(Guid.Empty, "VIP", 2);
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region 4. HeadCountBreakdown factories

    [Fact]
    public void HeadCountBreakdown_ForTotalOnly_AcceptsTotalDirectly()
    {
        var result = HeadCountBreakdown.ForTotalOnly(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(5);
        result.Value.Demographics.Should().BeNull();
        result.Value.TierCounts.Should().BeNull();
    }

    [Fact]
    public void HeadCountBreakdown_ForByAge_DerivesTotalFromLeaves()
    {
        var result = HeadCountBreakdown.ForByAge(adults: 3, children: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(5);
        result.Value.Demographics!.Adults.Should().Be(3);
        result.Value.Demographics.Children.Should().Be(2);
        result.Value.Demographics.Males.Should().BeNull();
    }

    [Fact]
    public void HeadCountBreakdown_ForByGender_DerivesTotalFromLeaves()
    {
        var result = HeadCountBreakdown.ForByGender(males: 4, females: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(5);
        result.Value.Demographics!.Males.Should().Be(4);
        result.Value.Demographics.Females.Should().Be(1);
        result.Value.Demographics.Adults.Should().BeNull();
    }

    [Fact]
    public void HeadCountBreakdown_ForByAgeAndGender_DerivesTotalFromFourLeaves()
    {
        var result = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 1, adultFemales: 2, childMales: 1, childFemales: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(5);
        result.Value.Demographics!.AdultMales.Should().Be(1);
        result.Value.Demographics.AdultFemales.Should().Be(2);
        result.Value.Demographics.ChildMales.Should().Be(1);
        result.Value.Demographics.ChildFemales.Should().Be(1);

        // ComputedAdults / ComputedChildren derive from leaves.
        result.Value.Demographics.ComputedAdults.Should().Be(3);
        result.Value.Demographics.ComputedChildren.Should().Be(2);
    }

    [Fact]
    public void HeadCountBreakdown_ForByAge_Rejects_BothZero()
    {
        var result = HeadCountBreakdown.ForByAge(0, 0);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void HeadCountBreakdown_ForByAge_Rejects_NegativeCount()
    {
        var result = HeadCountBreakdown.ForByAge(-1, 3);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void HeadCountBreakdown_TierCountSum_MustEqualTotal_OrFactoryFails()
    {
        var vipId = Guid.NewGuid();
        var genId = Guid.NewGuid();
        var validTiers = new[]
        {
            TierCount.Create(vipId, "VIP", 2).Value,
            TierCount.Create(genId, "General", 3).Value,
        };

        // Valid: 2 + 3 = 5 = Total
        var ok = HeadCountBreakdown.ForByAge(adults: 4, children: 1, tierCounts: validTiers);
        ok.IsSuccess.Should().BeTrue();

        // Invalid: 2 + 3 = 5 but Total is 6 (3 adults + 3 children)
        var bad = HeadCountBreakdown.ForByAge(adults: 3, children: 3, tierCounts: validTiers);
        bad.IsFailure.Should().BeTrue();
        bad.Errors.Should().Contain(e => e.Contains("Sum of tier counts"));
    }

    #endregion

    #region 5 + 6. Registration snapshot + mutual exclusion

    [Fact]
    public void Registration_CreateWithHeadCount_SnapshotsRegistrationMode()
    {
        var headCount = HeadCountBreakdown.ForByAge(2, 1).Value;
        var price = Money.Create(0m, Currency.USD).Value;

        var result = Registration.CreateWithHeadCount(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RegistrationMode.HeadCountByAge,
            "Niroshana",
            headCount,
            CreateContact(),
            price);

        result.IsSuccess.Should().BeTrue();
        result.Value.RegistrationMode.Should().Be(RegistrationMode.HeadCountByAge);
        result.Value.LeadAttendeeName.Should().Be("Niroshana");
        result.Value.HeadCount.Should().NotBeNull();
        result.Value.HeadCount!.Total.Should().Be(3);
    }

    [Fact]
    public void Registration_CreateWithHeadCount_Rejects_DetailedAttendeesMode()
    {
        var headCount = HeadCountBreakdown.ForTotalOnly(2).Value;
        var price = Money.Create(0m, Currency.USD).Value;

        var result = Registration.CreateWithHeadCount(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RegistrationMode.DetailedAttendees, // wrong factory for this mode
            "Niroshana",
            headCount,
            CreateContact(),
            price);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Registration_CreateWithHeadCount_Rejects_NoRegistrationMode()
    {
        var headCount = HeadCountBreakdown.ForTotalOnly(2).Value;
        var price = Money.Create(0m, Currency.USD).Value;

        var result = Registration.CreateWithHeadCount(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RegistrationMode.NoRegistration, // mode C should never produce Registration rows
            "Niroshana",
            headCount,
            CreateContact(),
            price);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Registration_HeadCountAndAttendees_AreMutuallyExclusive()
    {
        // CreateWithHeadCount produces NO attendees.
        var hc = HeadCountBreakdown.ForByAge(2, 1).Value;
        var price = Money.Create(0m, Currency.USD).Value;
        var bMode = Registration.CreateWithHeadCount(
            Guid.NewGuid(), null, RegistrationMode.HeadCountByAge,
            "Lead", hc, CreateContact(), price).Value;

        bMode.Attendees.Should().BeEmpty();
        bMode.HeadCount.Should().NotBeNull();

        // CreateWithAttendees produces NO HeadCount.
        var attendees = new[] { CreateAttendee() };
        var aMode = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), attendees, CreateContact(), price).Value;

        aMode.Attendees.Should().HaveCount(1);
        aMode.HeadCount.Should().BeNull();
        aMode.LeadAttendeeName.Should().BeNull();
    }

    #endregion

    #region 7. GetAttendeeCount canonical aggregator

    [Fact]
    public void GetAttendeeCount_HonorsHeadCountTotal_ForBMode()
    {
        var hc = HeadCountBreakdown.ForByAge(adults: 3, children: 2).Value;
        var price = Money.Create(0m, Currency.USD).Value;

        var registration = Registration.CreateWithHeadCount(
            Guid.NewGuid(), null, RegistrationMode.HeadCountByAge,
            "Lead", hc, CreateContact(), price).Value;

        registration.GetAttendeeCount().Should().Be(5);
    }

    [Fact]
    public void GetAttendeeCount_FallsBackToAttendees_ForAMode()
    {
        var attendees = new[] { CreateAttendee("A"), CreateAttendee("B"), CreateAttendee("C") };
        var price = Money.Create(0m, Currency.USD).Value;

        var registration = Registration.CreateWithAttendees(
            Guid.NewGuid(), Guid.NewGuid(), attendees, CreateContact(), price).Value;

        registration.GetAttendeeCount().Should().Be(3);
    }

    #endregion

    #region 8. JSON round-trip (architect-required defensive test)

    /// <summary>
    /// Architect-required: validates that the JSON serialisation pipeline used by the head_count
    /// JSONB column correctly round-trips a HeadCountBreakdown with all three axes populated.
    /// This is the in-memory equivalent of the staging round-trip test — failures here mean
    /// the JSONB column would silently drop or corrupt data.
    /// </summary>
    [Fact]
    public void HeadCountBreakdown_JsonRoundTrip_PreservesAllAxes()
    {
        var vipId = Guid.NewGuid();
        var genId = Guid.NewGuid();
        var original = HeadCountBreakdown.ForByAgeAndGender(
            adultMales: 1, adultFemales: 1, childMales: 0, childFemales: 1,
            tierCounts: new[]
            {
                TierCount.Create(vipId, "VIP", 1).Value,
                TierCount.Create(genId, "General", 2).Value,
            }).Value;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var json = JsonSerializer.Serialize(original, options);
        var rehydrated = JsonSerializer.Deserialize<HeadCountBreakdown>(json, options)!;

        rehydrated.Total.Should().Be(3);
        rehydrated.Demographics!.AdultMales.Should().Be(1);
        rehydrated.Demographics.AdultFemales.Should().Be(1);
        rehydrated.Demographics.ChildMales.Should().Be(0);
        rehydrated.Demographics.ChildFemales.Should().Be(1);
        rehydrated.TierCounts.Should().NotBeNull();
        rehydrated.TierCounts!.Should().HaveCount(2);
        rehydrated.TierCounts![0].TierName.Should().Be("VIP");
        rehydrated.TierCounts![0].Count.Should().Be(1);

        // ValueObject structural equality survives the round-trip.
        rehydrated.Should().Be(original);
    }

    [Fact]
    public void HeadCountBreakdown_JsonRoundTrip_HandlesB1_NoBreakdown_NoTiers()
    {
        var original = HeadCountBreakdown.ForTotalOnly(7).Value;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        var json = JsonSerializer.Serialize(original, options);
        var rehydrated = JsonSerializer.Deserialize<HeadCountBreakdown>(json, options)!;

        rehydrated.Total.Should().Be(7);
        rehydrated.Demographics.Should().BeNull();
        rehydrated.TierCounts.Should().BeNull();
        rehydrated.Should().Be(original);
    }

    #endregion
}
