using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

/// <summary>
/// Phase 6D: Unit tests for GroupPricingTier value object
/// Tests quantity-based pricing tiers for group event registrations
/// </summary>
public class GroupPricingTierTests
{
    #region Valid Creation Tests

    [Fact]
    public void Create_WithValidMinMaxAndPrice_ShouldSucceed()
    {
        // Arrange
        var minAttendees = 1;
        var maxAttendees = 2;
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;

        // Act
        var result = GroupPricingTier.Create(minAttendees, maxAttendees, pricePerPerson);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinAttendees.Should().Be(minAttendees);
        result.Value.MaxAttendees.Should().Be(maxAttendees);
        result.Value.PricePerPerson.Should().Be(pricePerPerson);
    }

    [Fact]
    public void Create_WithNullMaxAttendees_ShouldSucceed()
    {
        // Arrange - Represents "3+ attendees" tier
        var minAttendees = 3;
        int? maxAttendees = null;
        var pricePerPerson = Money.Create(10.00m, Currency.USD).Value;

        // Act
        var result = GroupPricingTier.Create(minAttendees, maxAttendees, pricePerPerson);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinAttendees.Should().Be(minAttendees);
        result.Value.MaxAttendees.Should().BeNull();
        result.Value.PricePerPerson.Should().Be(pricePerPerson);
        result.Value.IsUnlimitedTier.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMinAttendeesEqualToMax_ShouldSucceed()
    {
        // Arrange - Represents "exactly 5 attendees" tier
        var minAttendees = 5;
        var maxAttendees = 5;
        var pricePerPerson = Money.Create(12.00m, Currency.USD).Value;

        // Act
        var result = GroupPricingTier.Create(minAttendees, maxAttendees, pricePerPerson);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinAttendees.Should().Be(minAttendees);
        result.Value.MaxAttendees.Should().Be(maxAttendees);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithInvalidMinAttendees_ShouldFail(int invalidMin)
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;

        // Act
        var result = GroupPricingTier.Create(invalidMin, 5, pricePerPerson);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("MinAttendees must be at least 1");
    }

    [Fact]
    public void Create_WithMaxAttendeesLessThanMin_ShouldFail()
    {
        // Arrange
        var minAttendees = 5;
        var maxAttendees = 3; // Less than min
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;

        // Act
        var result = GroupPricingTier.Create(minAttendees, maxAttendees, pricePerPerson);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("MaxAttendees must be greater than or equal to MinAttendees");
    }

    [Fact]
    public void Create_WithNullPricePerPerson_ShouldFail()
    {
        // Act
        var result = GroupPricingTier.Create(1, 2, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PricePerPerson is required");
    }

    [Fact]
    public void Create_WithNegativePricePerPerson_ShouldFail()
    {
        // Arrange - Money.Create should already prevent negative amounts,
        // but test the tier creation logic
        var negativePrice = Money.Create(-10.00m, Currency.USD);

        // Assert
        negativePrice.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Tier Matching Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void CoversAttendeeCount_WithDefinedMaxAttendees_ShouldReturnCorrectResult(int attendeeCount, bool expected)
    {
        // Arrange - Tier covers 1-2 attendees
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier = GroupPricingTier.Create(1, 2, pricePerPerson).Value;

        // Act
        var result = tier.CoversAttendeeCount(attendeeCount);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(100, true)]
    [InlineData(2, false)]
    public void CoversAttendeeCount_WithUnlimitedTier_ShouldReturnCorrectResult(int attendeeCount, bool expected)
    {
        // Arrange - Tier covers 3+ attendees (unlimited)
        var pricePerPerson = Money.Create(10.00m, Currency.USD).Value;
        var tier = GroupPricingTier.Create(3, null, pricePerPerson).Value;

        // Act
        var result = tier.CoversAttendeeCount(attendeeCount);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CoversAttendeeCount_WithInvalidAttendeeCount_ShouldReturnFalse(int invalidCount)
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier = GroupPricingTier.Create(1, 2, pricePerPerson).Value;

        // Act
        var result = tier.CoversAttendeeCount(invalidCount);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Overlap Detection Tests

    [Fact]
    public void OverlapsWith_WithOverlappingRanges_ShouldReturnTrue()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(1, 3, pricePerPerson).Value; // 1-3
        var tier2 = GroupPricingTier.Create(2, 5, pricePerPerson).Value; // 2-5 (overlaps)

        // Act
        var result = tier1.OverlapsWith(tier2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WithAdjacentRanges_ShouldReturnFalse()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(1, 2, pricePerPerson).Value; // 1-2
        var tier2 = GroupPricingTier.Create(3, 5, pricePerPerson).Value; // 3-5 (adjacent, no overlap)

        // Act
        var result = tier1.OverlapsWith(tier2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WithUnlimitedTierAndDefinedTier_ShouldDetectOverlap()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(3, null, pricePerPerson).Value; // 3+
        var tier2 = GroupPricingTier.Create(5, 10, pricePerPerson).Value; // 5-10 (overlaps with 3+)

        // Act
        var result = tier1.OverlapsWith(tier2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WithTwoUnlimitedTiers_ShouldReturnTrue()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(3, null, pricePerPerson).Value; // 3+
        var tier2 = GroupPricingTier.Create(5, null, pricePerPerson).Value; // 5+ (overlaps)

        // Act
        var result = tier1.OverlapsWith(tier2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ToString and Display Tests

    [Fact]
    public void ToString_WithDefinedMaxAttendees_ShouldFormatCorrectly()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier = GroupPricingTier.Create(1, 2, pricePerPerson).Value;

        // Act
        var result = tier.ToString();

        // Assert
        result.Should().Contain("1-2");
        result.Should().Contain("$15.00");
        result.Should().Contain("person");
    }

    [Fact]
    public void ToString_WithUnlimitedTier_ShouldFormatCorrectly()
    {
        // Arrange
        var pricePerPerson = Money.Create(10.00m, Currency.USD).Value;
        var tier = GroupPricingTier.Create(3, null, pricePerPerson).Value;

        // Act
        var result = tier.ToString();

        // Assert
        result.Should().Contain("3+");
        result.Should().Contain("$10.00");
        result.Should().Contain("person");
    }

    #endregion

    #region Value Object Equality Tests

    [Fact]
    public void ValueObjectEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(1, 2, pricePerPerson).Value;
        var tier2 = GroupPricingTier.Create(1, 2, pricePerPerson).Value;

        // Assert
        tier1.Should().Be(tier2);
        (tier1 == tier2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentMinAttendees_ShouldNotBeEqual()
    {
        // Arrange
        var pricePerPerson = Money.Create(15.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(1, 2, pricePerPerson).Value;
        var tier2 = GroupPricingTier.Create(2, 2, pricePerPerson).Value;

        // Assert
        tier1.Should().NotBe(tier2);
        (tier1 == tier2).Should().BeFalse();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentPrices_ShouldNotBeEqual()
    {
        // Arrange
        var price1 = Money.Create(15.00m, Currency.USD).Value;
        var price2 = Money.Create(20.00m, Currency.USD).Value;
        var tier1 = GroupPricingTier.Create(1, 2, price1).Value;
        var tier2 = GroupPricingTier.Create(1, 2, price2).Value;

        // Assert
        tier1.Should().NotBe(tier2);
        (tier1 == tier2).Should().BeFalse();
    }

    #endregion
}
