using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class EventDonationTests
{
    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("A test event for donation testing").Value;
        return Event.Create(
            title,
            description,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(),
            100).Value;
    }

    #region SetDonationConfiguration Tests

    [Fact]
    public void SetDonationConfiguration_WithValidConfig_Should_Succeed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 10m, 25m, 50m },
            allowCustomAmount: true,
            minAmount: 5m,
            maxAmount: 500m,
            donationMessage: "Help us!").Value;

        // Act
        var result = @event.SetDonationConfiguration(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.DonationConfig.Should().NotBeNull();
        @event.DonationConfig!.IsEnabled.Should().BeTrue();
        @event.DonationConfig.SuggestedAmounts.Should().HaveCount(3);
    }

    [Fact]
    public void SetDonationConfiguration_WithNull_Should_Fail()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.SetDonationConfiguration(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    #endregion

    #region DisableDonations Tests

    [Fact]
    public void DisableDonations_Should_SetConfigToDisabled()
    {
        // Arrange
        var @event = CreateTestEvent();
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null).Value;
        @event.SetDonationConfiguration(config);

        // Act
        var result = @event.DisableDonations();

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.DonationConfig.Should().NotBeNull();
        @event.DonationConfig!.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region AreDonationsEnabled Tests

    [Fact]
    public void AreDonationsEnabled_WhenConfigNull_Should_ReturnFalse()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act & Assert
        @event.AreDonationsEnabled().Should().BeFalse();
    }

    [Fact]
    public void AreDonationsEnabled_WhenConfigDisabled_Should_ReturnFalse()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.SetDonationConfiguration(DonationConfiguration.Disabled());

        // Act & Assert
        @event.AreDonationsEnabled().Should().BeFalse();
    }

    [Fact]
    public void AreDonationsEnabled_WhenConfigEnabled_Should_ReturnTrue()
    {
        // Arrange
        var @event = CreateTestEvent();
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: null,
            maxAmount: null,
            donationMessage: null).Value;
        @event.SetDonationConfiguration(config);

        // Act & Assert
        @event.AreDonationsEnabled().Should().BeTrue();
    }

    #endregion

    #region ValidateDonationAmount Tests

    [Fact]
    public void ValidateDonationAmount_WhenDonationsDisabled_Should_Fail()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var result = @event.ValidateDonationAmount(25m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not enabled");
    }

    [Fact]
    public void ValidateDonationAmount_WithValidAmount_Should_Succeed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 10m,
            maxAmount: 100m,
            donationMessage: null).Value;
        @event.SetDonationConfiguration(config);

        // Act
        var result = @event.ValidateDonationAmount(50m);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateDonationAmount_BelowMinimum_Should_Fail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 20m,
            maxAmount: 100m,
            donationMessage: null).Value;
        @event.SetDonationConfiguration(config);

        // Act
        var result = @event.ValidateDonationAmount(5m);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ValidateDonationAmount_AboveMaximum_Should_Fail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var config = DonationConfiguration.Create(
            isEnabled: true,
            suggestedAmounts: new List<decimal> { 25m },
            allowCustomAmount: true,
            minAmount: 10m,
            maxAmount: 100m,
            donationMessage: null).Value;
        @event.SetDonationConfiguration(config);

        // Act
        var result = @event.ValidateDonationAmount(200m);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
