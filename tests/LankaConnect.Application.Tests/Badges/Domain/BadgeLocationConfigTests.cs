using FluentAssertions;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common.Exceptions;
using Xunit;

namespace LankaConnect.Application.Tests.Badges.Domain;

public class BadgeLocationConfigTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesInstance()
    {
        // Arrange & Act
        var config = new BadgeLocationConfig(
            positionX: 0.5m,
            positionY: 0.5m,
            sizeWidth: 0.26m,
            sizeHeight: 0.26m,
            rotation: 45m
        );

        // Assert
        config.PositionX.Should().Be(0.5m);
        config.PositionY.Should().Be(0.5m);
        config.SizeWidth.Should().Be(0.26m);
        config.SizeHeight.Should().Be(0.26m);
        config.Rotation.Should().Be(45m);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void Constructor_InvalidPositionX_ThrowsDomainException(decimal invalidPositionX)
    {
        // Arrange & Act
        Action act = () => new BadgeLocationConfig(
            positionX: invalidPositionX,
            positionY: 0.5m,
            sizeWidth: 0.26m,
            sizeHeight: 0.26m,
            rotation: 0m
        );

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void Constructor_InvalidPositionY_ThrowsValidationException(decimal invalidPositionY)
    {
        // Arrange & Act
        Action act = () => new BadgeLocationConfig(
            positionX: 0.5m,
            positionY: invalidPositionY,
            sizeWidth: 0.26m,
            sizeHeight: 0.26m,
            rotation: 0m
        );

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(0.04)]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_InvalidSizeWidth_ThrowsValidationException(decimal invalidWidth)
    {
        // Arrange & Act
        Action act = () => new BadgeLocationConfig(
            positionX: 0.5m,
            positionY: 0.5m,
            sizeWidth: invalidWidth,
            sizeHeight: 0.26m,
            rotation: 0m
        );

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(0.04)]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_InvalidSizeHeight_ThrowsValidationException(decimal invalidHeight)
    {
        // Arrange & Act
        Action act = () => new BadgeLocationConfig(
            positionX: 0.5m,
            positionY: 0.5m,
            sizeWidth: 0.26m,
            sizeHeight: invalidHeight,
            rotation: 0m
        );

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(361)]
    [InlineData(-10)]
    [InlineData(400)]
    public void Constructor_InvalidRotation_ThrowsValidationException(decimal invalidRotation)
    {
        // Arrange & Act
        Action act = () => new BadgeLocationConfig(
            positionX: 0.5m,
            positionY: 0.5m,
            sizeWidth: 0.26m,
            sizeHeight: 0.26m,
            rotation: invalidRotation
        );

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Constructor_BoundaryValues_AcceptsValidBoundaries()
    {
        // Arrange & Act
        var configMin = new BadgeLocationConfig(0m, 0m, 0.05m, 0.05m, 0m);
        var configMax = new BadgeLocationConfig(1m, 1m, 1m, 1m, 360m);

        // Assert
        configMin.PositionX.Should().Be(0m);
        configMin.PositionY.Should().Be(0m);
        configMin.SizeWidth.Should().Be(0.05m);
        configMin.SizeHeight.Should().Be(0.05m);
        configMin.Rotation.Should().Be(0m);

        configMax.PositionX.Should().Be(1m);
        configMax.PositionY.Should().Be(1m);
        configMax.SizeWidth.Should().Be(1m);
        configMax.SizeHeight.Should().Be(1m);
        configMax.Rotation.Should().Be(360m);
    }

    [Fact]
    public void DefaultListing_ReturnsCorrectDefaults()
    {
        // Arrange & Act
        var config = BadgeLocationConfig.DefaultListing;

        // Assert
        config.PositionX.Should().Be(1.0m); // Right edge
        config.PositionY.Should().Be(0.0m); // Top edge
        config.SizeWidth.Should().Be(0.26m); // 26%
        config.SizeHeight.Should().Be(0.26m); // 26%
        config.Rotation.Should().Be(0m);
    }

    [Fact]
    public void DefaultFeatured_ReturnsCorrectDefaults()
    {
        // Arrange & Act
        var config = BadgeLocationConfig.DefaultFeatured;

        // Assert
        config.PositionX.Should().Be(1.0m);
        config.PositionY.Should().Be(0.0m);
        config.SizeWidth.Should().Be(0.26m);
        config.SizeHeight.Should().Be(0.26m);
        config.Rotation.Should().Be(0m);
    }

    [Fact]
    public void DefaultDetail_ReturnsCorrectDefaults()
    {
        // Arrange & Act
        var config = BadgeLocationConfig.DefaultDetail;

        // Assert
        config.PositionX.Should().Be(1.0m);
        config.PositionY.Should().Be(0.0m);
        config.SizeWidth.Should().Be(0.21m); // 21% (smaller for large containers)
        config.SizeHeight.Should().Be(0.21m);
        config.Rotation.Should().Be(0m);
    }

    [Fact]
    public void ValueObjectEquality_SameValues_AreEqual()
    {
        // Arrange
        var config1 = new BadgeLocationConfig(0.5m, 0.5m, 0.26m, 0.26m, 45m);
        var config2 = new BadgeLocationConfig(0.5m, 0.5m, 0.26m, 0.26m, 45m);

        // Assert
        config1.Should().Be(config2);
        (config1 == config2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var config1 = new BadgeLocationConfig(0.5m, 0.5m, 0.26m, 0.26m, 45m);
        var config2 = new BadgeLocationConfig(0.6m, 0.5m, 0.26m, 0.26m, 45m);

        // Assert
        config1.Should().NotBe(config2);
        (config1 == config2).Should().BeFalse();
    }
}
