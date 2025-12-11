using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Phase 6D: Unit tests for Event entity with GroupTiered pricing support
/// Tests SetGroupPricing() and updated CalculatePriceForAttendees() with group tiers
/// </summary>
public class EventGroupPricingTests
{
    private Event CreateValidEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var organizerId = Guid.NewGuid();

        return Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            capacity: 100
        ).Value;
    }

    #region SetGroupPricing Tests

    [Fact]
    public void SetGroupPricing_WithValidTiers_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, Money.Create(15.00m, currency).Value).Value;
        var tier2 = GroupPricingTier.Create(3, 5, Money.Create(12.00m, currency).Value).Value;
        var tier3 = GroupPricingTier.Create(6, null, Money.Create(10.00m, currency).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2, tier3 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;

        // Act
        var result = @event.SetGroupPricing(pricing);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Pricing.Should().NotBeNull();
        @event.Pricing!.Type.Should().Be(PricingType.GroupTiered);
        @event.Pricing.GroupTiers.Should().HaveCount(3);
        @event.IsFree().Should().BeFalse();
    }

    [Fact]
    public void SetGroupPricing_WithNullPricing_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.SetGroupPricing(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pricing cannot be null");
    }

    [Fact]
    public void SetGroupPricing_WithNonGroupTieredPricing_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();
        var singlePrice = Money.Create(25.00m, Currency.USD).Value;
        var pricing = TicketPricing.CreateSinglePrice(singlePrice).Value;

        // Act
        var result = @event.SetGroupPricing(pricing);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("GroupTiered pricing type");
    }

    [Fact]
    public void SetGroupPricing_ShouldRaiseEventPricingUpdatedEvent()
    {
        // Arrange
        var @event = CreateValidEvent();
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 5, Money.Create(15.00m, currency).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;

        // Act
        var result = @event.SetGroupPricing(pricing);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.DomainEvents.Should().Contain(e => e.GetType().Name == "EventPricingUpdatedEvent");
    }

    #endregion

    #region CalculatePriceForAttendees - Group Tiered Tests

    [Theory]
    [InlineData(1, 15.00)] // 1 attendee: tier 1 (1-2) → 1 × $15 = $15
    [InlineData(2, 30.00)] // 2 attendees: tier 1 (1-2) → 2 × $15 = $30
    [InlineData(3, 36.00)] // 3 attendees: tier 2 (3-5) → 3 × $12 = $36
    [InlineData(4, 48.00)] // 4 attendees: tier 2 (3-5) → 4 × $12 = $48
    [InlineData(5, 60.00)] // 5 attendees: tier 2 (3-5) → 5 × $12 = $60
    [InlineData(6, 60.00)] // 6 attendees: tier 3 (6+) → 6 × $10 = $60
    [InlineData(10, 100.00)] // 10 attendees: tier 3 (6+) → 10 × $10 = $100
    public void CalculatePriceForAttendees_WithGroupTieredPricing_ShouldCalculateCorrectly(int attendeeCount, decimal expectedAmount)
    {
        // Arrange
        var @event = CreateValidEvent();
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, Money.Create(15.00m, currency).Value).Value;
        var tier2 = GroupPricingTier.Create(3, 5, Money.Create(12.00m, currency).Value).Value;
        var tier3 = GroupPricingTier.Create(6, null, Money.Create(10.00m, currency).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2, tier3 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;
        @event.SetGroupPricing(pricing);

        // Create attendees (age doesn't matter for group pricing)
        var attendees = Enumerable.Range(1, attendeeCount)
            .Select(i => AttendeeDetails.Create($"Attendee {i}", 25).Value)
            .ToList();

        // Act
        var result = @event.CalculatePriceForAttendees(attendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(expectedAmount);
        result.Value.Currency.Should().Be(currency);
    }

    [Fact]
    public void CalculatePriceForAttendees_WithGroupTieredAndEmptyList_ShouldFail()
    {
        // Arrange
        var @event = CreateValidEvent();
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 5, Money.Create(15.00m, currency).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;
        @event.SetGroupPricing(pricing);

        var emptyAttendees = new List<AttendeeDetails>();

        // Act
        var result = @event.CalculatePriceForAttendees(emptyAttendees);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one attendee is required");
    }

    [Fact]
    public void CalculatePriceForAttendees_WithGroupTieredFreeEvent_ShouldReturnZero()
    {
        // Arrange
        var @event = CreateValidEvent();
        // Don't set any pricing - event is free

        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("Attendee 1", 25).Value,
            AttendeeDetails.Create("Attendee 2", 30).Value
        };

        // Act
        var result = @event.CalculatePriceForAttendees(attendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsZero.Should().BeTrue();
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void SetGroupPricing_ShouldNotBreakExistingDualPricingEvents()
    {
        // Arrange - Event with existing Dual pricing
        var @event = CreateValidEvent();
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var dualPricing = TicketPricing.CreateDualPrice(adultPrice, childPrice, 12).Value;
        @event.SetDualPricing(dualPricing);

        // Act - Try to set group pricing
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 5, Money.Create(15.00m, currency).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1 };
        var groupPricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;
        var result = @event.SetGroupPricing(groupPricing);

        // Assert - Should succeed and replace dual pricing
        result.IsSuccess.Should().BeTrue();
        @event.Pricing!.Type.Should().Be(PricingType.GroupTiered);
        @event.Pricing.HasChildPricing.Should().BeFalse();
        @event.Pricing.HasGroupTiers.Should().BeTrue();
    }

    [Fact]
    public void CalculatePriceForAttendees_WithLegacySinglePrice_ShouldStillWork()
    {
        // Arrange - Legacy single price (TicketPrice property)
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var ticketPrice = Money.Create(20.00m, Currency.USD).Value;

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            capacity: 100,
            ticketPrice: ticketPrice
        ).Value;

        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("Attendee 1", 25).Value,
            AttendeeDetails.Create("Attendee 2", 30).Value,
            AttendeeDetails.Create("Attendee 3", 35).Value
        };

        // Act
        var result = @event.CalculatePriceForAttendees(attendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(60.00m); // 3 × $20
    }

    [Fact]
    public void CalculatePriceForAttendees_WithDualPricing_ShouldStillWork()
    {
        // Arrange - Dual pricing (Session 21)
        var @event = CreateValidEvent();
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var dualPricing = TicketPricing.CreateDualPrice(adultPrice, childPrice, 12).Value;
        @event.SetDualPricing(dualPricing);

        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("Adult 1", 30).Value,    // $50
            AttendeeDetails.Create("Child 1", 8).Value,     // $25
            AttendeeDetails.Create("Child 2", 10).Value     // $25
        };

        // Act
        var result = @event.CalculatePriceForAttendees(attendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100.00m); // $50 + $25 + $25
    }

    #endregion

    #region IsFree Tests

    [Fact]
    public void IsFree_WithGroupTieredPricing_ShouldReturnFalse()
    {
        // Arrange
        var @event = CreateValidEvent();
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 5, Money.Create(15.00m, currency).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;
        @event.SetGroupPricing(pricing);

        // Act & Assert
        @event.IsFree().Should().BeFalse();
    }

    [Fact]
    public void IsFree_WithNoGroupTieredPricing_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateValidEvent();
        // No pricing set

        // Act & Assert
        @event.IsFree().Should().BeTrue();
    }

    #endregion
}
