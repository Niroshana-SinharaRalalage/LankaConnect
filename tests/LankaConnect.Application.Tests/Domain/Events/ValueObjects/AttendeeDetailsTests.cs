using FluentAssertions;
using LankaConnect.Domain.Events.Enums;
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
    public void Create_WithValidNameAndAgeCategory_ShouldSucceed()
    {
        // Arrange
        var name = "John Doe";
        var ageCategory = AgeCategory.Adult;

        // Act
        var result = AttendeeDetails.Create(name, ageCategory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.AgeCategory.Should().Be(ageCategory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldFail(string invalidName)
    {
        // Act
        var result = AttendeeDetails.Create(invalidName, AgeCategory.Adult);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Create_WithInvalidAgeCategory_ShouldFail(int invalidCategory)
    {
        // Act
        var result = AttendeeDetails.Create("John Doe", (AgeCategory)invalidCategory);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid age category");
    }

    [Theory]
    [InlineData("John Doe", AgeCategory.Adult)]
    [InlineData("Jane Smith", AgeCategory.Child)]
    [InlineData("Bob Johnson", AgeCategory.Adult)]
    [InlineData("Alice Williams", AgeCategory.Child)]
    public void Create_WithValidAgeCategories_ShouldSucceed(string name, AgeCategory ageCategory)
    {
        // Act
        var result = AttendeeDetails.Create(name, ageCategory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.AgeCategory.Should().Be(ageCategory);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromName()
    {
        // Arrange
        var nameWithWhitespace = "  John Doe  ";
        var expectedName = "John Doe";

        // Act
        var result = AttendeeDetails.Create(nameWithWhitespace, AgeCategory.Adult);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("John Doe", AgeCategory.Adult)]
    [InlineData("Jane Smith", AgeCategory.Child)]
    public void ValueObjectEquality_WithSameValues_ShouldBeEqual(string name, AgeCategory ageCategory)
    {
        // Arrange
        var details1 = AttendeeDetails.Create(name, ageCategory).Value;
        var details2 = AttendeeDetails.Create(name, ageCategory).Value;

        // Act & Assert
        details1.Should().Be(details2);
        (details1 == details2).Should().BeTrue();
        (details1 != details2).Should().BeFalse();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentNames_ShouldNotBeEqual()
    {
        // Arrange
        var details1 = AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value;
        var details2 = AttendeeDetails.Create("Jane Smith", AgeCategory.Adult).Value;

        // Act & Assert
        details1.Should().NotBe(details2);
        (details1 == details2).Should().BeFalse();
        (details1 != details2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjectEquality_WithDifferentAgeCategories_ShouldNotBeEqual()
    {
        // Arrange
        var details1 = AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value;
        var details2 = AttendeeDetails.Create("John Doe", AgeCategory.Child).Value;

        // Act & Assert
        details1.Should().NotBe(details2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var details = AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value;

        // Act
        var result = details.ToString();

        // Assert
        result.Should().Contain("John Doe");
        result.Should().Contain("Adult");
    }
}
