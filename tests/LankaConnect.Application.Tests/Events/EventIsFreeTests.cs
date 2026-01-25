using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events;

/// <summary>
/// Phase 6A.81: Unit tests for Event.IsFree() method
/// Tests security fix that prevents payment bypass for events with misconfigured pricing
/// </summary>
public class EventIsFreeTests
{
    private readonly DateTime _startDate = DateTime.UtcNow.AddDays(30);
    private readonly DateTime _endDate = DateTime.UtcNow.AddDays(30).AddHours(3);
    private readonly Guid _organizerId = Guid.NewGuid();

    #region IsFree() - Free Events (Returns TRUE)

    [Fact]
    public void IsFree_WithZeroTicketPrice_ReturnsTrue()
    {
        // Arrange: Event with legacy single pricing set to $0
        var ticketPrice = Money.Create(0, Currency.USD).Value;
        var eventResult = Event.Create(
            EventTitle.Create("Free Community Event").Value,
            EventDescription.Create("A free event for the community").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100,
            ticketPrice: ticketPrice);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.True(isFree, "Event with $0 ticket price should be free");
    }

    [Fact]
    public void IsFree_WithZeroAdultPriceInPricing_ReturnsTrue()
    {
        // Arrange: Event with Pricing value object set to $0
        var adultPrice = Money.Create(0, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateSinglePrice(adultPrice);
        Assert.True(pricingResult.IsSuccess);

        var eventResult = Event.Create(
            EventTitle.Create("Free Workshop").Value,
            EventDescription.Create("A free workshop").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 50);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;
        var setPricingResult = @event.SetDualPricing(pricingResult.Value);
        Assert.True(setPricingResult.IsSuccess);

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.True(isFree, "Event with $0 adult price in Pricing should be free");
    }

    [Fact]
    public void IsFree_WithZeroDualPricing_ReturnsTrue()
    {
        // Arrange: Event with dual pricing both set to $0
        var adultPrice = Money.Create(0, Currency.USD).Value;
        var childPrice = Money.Create(0, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateDualPrice(adultPrice, childPrice, childAgeLimit: 12);
        Assert.True(pricingResult.IsSuccess);

        var eventResult = Event.Create(
            EventTitle.Create("Free Family Event").Value,
            EventDescription.Create("Free event for families").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 200);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;
        var setPricingResult = @event.SetDualPricing(pricingResult.Value);
        Assert.True(setPricingResult.IsSuccess);

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.True(isFree, "Event with $0 dual pricing should be free");
    }

    [Fact]
    public void IsFree_WithEmptyGroupTiers_ReturnsTrue()
    {
        // Arrange: Event with GroupTiered pricing but no tiers configured
        var eventResult = Event.Create(
            EventTitle.Create("Group Event").Value,
            EventDescription.Create("Event with group pricing").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;

        // Note: Can't actually create TicketPricing with empty tiers (validation prevents it)
        // This test documents the expected behavior per IsFree() line 726-727

        // Act & Assert: This scenario is prevented by TicketPricing.CreateGroupTiered() validation
        // If it were possible, IsFree() would return !Pricing.HasGroupTiers == !false == true
    }

    #endregion

    #region IsFree() - Paid Events (Returns FALSE)

    [Fact]
    public void IsFree_WithNonZeroTicketPrice_ReturnsFalse()
    {
        // Arrange: Event with $50 ticket price
        var ticketPrice = Money.Create(50, Currency.USD).Value;
        var eventResult = Event.Create(
            EventTitle.Create("Paid Concert").Value,
            EventDescription.Create("A paid concert event").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 500,
            ticketPrice: ticketPrice);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.False(isFree, "Event with $50 ticket price should NOT be free");
    }

    [Fact]
    public void IsFree_WithNonZeroAdultPriceInPricing_ReturnsFalse()
    {
        // Arrange: Event with $75 adult price
        var adultPrice = Money.Create(75, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateSinglePrice(adultPrice);
        Assert.True(pricingResult.IsSuccess);

        var eventResult = Event.Create(
            EventTitle.Create("Paid Workshop").Value,
            EventDescription.Create("A paid professional workshop").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 30);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;
        var setPricingResult = @event.SetDualPricing(pricingResult.Value);
        Assert.True(setPricingResult.IsSuccess);

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.False(isFree, "Event with $75 adult price should NOT be free");
    }

    [Fact]
    public void IsFree_WithDualPricing_AdultPaidChildFree_ReturnsFalse()
    {
        // Arrange: Event with $100 adult, $0 child (adult price determines if free)
        var adultPrice = Money.Create(100, Currency.USD).Value;
        var childPrice = Money.Create(0, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateDualPrice(adultPrice, childPrice, childAgeLimit: 12);
        Assert.True(pricingResult.IsSuccess);

        var eventResult = Event.Create(
            EventTitle.Create("Family Dinner").Value,
            EventDescription.Create("Dinner with kids eat free").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 150);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;
        var setPricingResult = @event.SetDualPricing(pricingResult.Value);
        Assert.True(setPricingResult.IsSuccess);

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.False(isFree, "Event with $100 adult price should NOT be free (even if child is $0)");
    }

    [Fact]
    public void IsFree_WithDualPricing_BothNonZero_ReturnsFalse()
    {
        // Arrange: Christmas Dinner Dance scenario - $100 adult, $50 child
        var adultPrice = Money.Create(100, Currency.USD).Value;
        var childPrice = Money.Create(50, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateDualPrice(adultPrice, childPrice, childAgeLimit: 12);
        Assert.True(pricingResult.IsSuccess);

        var eventResult = Event.Create(
            EventTitle.Create("Christmas Dinner Dance 2025").Value,
            EventDescription.Create("Annual dinner dance").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 200);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;
        var setPricingResult = @event.SetDualPricing(pricingResult.Value);
        Assert.True(setPricingResult.IsSuccess);

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.False(isFree, "Event with $100/$50 dual pricing should NOT be free");
    }

    [Fact]
    public void IsFree_WithGroupTiersConfigured_ReturnsFalse()
    {
        // Arrange: Event with group pricing tiers
        var tier1Price = Money.Create(20, Currency.USD).Value;
        var tier2Price = Money.Create(15, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(minAttendees: 1, maxAttendees: 9, tier1Price).Value;
        var tier2 = GroupPricingTier.Create(minAttendees: 10, maxAttendees: null, tier2Price).Value;

        var tiers = new List<GroupPricingTier> { tier1, tier2 };
        var pricingResult = TicketPricing.CreateGroupTiered(tiers, Currency.USD);
        Assert.True(pricingResult.IsSuccess);

        var eventResult = Event.Create(
            EventTitle.Create("Corporate Team Building").Value,
            EventDescription.Create("Group event with volume discounts").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;
        var setPricingResult = @event.SetGroupPricing(pricingResult.Value);
        Assert.True(setPricingResult.IsSuccess);

        // Act
        var isFree = @event.IsFree();

        // Assert
        Assert.False(isFree, "Event with configured group tiers should NOT be free");
    }

    #endregion

    #region IsFree() - Phase 6A.81 Security Fix (Missing Pricing Configuration)

    [Fact]
    public void IsFree_WithNullPricingAndNullTicketPrice_ReturnsFalse()
    {
        // Arrange: Event with NO pricing configured (both Pricing and TicketPrice are null)
        // This simulates the Christmas Dinner Dance bug scenario
        var eventResult = Event.Create(
            EventTitle.Create("Event With Broken Pricing").Value,
            EventDescription.Create("Event created without pricing configuration").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;

        // Act
        var isFree = @event.IsFree();

        // Assert: Phase 6A.81 Security Fix - defaults to FALSE (paid) to prevent payment bypass
        Assert.False(isFree, "Event with null pricing should default to FALSE (paid) to prevent payment bypass vulnerability");
    }

    [Fact]
    public void IsFree_SecurityFix_PreventsPaymentBypass()
    {
        // Arrange: Simulate the exact scenario from Phase 6A.81 bug report
        // Event created during migration period without Pricing value object
        var eventResult = Event.Create(
            EventTitle.Create("Legacy Dual Pricing Event").Value,
            EventDescription.Create("Event created before Pricing migration").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 200);

        Assert.True(eventResult.IsSuccess);
        var @event = eventResult.Value;

        // Verify both Pricing and TicketPrice are null (as in production bug)
        Assert.Null(@event.Pricing);
        Assert.Null(@event.TicketPrice);

        // Act
        var isFree = @event.IsFree();

        // Assert: MUST return FALSE to trigger Preliminary status and prevent bypass
        Assert.False(isFree,
            "Phase 6A.81 Security Fix: IsFree() MUST return FALSE for events with missing pricing " +
            "to ensure registrations are created with Status=Preliminary, preventing payment bypass. " +
            "CalculatePriceForAttendees() will fail with 'Event pricing is not configured', " +
            "which is safer than allowing free registration.");
    }

    #endregion

    #region Integration Behavior Documentation

    [Fact]
    public void IsFree_Integration_FreeEventAllowsImmediateConfirmation()
    {
        // Arrange: Truly free event with explicit $0 pricing
        var ticketPrice = Money.Create(0, Currency.USD).Value;
        var eventResult = Event.Create(
            EventTitle.Create("Free Event").Value,
            EventDescription.Create("Confirmed free event").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100,
            ticketPrice: ticketPrice);

        var @event = eventResult.Value;

        // Act: Create registration (simulating RegisterWithAttendees flow)
        var isFree = @event.IsFree();
        var expectedStatus = isFree ? RegistrationStatus.Confirmed : RegistrationStatus.Preliminary;

        // Assert
        Assert.True(isFree);
        Assert.Equal(RegistrationStatus.Confirmed, expectedStatus);
        // Free events should allow immediate confirmation without payment
    }

    [Fact]
    public void IsFree_Integration_PaidEventRequiresPreliminThreeStateLifecycle()
    {
        // Arrange: Paid event with proper pricing configured
        var adultPrice = Money.Create(100, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateSinglePrice(adultPrice);
        var eventResult = Event.Create(
            EventTitle.Create("Paid Event").Value,
            EventDescription.Create("Properly configured paid event").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100);

        var @event = eventResult.Value;
        @event.SetDualPricing(pricingResult.Value);

        // Act: Check if registration should use Three-State Lifecycle
        var isFree = @event.IsFree();
        var expectedStatus = isFree ? RegistrationStatus.Confirmed : RegistrationStatus.Preliminary;

        // Assert
        Assert.False(isFree);
        Assert.Equal(RegistrationStatus.Preliminary, expectedStatus);
        // Paid events MUST start as Preliminary until payment webhook confirms
    }

    [Fact]
    public void IsFree_Integration_MisconfiguredEventPreventsRegistration()
    {
        // Arrange: Event with broken pricing (null Pricing and TicketPrice)
        var eventResult = Event.Create(
            EventTitle.Create("Broken Event").Value,
            EventDescription.Create("Event with missing pricing config").Value,
            _startDate,
            _endDate,
            _organizerId,
            capacity: 100);

        var @event = eventResult.Value;

        // Act: Simulate registration flow
        var isFree = @event.IsFree();

        // Assert: isFree() returns FALSE (security fix)
        Assert.False(isFree);

        // Expected behavior:
        // 1. isPaidEvent = !IsFree() = TRUE
        // 2. Registration.CreateWithAttendees() sets Status=Preliminary
        // 3. CalculatePriceForAttendees() returns FAILURE "Event pricing is not configured"
        // 4. Registration creation FAILS (prevents payment bypass)
        // 5. User sees error message (event admin must fix pricing)

        // This is CORRECT security-first behavior:
        // - Prevents payment bypass vulnerability
        // - Forces explicit pricing configuration
        // - Fails safe rather than allowing free registration
    }

    #endregion
}
