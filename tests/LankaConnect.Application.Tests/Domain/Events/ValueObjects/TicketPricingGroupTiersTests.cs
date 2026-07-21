using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.SharedKernel.Money;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

/// <summary>
/// Phase 6D: Unit tests for TicketPricing with GroupTiered pricing support
/// Tests the extended TicketPricing value object with PricingType and GroupTiers
/// </summary>
public class TicketPricingGroupTiersTests
{
    #region Factory Method Tests - Single Pricing

    [Fact]
    public void CreateSinglePrice_WithValidPrice_ShouldSucceed()
    {
        // Arrange
        var price = new Money(25.00m, Currency.USD);

        // Act
        var result = TicketPricing.CreateSinglePrice(price);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(PricingType.Single);
        result.Value.AdultPrice.Should().Be(price);
        result.Value.ChildPrice.Should().BeNull();
        result.Value.ChildAgeLimit.Should().BeNull();
        result.Value.GroupTiers.Should().BeEmpty();
        result.Value.HasChildPricing.Should().BeFalse();
        result.Value.HasGroupTiers.Should().BeFalse();
    }

    [Fact]
    public void CreateSinglePrice_WithNullPrice_ShouldFail()
    {
        // Act
        var result = TicketPricing.CreateSinglePrice(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("price is required");
    }

    #endregion

    #region Factory Method Tests - Dual Pricing

    [Fact]
    public void CreateDualPrice_WithValidPrices_ShouldSucceed()
    {
        // Arrange
        var adultPrice = new Money(50.00m, Currency.USD);
        var childPrice = new Money(25.00m, Currency.USD);
        var childAgeLimit = 12;

        // Act
        var result = TicketPricing.CreateDualPrice(adultPrice, childPrice, childAgeLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(PricingType.AgeDual);
        result.Value.AdultPrice.Should().Be(adultPrice);
        result.Value.ChildPrice.Should().Be(childPrice);
        result.Value.ChildAgeLimit.Should().Be(childAgeLimit);
        result.Value.GroupTiers.Should().BeEmpty();
        result.Value.HasChildPricing.Should().BeTrue();
        result.Value.HasGroupTiers.Should().BeFalse();
    }

    #endregion

    #region Factory Method Tests - Group Tiered Pricing

    [Fact]
    public void CreateGroupTiered_WithValidTiers_ShouldSucceed()
    {
        // Arrange
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(3, 5, new Money(12.00m, currency)).Value;
        var tier3 = GroupPricingTier.Create(6, null, new Money(10.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2, tier3 };

        // Act
        var result = TicketPricing.CreateGroupTiered(tiers, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(PricingType.GroupTiered);
        result.Value.GroupTiers.Should().HaveCount(3);
        result.Value.GroupTiers.Should().ContainInOrder(tier1, tier2, tier3);
        result.Value.Currency.Should().Be(currency);
        result.Value.HasGroupTiers.Should().BeTrue();
        result.Value.HasChildPricing.Should().BeFalse();
    }

    [Fact]
    public void CreateGroupTiered_WithEmptyTiers_ShouldFail()
    {
        // Arrange
        var tiers = new List<GroupPricingTier>();

        // Act
        var result = TicketPricing.CreateGroupTiered(tiers, Currency.USD);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one tier is required");
    }

    [Fact]
    public void CreateGroupTiered_WithNullTiers_ShouldFail()
    {
        // Act
        var result = TicketPricing.CreateGroupTiered(null, Currency.USD);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one tier is required");
    }

    [Fact]
    public void CreateGroupTiered_WithOverlappingTiers_ShouldFail()
    {
        // Arrange - Tiers 1-3 and 2-5 overlap
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 3, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(2, 5, new Money(12.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2 };

        // Act
        var result = TicketPricing.CreateGroupTiered(tiers, currency);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("overlap");
    }

    [Fact]
    public void CreateGroupTiered_WithGapInTiers_ShouldFail()
    {
        // Arrange - Gap between tier1 (1-2) and tier2 (4-5), missing 3
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(4, 5, new Money(12.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2 };

        // Act
        var result = TicketPricing.CreateGroupTiered(tiers, currency);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("gap");
    }

    [Fact]
    public void CreateGroupTiered_NotStartingAtOne_ShouldFail()
    {
        // Arrange - Starts at 2 instead of 1
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(2, 5, new Money(15.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1 };

        // Act
        var result = TicketPricing.CreateGroupTiered(tiers, currency);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("must start at 1");
    }

    [Fact]
    public void CreateGroupTiered_WithMixedCurrencies_ShouldFail()
    {
        // Arrange - Tiers have different currencies
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, Currency.USD)).Value;
        var tier2 = GroupPricingTier.Create(3, 5, new Money(12.00m, Currency.LKR)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2 };

        // Act
        var result = TicketPricing.CreateGroupTiered(tiers, Currency.USD);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("same currency");
    }

    #endregion

    #region Tier Finding Tests

    [Theory]
    [InlineData(1, 0)] // 1 attendee -> tier 1 (1-2)
    [InlineData(2, 0)] // 2 attendees -> tier 1 (1-2)
    [InlineData(3, 1)] // 3 attendees -> tier 2 (3-5)
    [InlineData(5, 1)] // 5 attendees -> tier 2 (3-5)
    [InlineData(6, 2)] // 6 attendees -> tier 3 (6+)
    [InlineData(100, 2)] // 100 attendees -> tier 3 (6+)
    public void FindTierForAttendeeCount_WithValidCount_ShouldReturnCorrectTier(int attendeeCount, int expectedTierIndex)
    {
        // Arrange
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(3, 5, new Money(12.00m, currency)).Value;
        var tier3 = GroupPricingTier.Create(6, null, new Money(10.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2, tier3 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;

        // Act
        var result = pricing.FindTierForAttendeeCount(attendeeCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(tiers[expectedTierIndex]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FindTierForAttendeeCount_WithInvalidCount_ShouldFail(int invalidCount)
    {
        // Arrange
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;

        // Act
        var result = pricing.FindTierForAttendeeCount(invalidCount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least 1");
    }

    [Fact]
    public void FindTierForAttendeeCount_OnNonGroupTieredPricing_ShouldFail()
    {
        // Arrange - Single pricing
        var price = new Money(25.00m, Currency.USD);
        var pricing = TicketPricing.CreateSinglePrice(price).Value;

        // Act
        var result = pricing.FindTierForAttendeeCount(5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("only available for GroupTiered");
    }

    #endregion

    #region Calculate Group Price Tests

    [Theory]
    [InlineData(1, 15.00)] // 1 attendee: 1 Ã— $15 = $15
    [InlineData(2, 30.00)] // 2 attendees: 2 Ã— $15 = $30
    [InlineData(3, 36.00)] // 3 attendees: 3 Ã— $12 = $36
    [InlineData(5, 60.00)] // 5 attendees: 5 Ã— $12 = $60
    [InlineData(6, 60.00)] // 6 attendees: 6 Ã— $10 = $60
    [InlineData(10, 100.00)] // 10 attendees: 10 Ã— $10 = $100
    public void CalculateGroupPrice_WithValidCount_ShouldReturnCorrectPrice(int attendeeCount, decimal expectedAmount)
    {
        // Arrange
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(3, 5, new Money(12.00m, currency)).Value;
        var tier3 = GroupPricingTier.Create(6, null, new Money(10.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2, tier3 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;

        // Act
        var result = pricing.CalculateGroupPrice(attendeeCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(expectedAmount);
        result.Value.Currency.Should().Be(currency);
    }

    [Fact]
    public void CalculateGroupPrice_OnNonGroupTieredPricing_ShouldFail()
    {
        // Arrange - Dual pricing
        var adultPrice = new Money(50.00m, Currency.USD);
        var childPrice = new Money(25.00m, Currency.USD);
        var pricing = TicketPricing.CreateDualPrice(adultPrice, childPrice, 12).Value;

        // Act
        var result = pricing.CalculateGroupPrice(5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("only available for GroupTiered");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ForGroupTiered_ShouldFormatCorrectly()
    {
        // Arrange
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(3, null, new Money(12.00m, currency)).Value;
        var tiers = new List<GroupPricingTier> { tier1, tier2 };
        var pricing = TicketPricing.CreateGroupTiered(tiers, currency).Value;

        // Act
        var result = pricing.ToString();

        // Assert
        result.Should().Contain("Group Pricing");
        result.Should().Contain("1-2");
        result.Should().Contain("3+");
        result.Should().Contain("$15.00");
        result.Should().Contain("$12.00");
    }

    #endregion

    #region Value Object Equality Tests

    [Fact]
    public void ValueObjectEquality_GroupTieredWithSameTiers_ShouldBeEqual()
    {
        // Arrange
        var currency = Currency.USD;
        var tier1 = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier2 = GroupPricingTier.Create(3, null, new Money(12.00m, currency)).Value;
        var tiers1 = new List<GroupPricingTier> { tier1, tier2 };
        var tiers2 = new List<GroupPricingTier> { tier1, tier2 };
        var pricing1 = TicketPricing.CreateGroupTiered(tiers1, currency).Value;
        var pricing2 = TicketPricing.CreateGroupTiered(tiers2, currency).Value;

        // Assert
        pricing1.Should().Be(pricing2);
        (pricing1 == pricing2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_GroupTieredWithDifferentTiers_ShouldNotBeEqual()
    {
        // Arrange
        var currency = Currency.USD;
        var tier1a = GroupPricingTier.Create(1, 2, new Money(15.00m, currency)).Value;
        var tier1b = GroupPricingTier.Create(1, 2, new Money(20.00m, currency)).Value; // Different price
        var tiers1 = new List<GroupPricingTier> { tier1a };
        var tiers2 = new List<GroupPricingTier> { tier1b };
        var pricing1 = TicketPricing.CreateGroupTiered(tiers1, currency).Value;
        var pricing2 = TicketPricing.CreateGroupTiered(tiers2, currency).Value;

        // Assert
        pricing1.Should().NotBe(pricing2);
        (pricing1 == pricing2).Should().BeFalse();
    }

    #endregion
}
