using FluentAssertions;
using LankaConnect.Domain.Users.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Users.Enums;

/// <summary>
/// Tests for ProficiencyLevel enum (4-level scale)
/// Follows architect guidance: Basic (1), Intermediate (2), Advanced (3), Native (4)
/// </summary>
public class ProficiencyLevelTests
{
    [Fact]
    public void ProficiencyLevel_Should_Have_Four_Levels()
    {
        // Act
        var values = Enum.GetValues<ProficiencyLevel>();

        // Assert
        values.Should().HaveCount(4, "architect recommends 4-level scale");
    }

    [Fact]
    public void ProficiencyLevel_Should_Include_Basic()
    {
        // Act
        var level = ProficiencyLevel.Basic;

        // Assert
        level.Should().BeDefined();
        ((int)level).Should().Be(1, "Basic should be level 1");
    }

    [Fact]
    public void ProficiencyLevel_Should_Include_Intermediate()
    {
        // Act
        var level = ProficiencyLevel.Intermediate;

        // Assert
        level.Should().BeDefined();
        ((int)level).Should().Be(2, "Intermediate should be level 2");
    }

    [Fact]
    public void ProficiencyLevel_Should_Include_Advanced()
    {
        // Act
        var level = ProficiencyLevel.Advanced;

        // Assert
        level.Should().BeDefined();
        ((int)level).Should().Be(3, "Advanced should be level 3");
    }

    [Fact]
    public void ProficiencyLevel_Should_Include_Native()
    {
        // Act
        var level = ProficiencyLevel.Native;

        // Assert
        level.Should().BeDefined();
        ((int)level).Should().Be(4, "Native should be level 4");
    }

    [Fact]
    public void ProficiencyLevel_Values_Should_Be_Sequential()
    {
        // Arrange
        var expectedSequence = new[] { 1, 2, 3, 4 };

        // Act
        var actualValues = Enum.GetValues<ProficiencyLevel>()
            .Cast<int>()
            .OrderBy(v => v)
            .ToArray();

        // Assert
        actualValues.Should().Equal(expectedSequence, "levels should be sequential from 1-4");
    }

    [Fact]
    public void ProficiencyLevel_Should_Be_Ordered_By_Proficiency()
    {
        // Arrange & Act
        var basic = ProficiencyLevel.Basic;
        var intermediate = ProficiencyLevel.Intermediate;
        var advanced = ProficiencyLevel.Advanced;
        var native = ProficiencyLevel.Native;

        // Assert
        ((int)basic).Should().BeLessThan((int)intermediate);
        ((int)intermediate).Should().BeLessThan((int)advanced);
        ((int)advanced).Should().BeLessThan((int)native);
    }

    [Theory]
    [InlineData(ProficiencyLevel.Basic, 1)]
    [InlineData(ProficiencyLevel.Intermediate, 2)]
    [InlineData(ProficiencyLevel.Advanced, 3)]
    [InlineData(ProficiencyLevel.Native, 4)]
    public void ProficiencyLevel_Should_Have_Correct_Integer_Value(ProficiencyLevel level, int expectedValue)
    {
        // Act
        var actualValue = (int)level;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void ProficiencyLevel_ToString_Should_Return_Name()
    {
        // Arrange
        var level = ProficiencyLevel.Intermediate;

        // Act
        var stringValue = level.ToString();

        // Assert
        stringValue.Should().Be("Intermediate");
    }

    [Theory]
    [InlineData(1, ProficiencyLevel.Basic)]
    [InlineData(2, ProficiencyLevel.Intermediate)]
    [InlineData(3, ProficiencyLevel.Advanced)]
    [InlineData(4, ProficiencyLevel.Native)]
    public void ProficiencyLevel_Should_Cast_From_Integer(int value, ProficiencyLevel expected)
    {
        // Act
        var level = (ProficiencyLevel)value;

        // Assert
        level.Should().Be(expected);
    }

    [Fact]
    public void ProficiencyLevel_Should_Support_IsDefined()
    {
        // Arrange
        var validLevel = 2;
        var invalidLevel = 5;

        // Act
        var isValidDefined = Enum.IsDefined(typeof(ProficiencyLevel), validLevel);
        var isInvalidDefined = Enum.IsDefined(typeof(ProficiencyLevel), invalidLevel);

        // Assert
        isValidDefined.Should().BeTrue("2 corresponds to Intermediate");
        isInvalidDefined.Should().BeFalse("5 is not a valid proficiency level");
    }

    [Fact]
    public void ProficiencyLevel_Equality_Should_Work()
    {
        // Arrange
        var level1 = ProficiencyLevel.Advanced;
        var level2 = ProficiencyLevel.Advanced;
        var level3 = ProficiencyLevel.Intermediate;

        // Assert
        level1.Should().Be(level2);
        level1.Should().NotBe(level3);
        (level1 == level2).Should().BeTrue();
        (level1 == level3).Should().BeFalse();
    }

    [Fact]
    public void ProficiencyLevel_Basic_Should_Be_Lowest()
    {
        // Arrange
        var allLevels = Enum.GetValues<ProficiencyLevel>();

        // Act
        var lowestLevel = allLevels.Cast<int>().Min();

        // Assert
        lowestLevel.Should().Be((int)ProficiencyLevel.Basic, "Basic should be the lowest proficiency level");
    }

    [Fact]
    public void ProficiencyLevel_Native_Should_Be_Highest()
    {
        // Arrange
        var allLevels = Enum.GetValues<ProficiencyLevel>();

        // Act
        var highestLevel = allLevels.Cast<int>().Max();

        // Assert
        highestLevel.Should().Be((int)ProficiencyLevel.Native, "Native should be the highest proficiency level");
    }

    [Fact]
    public void ProficiencyLevel_Should_Not_Have_Zero_Value()
    {
        // Act
        var values = Enum.GetValues<ProficiencyLevel>().Cast<int>();

        // Assert
        values.Should().NotContain(0, "proficiency scale starts at 1 (Basic), not 0");
    }
}
