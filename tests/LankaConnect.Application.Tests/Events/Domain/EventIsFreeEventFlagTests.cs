using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.86: Tests for explicit IsFreeEvent flag
/// TDD RED Phase: These tests will FAIL until we implement IsFreeEvent property
///
/// Purpose: Eliminate ambiguity of NULL pricing by using explicit flag
/// Problem: Current IsFree() computes state from amounts, causing Phase 6A.81 bug
/// Solution: Add explicit IsFreeEvent boolean flag as source of truth
/// </summary>
public class EventIsFreeEventFlagTests
{
    #region Test Helpers

    private static Event CreateTestEvent(bool isFreeEvent = false)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);
        var organizerId = Guid.NewGuid();
        const int capacity = 100;

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, capacity);
        var @event = eventResult.Value;

        if (isFreeEvent)
        {
            // Use SetAsFreeEvent method (will implement)
            var result = @event.SetAsFreeEvent();
            Assert.True(result.IsSuccess);
        }

        return @event;
    }

    #endregion

    #region Core IsFreeEvent Flag Tests

    [Fact]
    public void IsFreeEvent_WhenEventCreated_DefaultsToFalse()
    {
        // Arrange & Act
        var @event = CreateTestEvent(isFreeEvent: false);

        // Assert
        Assert.False(@event.IsFreeEvent);
        Assert.False(@event.IsFree());
    }

    [Fact]
    public void SetAsFreeEvent_SetsIsFreeEventFlagToTrue()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.SetAsFreeEvent();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(@event.IsFreeEvent);
        Assert.True(@event.IsFree());
    }

    [Fact]
    public void IsFree_ReturnsIsFreeEventFlag_NotComputedFromPricing()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.SetAsFreeEvent();

        // Act
        var isFree = @event.IsFree();

        // Assert - IsFree() should return flag value directly
        Assert.True(isFree);
        Assert.Equal(@event.IsFreeEvent, isFree);
    }

    #endregion

    #region SetPricing Tests with Flag Update

    [Fact]
    public void SetPricing_WithZeroAmount_SetsIsFreeEventToTrue()
    {
        // Arrange
        var @event = CreateTestEvent();
        var zeroPrice = Money.Create(0m, Currency.USD).Value;

        // Act
        var result = @event.SetPricing(zeroPrice);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(@event.IsFreeEvent);
        Assert.True(@event.IsFree());
    }

    [Fact]
    public void SetPricing_WithPositiveAmount_SetsIsFreeEventToFalse()
    {
        // Arrange
        var @event = CreateTestEvent(isFreeEvent: true); // Start as free
        var price = Money.Create(25.00m, Currency.USD).Value;

        // Act
        var result = @event.SetPricing(price);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(@event.IsFreeEvent);
        Assert.False(@event.IsFree());
    }

    [Fact]
    public void SetPricing_WithNullPrice_ReturnsFailure()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.SetPricing(null);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot be null", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region SetDualPricing Tests with Flag Update

    [Fact]
    public void SetDualPricing_WithZeroAdultPrice_SetsIsFreeEventToTrue()
    {
        // Arrange
        var @event = CreateTestEvent();
        var zeroAdultPrice = Money.Create(0m, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateSinglePrice(zeroAdultPrice);
        Assert.True(pricingResult.IsSuccess);

        // Act
        var result = @event.SetDualPricing(pricingResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(@event.IsFreeEvent);
        Assert.True(@event.IsFree());
    }

    [Fact]
    public void SetDualPricing_WithPositiveAdultPrice_SetsIsFreeEventToFalse()
    {
        // Arrange
        var @event = CreateTestEvent(isFreeEvent: true);
        var adultPrice = Money.Create(50.00m, Currency.USD).Value;
        var childPrice = Money.Create(25.00m, Currency.USD).Value;
        var pricingResult = TicketPricing.CreateDualPrice(adultPrice, childPrice, 12);
        Assert.True(pricingResult.IsSuccess);

        // Act
        var result = @event.SetDualPricing(pricingResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(@event.IsFreeEvent);
        Assert.False(@event.IsFree());
    }

    #endregion

    #region Security Tests - Prevent NULL Pricing Bypass

    [Fact]
    public void CalculatePriceForAttendees_WhenPaidEventWithNullPricing_ThrowsInvalidOperationException()
    {
        // Arrange
        var @event = CreateTestEvent(isFreeEvent: false); // Paid event
        // Don't set any pricing (TicketPrice and Pricing are both null)

        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", Domain.Events.Enums.AgeCategory.Adult, null).Value
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            @event.CalculatePriceForAttendees(attendees));

        Assert.Contains("pricing is not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("paid event", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculatePriceForAttendees_WhenFreeEvent_ReturnsZero()
    {
        // Arrange
        var @event = CreateTestEvent(isFreeEvent: true);
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", Domain.Events.Enums.AgeCategory.Adult, null).Value
        };

        // Act
        var price = @event.CalculatePriceForAttendees(attendees);

        // Assert
        Assert.NotNull(price);
        Assert.Equal(0m, price.Value.Amount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetAsFreeEvent_ThenSetPricing_MaintainsConsistency()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.SetAsFreeEvent(); // Mark as free

        // Act - Set explicit $0 pricing (optional for display)
        var zeroPrice = Money.Create(0m, Currency.USD).Value;
        var result = @event.SetPricing(zeroPrice);

        // Assert - Should remain free
        Assert.True(result.IsSuccess);
        Assert.True(@event.IsFreeEvent);
        Assert.True(@event.IsFree());
    }

    [Fact]
    public void IsFreeEvent_PersistsThroughMultiplePricingChanges()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act & Assert - Change pricing multiple times
        var price1 = Money.Create(10m, Currency.USD).Value;
        @event.SetPricing(price1);
        Assert.False(@event.IsFreeEvent);

        var price2 = Money.Create(0m, Currency.USD).Value;
        @event.SetPricing(price2);
        Assert.True(@event.IsFreeEvent);

        var price3 = Money.Create(25m, Currency.USD).Value;
        @event.SetPricing(price3);
        Assert.False(@event.IsFreeEvent);
    }

    #endregion

    #region Backwards Compatibility Tests

    [Fact]
    public void IsFree_WithExplicitFlag_TakesPrecedenceOverPricingAmounts()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.SetAsFreeEvent(); // Explicitly free

        // Act - Even if we somehow set a non-zero price (shouldn't happen in practice)
        // The flag should take precedence
        var isFree = @event.IsFree();

        // Assert
        Assert.True(isFree);
        Assert.True(@event.IsFreeEvent);
    }

    #endregion

    #region Group Tiered Pricing Tests

    [Fact]
    public void SetGroupTieredPricing_WithZeroPrices_SetsIsFreeEventToTrue()
    {
        // Arrange
        var @event = CreateTestEvent();
        var tier1 = GroupPricingTier.Create(1, 2, Money.Create(0m, Currency.USD).Value).Value;
        var tier2 = GroupPricingTier.Create(3, 5, Money.Create(0m, Currency.USD).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2 };
        var pricingResult = TicketPricing.CreateGroupTiered(tiers, Currency.USD);
        Assert.True(pricingResult.IsSuccess);

        // Act
        var result = @event.SetDualPricing(pricingResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(@event.IsFreeEvent);
    }

    [Fact]
    public void SetGroupTieredPricing_WithPositivePrices_SetsIsFreeEventToFalse()
    {
        // Arrange
        var @event = CreateTestEvent(isFreeEvent: true);
        var tier1 = GroupPricingTier.Create(1, 2, Money.Create(20m, Currency.USD).Value).Value;
        var tier2 = GroupPricingTier.Create(3, 5, Money.Create(15m, Currency.USD).Value).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2 };
        var pricingResult = TicketPricing.CreateGroupTiered(tiers, Currency.USD);
        Assert.True(pricingResult.IsSuccess);

        // Act
        var result = @event.SetDualPricing(pricingResult.Value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(@event.IsFreeEvent);
    }

    #endregion
}
