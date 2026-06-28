using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Comprehensive unit tests for AddOnConfiguration value object.
/// Covers creation, context availability validation, message rules, and disabled state.
/// </summary>
public class AddOnConfigurationTests
{
    #region Create() - Enabled with Valid Data

    [Fact]
    public void Create_EnabledWithBothContexts_ShouldSucceed()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: true,
            addOnMessage: "Check out our add-ons!");

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeTrue();
        config.AvailableDuringRegistration.Should().BeTrue();
        config.AvailableStandalone.Should().BeTrue();
        config.AddOnMessage.Should().Be("Check out our add-ons!");
    }

    [Fact]
    public void Create_EnabledWithRegistrationOnly_ShouldSucceed()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: null);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.AvailableDuringRegistration.Should().BeTrue();
        config.AvailableStandalone.Should().BeFalse();
    }

    [Fact]
    public void Create_EnabledWithStandaloneOnly_ShouldSucceed()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: false,
            availableStandalone: true,
            addOnMessage: null);

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.AvailableDuringRegistration.Should().BeFalse();
        config.AvailableStandalone.Should().BeTrue();
    }

    #endregion

    #region Create() - Neither Context

    [Fact]
    public void Create_NeitherContext_ShouldFail()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: false,
            availableStandalone: false,
            addOnMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one context");
    }

    #endregion

    #region Create() - Disabled

    [Fact]
    public void Create_Disabled_ShouldReturnDisabledConfig()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: false,
            availableDuringRegistration: true,
            availableStandalone: true,
            addOnMessage: "ignored");

        result.IsSuccess.Should().BeTrue();
        var config = result.Value;
        config.IsEnabled.Should().BeFalse();
        config.AvailableDuringRegistration.Should().BeFalse();
        config.AvailableStandalone.Should().BeFalse();
        config.AddOnMessage.Should().BeNull();
    }

    [Fact]
    public void Create_DisabledWithNeitherContext_ShouldStillSucceed()
    {
        // When disabled, neither context validation should not apply
        var result = AddOnConfiguration.Create(
            isEnabled: false,
            availableDuringRegistration: false,
            availableStandalone: false,
            addOnMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Disabled_ShouldReturnConfigWithAllDefaults()
    {
        var config = AddOnConfiguration.Disabled();

        config.IsEnabled.Should().BeFalse();
        config.AvailableDuringRegistration.Should().BeFalse();
        config.AvailableStandalone.Should().BeFalse();
        config.AddOnMessage.Should().BeNull();
    }

    #endregion

    #region Message Validation

    [Fact]
    public void Create_WithMessageExceeding500Chars_ShouldFail()
    {
        var longMessage = new string('x', 501);

        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: true,
            addOnMessage: longMessage);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
    }

    [Fact]
    public void Create_WithMessageExactly500Chars_ShouldSucceed()
    {
        var message = new string('x', 500);

        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: message);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimMessage()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: "  Browse add-ons!  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.AddOnMessage.Should().Be("Browse add-ons!");
    }

    [Fact]
    public void Create_WithNullMessage_ShouldSucceed()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.AddOnMessage.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyMessage_ShouldSucceed()
    {
        var result = AddOnConfiguration.Create(
            isEnabled: true,
            availableDuringRegistration: true,
            availableStandalone: false,
            addOnMessage: "");

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Value Object Equality

    [Fact]
    public void TwoConfigsWithSameValues_ShouldBeEqual()
    {
        var config1 = AddOnConfiguration.Create(
            isEnabled: true, availableDuringRegistration: true,
            availableStandalone: false, addOnMessage: "Test").Value;

        var config2 = AddOnConfiguration.Create(
            isEnabled: true, availableDuringRegistration: true,
            availableStandalone: false, addOnMessage: "Test").Value;

        config1.Should().Be(config2);
    }

    [Fact]
    public void TwoConfigsWithDifferentValues_ShouldNotBeEqual()
    {
        var config1 = AddOnConfiguration.Create(
            isEnabled: true, availableDuringRegistration: true,
            availableStandalone: false, addOnMessage: "Test").Value;

        var config2 = AddOnConfiguration.Create(
            isEnabled: true, availableDuringRegistration: false,
            availableStandalone: true, addOnMessage: "Test").Value;

        config1.Should().NotBe(config2);
    }

    [Fact]
    public void DisabledConfigs_ShouldBeEqual()
    {
        var config1 = AddOnConfiguration.Disabled();
        var config2 = AddOnConfiguration.Disabled();

        config1.Should().Be(config2);
    }

    #endregion
}
