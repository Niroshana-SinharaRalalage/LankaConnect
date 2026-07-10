using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Options;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Options;

/// <summary>
/// Phase 6A.95: Unit tests for SalesTaxSettings configuration class
/// </summary>
public class SalesTaxSettingsTests
{
    [Fact]
    public void Validate_WithDefaultValues_ShouldNotThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEnabledFalse_ShouldNotThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = false,
            DefaultRateWhenDisabled = 0m
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNonZeroDefaultRate_ShouldThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = false,
            DefaultRateWhenDisabled = 0.05m
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultRateWhenDisabled must be 0*");
    }

    [Fact]
    public void Validate_WithEmptyStateInEnabledStates_ShouldThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "", "NY" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot contain empty*");
    }

    [Fact]
    public void Validate_WithWhitespaceStateInEnabledStates_ShouldThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "   ", "NY" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot contain empty*");
    }

    [Fact]
    public void Validate_WithInvalidStateCodes_ShouldThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "California", "NY" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must contain valid 2-letter state codes*California*");
    }

    [Fact]
    public void Validate_WithValidStateCodes_ShouldNotThrow()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "NY", "TX" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #region IsTaxEnabledForState Tests

    [Fact]
    public void IsTaxEnabledForState_WhenFeatureDisabled_ShouldReturnFalse()
    {
        // Arrange
        var settings = new SalesTaxSettings { Enabled = false };

        // Act
        var result = settings.IsTaxEnabledForState("CA");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTaxEnabledForState_WhenFeatureEnabled_NoEnabledStates_ShouldReturnTrue()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = null
        };

        // Act
        var result = settings.IsTaxEnabledForState("CA");

        // Assert
        result.Should().BeTrue("All states should have tax when feature is enabled and no specific states are listed");
    }

    [Fact]
    public void IsTaxEnabledForState_WhenFeatureEnabled_EmptyEnabledStates_ShouldReturnTrue()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string>()
        };

        // Act
        var result = settings.IsTaxEnabledForState("CA");

        // Assert
        result.Should().BeTrue("All states should have tax when feature is enabled and EnabledStates is empty");
    }

    [Fact]
    public void IsTaxEnabledForState_WhenStateInEnabledList_ShouldReturnTrue()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "NY", "TX" }
        };

        // Act
        var result = settings.IsTaxEnabledForState("CA");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTaxEnabledForState_WhenStateNotInEnabledList_ShouldReturnFalse()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "NY", "TX" }
        };

        // Act
        var result = settings.IsTaxEnabledForState("FL");

        // Assert
        result.Should().BeFalse("FL is not in the EnabledStates list");
    }

    [Fact]
    public void IsTaxEnabledForState_ShouldBeCaseInsensitive()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "NY", "TX" }
        };

        // Act & Assert
        settings.IsTaxEnabledForState("ca").Should().BeTrue();
        settings.IsTaxEnabledForState("Ca").Should().BeTrue();
        settings.IsTaxEnabledForState("cA").Should().BeTrue();
        settings.IsTaxEnabledForState("CA").Should().BeTrue();
    }

    [Fact]
    public void IsTaxEnabledForState_WithNullStateCode_ShouldReturnFalse()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA" }
        };

        // Act
        var result = settings.IsTaxEnabledForState(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTaxEnabledForState_WithEmptyStateCode_ShouldReturnFalse()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA" }
        };

        // Act
        var result = settings.IsTaxEnabledForState("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTaxEnabledForState_WithWhitespaceStateCode_ShouldReturnFalse()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA" }
        };

        // Act
        var result = settings.IsTaxEnabledForState("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTaxEnabledForState_ShouldTrimWhitespaceFromStateCode()
    {
        // Arrange
        var settings = new SalesTaxSettings
        {
            Enabled = true,
            EnabledStates = new List<string> { "CA", "NY" }
        };

        // Act
        var result = settings.IsTaxEnabledForState("  CA  ");

        // Assert
        result.Should().BeTrue("Whitespace should be trimmed from state code");
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void DefaultValues_ShouldBeDisabled()
    {
        // Arrange & Act
        var settings = new SalesTaxSettings();

        // Assert
        settings.Enabled.Should().BeFalse("Sales tax should be disabled by default for safety");
        settings.DefaultRateWhenDisabled.Should().Be(0m);
        settings.EnabledStates.Should().BeNull();
    }

    [Fact]
    public void SectionName_ShouldBeSalesTax()
    {
        // Assert
        SalesTaxSettings.SectionName.Should().Be("SalesTax");
    }

    #endregion
}
