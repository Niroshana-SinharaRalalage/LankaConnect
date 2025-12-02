using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

/// <summary>
/// Unit tests for AttendeeDetails value object
/// Following TDD Red-Green-Refactor cycle
/// </summary>
public class AttendeeDetailsTests
{
    [Fact]
    public void Create_WithValidNameAndAge_ShouldSucceed()
    {
        // Arrange
        var name = "John Doe";
        var age = 30;

        // Act
        var result = AttendeeDetails.Create(name, age);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Age.Should().Be(age);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldFail(string invalidName)
    {
        // Act
        var result = AttendeeDetails.Create(invalidName, 30);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithZeroOrNegativeAge_ShouldFail(int invalidAge)
    {
        // Act
        var result = AttendeeDetails.Create("John Doe", invalidAge);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Age must be greater than 0");
    }

    [Theory]
    [InlineData(121)]
    [InlineData(150)]
    [InlineData(200)]
    public void Create_WithAgeTooHigh_ShouldFail(int invalidAge)
    {
        // Act
        var result = AttendeeDetails.Create("John Doe", invalidAge);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Age must not exceed 120");
    }

    [Theory]
    [InlineData("John Doe", 1)]
    [InlineData("Jane Smith", 5)]
    [InlineData("Bob Johnson", 12)]
    [InlineData("Alice Williams", 18)]
    [InlineData("Charlie Brown", 25)]
    [InlineData("Eve Davis", 65)]
    [InlineData("Frank Miller", 120)]
    public void Create_WithValidAgeRange_ShouldSucceed(string name, int age)
    {
        // Act
        var result = AttendeeDetails.Create(name, age);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Age.Should().Be(age);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromName()
    {
        // Arrange
        var nameWithWhitespace = "  John Doe  ";
        var expectedName = "John Doe";

        // Act
        var result = AttendeeDetails.Create(nameWithWhitespace, 30);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("John Doe", 30)]
    [InlineData("Jane Smith", 25)]
    public void ValueObjectEquality_WithSameValues_ShouldBeEqual(string name, int age)
    {
        // Arrange
        var details1 = AttendeeDetails.Create(name, age).Value;
        var details2 = AttendeeDetails.Create(name, age).Value;

        // Act & Assert
        details1.Should().Be(details2);
        (details1 == details2).Should().BeTrue();
        (details1 != details2).Should().BeFalse();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentNames_ShouldNotBeEqual()
    {
        // Arrange
        var details1 = AttendeeDetails.Create("John Doe", 30).Value;
        var details2 = AttendeeDetails.Create("Jane Smith", 30).Value;

        // Act & Assert
        details1.Should().NotBe(details2);
        (details1 == details2).Should().BeFalse();
        (details1 != details2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentAges_ShouldNotBeEqual()
    {
        // Arrange
        var details1 = AttendeeDetails.Create("John Doe", 30).Value;
        var details2 = AttendeeDetails.Create("John Doe", 25).Value;

        // Act & Assert
        details1.Should().NotBe(details2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var details = AttendeeDetails.Create("John Doe", 30).Value;

        // Act
        var result = details.ToString();

        // Assert
        result.Should().Be("John Doe (30)");
    }
}
