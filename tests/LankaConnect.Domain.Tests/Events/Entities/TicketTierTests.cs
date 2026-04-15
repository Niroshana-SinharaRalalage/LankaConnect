using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class TicketTierTests
{
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Money _adultPrice = Money.Create(150.00m, Currency.USD).Value;
    private readonly Money _childPrice = Money.Create(100.00m, Currency.USD).Value;

    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        // Act
        var result = TicketTier.Create(
            _eventId, "VIP", "VIP seating with premium view",
            _adultPrice, _childPrice, childAgeLimit: 12,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tier = result.Value;
        tier.EventId.Should().Be(_eventId);
        tier.Name.Should().Be("VIP");
        tier.Description.Should().Be("VIP seating with premium view");
        tier.AdultPrice.Should().Be(_adultPrice);
        tier.ChildPrice.Should().Be(_childPrice);
        tier.ChildAgeLimit.Should().Be(12);
        tier.HasChildPricing.Should().BeTrue();
        tier.Capacity.Should().Be(30);
        tier.ReservedCount.Should().Be(0);
        tier.AvailableQuantity.Should().Be(30);
        tier.MaxPerUser.Should().Be(10);
        tier.SortOrder.Should().Be(1);
        tier.IsActive.Should().BeTrue();
        tier.IsFree.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutChildPricing_Should_Return_Success()
    {
        // Act
        var result = TicketTier.Create(
            _eventId, "Basic", "Standard entry",
            _adultPrice, childPrice: null, childAgeLimit: null,
            capacity: 50, maxPerUser: 10, sortOrder: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasChildPricing.Should().BeFalse();
        result.Value.ChildPrice.Should().BeNull();
        result.Value.ChildAgeLimit.Should().BeNull();
    }

    [Fact]
    public void Create_FreeTier_Should_Return_Success()
    {
        // Arrange
        var freePrice = Money.Create(0m, Currency.USD).Value;

        // Act
        var result = TicketTier.Create(
            _eventId, "Free Entry", "Complimentary admission",
            freePrice, childPrice: null, childAgeLimit: null,
            capacity: 100, maxPerUser: 5, sortOrder: 4);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFree.Should().BeTrue();
        result.Value.AdultPrice.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyEventId_Should_Fail()
    {
        var result = TicketTier.Create(
            Guid.Empty, "VIP", null,
            _adultPrice, null, null,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    [Fact]
    public void Create_WithEmptyName_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "", null,
            _adultPrice, null, null,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Tier name is required");
    }

    [Fact]
    public void Create_WithNullAdultPrice_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "VIP", null,
            adultPrice: null!, childPrice: null, childAgeLimit: null,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Adult price is required");
    }

    [Fact]
    public void Create_WithZeroCapacity_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, null, null,
            capacity: 0, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Capacity must be greater than 0");
    }

    [Fact]
    public void Create_WithZeroMaxPerUser_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, null, null,
            capacity: 30, maxPerUser: 0, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Max per user must be greater than 0");
    }

    [Fact]
    public void Create_WithChildPriceButNoAgeLimit_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, _childPrice, childAgeLimit: null,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Both child price and age limit are required");
    }

    [Fact]
    public void Create_WithAgeLimitButNoChildPrice_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, childPrice: null, childAgeLimit: 12,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Both child price and age limit are required");
    }

    [Fact]
    public void Create_WithInvalidChildAgeLimit_Should_Fail()
    {
        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, _childPrice, childAgeLimit: 20,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Child age limit must be between 1 and 18");
    }

    [Fact]
    public void Create_WithChildPriceGreaterThanAdult_Should_Fail()
    {
        var expensiveChildPrice = Money.Create(200.00m, Currency.USD).Value;

        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, expensiveChildPrice, childAgeLimit: 12,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Child price cannot be greater than adult price");
    }

    [Fact]
    public void Create_WithMismatchedCurrencies_Should_Fail()
    {
        var eurChildPrice = Money.Create(80.00m, Currency.EUR).Value;

        var result = TicketTier.Create(
            _eventId, "VIP", null,
            _adultPrice, eurChildPrice, childAgeLimit: 12,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Adult and child prices must use the same currency");
    }

    [Fact]
    public void Create_TrimsName()
    {
        var result = TicketTier.Create(
            _eventId, "  VIP  ", null,
            _adultPrice, null, null,
            capacity: 30, maxPerUser: 10, sortOrder: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("VIP");
    }

    #endregion

    #region Reserve / Release Tests

    [Fact]
    public void Reserve_WithValidQuantity_Should_Succeed()
    {
        var tier = CreateValidTier(capacity: 30);

        var result = tier.Reserve(5);

        result.IsSuccess.Should().BeTrue();
        tier.ReservedCount.Should().Be(5);
        tier.AvailableQuantity.Should().Be(25);
    }

    [Fact]
    public void Reserve_ExceedingCapacity_Should_Fail()
    {
        var tier = CreateValidTier(capacity: 10);

        var result = tier.Reserve(11);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient capacity");
    }

    [Fact]
    public void Reserve_ZeroQuantity_Should_Fail()
    {
        var tier = CreateValidTier(capacity: 30);

        var result = tier.Reserve(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity must be greater than 0");
    }

    [Fact]
    public void Release_WithValidQuantity_Should_Succeed()
    {
        var tier = CreateValidTier(capacity: 30);
        tier.Reserve(10);

        var result = tier.Release(5);

        result.IsSuccess.Should().BeTrue();
        tier.ReservedCount.Should().Be(5);
        tier.AvailableQuantity.Should().Be(25);
    }

    [Fact]
    public void Release_MoreThanReserved_Should_Fail()
    {
        var tier = CreateValidTier(capacity: 30);
        tier.Reserve(3);

        var result = tier.Release(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot release more than reserved");
    }

    [Fact]
    public void Release_ZeroQuantity_Should_Fail()
    {
        var tier = CreateValidTier(capacity: 30);

        var result = tier.Release(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Quantity must be greater than 0");
    }

    #endregion

    #region HasCapacityFor / CanUserPurchase Tests

    [Fact]
    public void HasCapacityFor_WithSufficientCapacity_Should_ReturnTrue()
    {
        var tier = CreateValidTier(capacity: 30);

        tier.HasCapacityFor(10).Should().BeTrue();
    }

    [Fact]
    public void HasCapacityFor_ExactlyAtCapacity_Should_ReturnTrue()
    {
        var tier = CreateValidTier(capacity: 30);

        tier.HasCapacityFor(30).Should().BeTrue();
    }

    [Fact]
    public void HasCapacityFor_ExceedingCapacity_Should_ReturnFalse()
    {
        var tier = CreateValidTier(capacity: 30);

        tier.HasCapacityFor(31).Should().BeFalse();
    }

    [Fact]
    public void HasCapacityFor_AfterReservation_ShouldAccountForReserved()
    {
        var tier = CreateValidTier(capacity: 30);
        tier.Reserve(25);

        tier.HasCapacityFor(5).Should().BeTrue();
        tier.HasCapacityFor(6).Should().BeFalse();
    }

    [Fact]
    public void CanUserPurchase_WithinLimit_Should_ReturnTrue()
    {
        var tier = CreateValidTier(capacity: 30, maxPerUser: 5);

        tier.CanUserPurchase(3, userCurrentQuantity: 0).Should().BeTrue();
    }

    [Fact]
    public void CanUserPurchase_ExceedingLimit_Should_ReturnFalse()
    {
        var tier = CreateValidTier(capacity: 30, maxPerUser: 5);

        tier.CanUserPurchase(3, userCurrentQuantity: 4).Should().BeFalse();
    }

    #endregion

    #region CalculatePriceForAttendee Tests

    [Fact]
    public void CalculatePriceForAttendee_Adult_ReturnsAdultPrice()
    {
        var tier = CreateTierWithChildPricing();

        var price = tier.CalculatePriceForAttendee(AgeCategory.Adult);

        price.Should().Be(_adultPrice);
    }

    [Fact]
    public void CalculatePriceForAttendee_Child_WithChildPricing_ReturnsChildPrice()
    {
        var tier = CreateTierWithChildPricing();

        var price = tier.CalculatePriceForAttendee(AgeCategory.Child);

        price.Should().Be(_childPrice);
    }

    [Fact]
    public void CalculatePriceForAttendee_Child_WithoutChildPricing_ReturnsAdultPrice()
    {
        var tier = CreateValidTier(capacity: 30);

        var price = tier.CalculatePriceForAttendee(AgeCategory.Child);

        price.Should().Be(_adultPrice);
    }

    [Fact]
    public void CalculatePriceForAttendee_FreeTier_ReturnsZero()
    {
        var freePrice = Money.Create(0m, Currency.USD).Value;
        var tier = TicketTier.Create(
            _eventId, "Free", null,
            freePrice, null, null,
            capacity: 100, maxPerUser: 5, sortOrder: 1).Value;

        var price = tier.CalculatePriceForAttendee(AgeCategory.Adult);

        price.IsZero.Should().BeTrue();
    }

    #endregion

    #region Update / Deactivate Tests

    [Fact]
    public void Update_WithValidData_Should_Succeed()
    {
        var tier = CreateValidTier(capacity: 30);
        var newPrice = Money.Create(200.00m, Currency.USD).Value;

        var result = tier.Update("Platinum", "Premium experience", newPrice, null, null, 40, 8, 0);

        result.IsSuccess.Should().BeTrue();
        tier.Name.Should().Be("Platinum");
        tier.Description.Should().Be("Premium experience");
        tier.AdultPrice.Should().Be(newPrice);
        tier.Capacity.Should().Be(40);
        tier.MaxPerUser.Should().Be(8);
    }

    [Fact]
    public void Update_CapacityBelowReserved_Should_Fail()
    {
        var tier = CreateValidTier(capacity: 30);
        tier.Reserve(20);

        var result = tier.Update("VIP", null, _adultPrice, null, null, 15, 10, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot reduce capacity below reserved count");
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse()
    {
        var tier = CreateValidTier(capacity: 30);

        var result = tier.Deactivate();

        result.IsSuccess.Should().BeTrue();
        tier.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WithReservations_Should_Fail()
    {
        var tier = CreateValidTier(capacity: 30);
        tier.Reserve(5);

        var result = tier.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot deactivate tier with existing reservations");
    }

    [Fact]
    public void Activate_Should_SetIsActiveTrue()
    {
        var tier = CreateValidTier(capacity: 30);
        tier.Deactivate();

        var result = tier.Activate();

        result.IsSuccess.Should().BeTrue();
        tier.IsActive.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private TicketTier CreateValidTier(int capacity, int maxPerUser = 10)
    {
        return TicketTier.Create(
            _eventId, "VIP", "VIP seating",
            _adultPrice, childPrice: null, childAgeLimit: null,
            capacity: capacity, maxPerUser: maxPerUser, sortOrder: 1).Value;
    }

    private TicketTier CreateTierWithChildPricing()
    {
        return TicketTier.Create(
            _eventId, "VIP", "VIP seating",
            _adultPrice, _childPrice, childAgeLimit: 12,
            capacity: 30, maxPerUser: 10, sortOrder: 1).Value;
    }

    #endregion
}
