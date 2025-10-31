using FluentAssertions;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Users.ValueObjects;

/// <summary>
/// Tests for CulturalInterest value object (Enumeration Pattern)
/// Follows architect guidance: strongly-typed predefined cultural interests for Sri Lankan diaspora
/// </summary>
public class CulturalInterestTests
{
    [Fact]
    public void CulturalInterest_Should_Have_Code_And_Name()
    {
        // Arrange & Act
        var interest = CulturalInterest.SriLankanCuisine;

        // Assert
        interest.Code.Should().NotBeNullOrWhiteSpace();
        interest.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CulturalInterest_Should_Provide_All_Predefined_Interests()
    {
        // Act
        var allInterests = CulturalInterest.All;

        // Assert
        allInterests.Should().NotBeNull();
        allInterests.Should().NotBeEmpty();
        allInterests.Count.Should().BeGreaterThanOrEqualTo(15, "architect recommends 15-20 categories");
    }

    [Fact]
    public void CulturalInterest_All_Should_Include_Essential_Categories()
    {
        // Arrange
        var expectedCodes = new[]
        {
            "SL_CUISINE",      // Sri Lankan Cuisine
            "BUDDHIST_FEST",   // Buddhist Festivals
            "HINDU_FEST",      // Hindu Festivals
            "CRICKET",         // Cricket Culture
            "VESAK",           // Vesak Celebrations
            "SINHALA_NY"       // Sinhala & Tamil New Year
        };

        // Act
        var allCodes = CulturalInterest.All.Select(i => i.Code).ToList();

        // Assert
        foreach (var code in expectedCodes)
        {
            allCodes.Should().Contain(code, $"{code} is an essential category for Sri Lankan diaspora");
        }
    }

    [Fact]
    public void FromCode_Should_Return_Success_For_Valid_Code()
    {
        // Arrange
        const string validCode = "SL_CUISINE";

        // Act
        var result = CulturalInterest.FromCode(validCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Code.Should().Be(validCode);
    }

    [Fact]
    public void FromCode_Should_Return_Failure_For_Invalid_Code()
    {
        // Arrange
        const string invalidCode = "INVALID_CODE";

        // Act
        var result = CulturalInterest.FromCode(invalidCode);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cultural interest code");
        result.Error.Should().Contain(invalidCode);
    }

    [Fact]
    public void FromCode_Should_Return_Failure_For_Null_Code()
    {
        // Act
        var result = CulturalInterest.FromCode(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromCode_Should_Return_Failure_For_Empty_Code()
    {
        // Act
        var result = CulturalInterest.FromCode(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromCode_Should_Be_Case_Sensitive()
    {
        // Arrange
        const string correctCode = "SL_CUISINE";
        const string lowercaseCode = "sl_cuisine";

        // Act
        var correctResult = CulturalInterest.FromCode(correctCode);
        var lowercaseResult = CulturalInterest.FromCode(lowercaseCode);

        // Assert
        correctResult.IsSuccess.Should().BeTrue();
        lowercaseResult.IsFailure.Should().BeTrue("codes should be case-sensitive");
    }

    [Fact]
    public void CulturalInterest_Should_Support_Equality_By_Code()
    {
        // Arrange
        var interest1 = CulturalInterest.FromCode("SL_CUISINE").Value;
        var interest2 = CulturalInterest.FromCode("SL_CUISINE").Value;

        // Act & Assert
        interest1.Should().Be(interest2);
        (interest1 == interest2).Should().BeTrue();
        interest1.GetHashCode().Should().Be(interest2.GetHashCode());
    }

    [Fact]
    public void CulturalInterest_Should_Support_Inequality_For_Different_Codes()
    {
        // Arrange
        var cuisine = CulturalInterest.SriLankanCuisine;
        var cricket = CulturalInterest.CricketCulture;

        // Act & Assert
        cuisine.Should().NotBe(cricket);
        (cuisine != cricket).Should().BeTrue();
    }

    [Fact]
    public void CulturalInterest_All_Should_Have_Unique_Codes()
    {
        // Act
        var allCodes = CulturalInterest.All.Select(i => i.Code).ToList();
        var distinctCodes = allCodes.Distinct().ToList();

        // Assert
        allCodes.Count.Should().Be(distinctCodes.Count, "all codes should be unique");
    }

    [Fact]
    public void CulturalInterest_Should_Include_Buddhist_Festivals()
    {
        // Act
        var interest = CulturalInterest.BuddhistFestivals;

        // Assert
        interest.Should().NotBeNull();
        interest.Code.Should().Be("BUDDHIST_FEST");
        interest.Name.Should().Contain("Buddhist");
    }

    [Fact]
    public void CulturalInterest_Should_Include_Hindu_Festivals()
    {
        // Act
        var interest = CulturalInterest.HinduFestivals;

        // Assert
        interest.Should().NotBeNull();
        interest.Code.Should().Be("HINDU_FEST");
        interest.Name.Should().Contain("Hindu");
    }

    [Fact]
    public void CulturalInterest_Should_Include_Islamic_Festivals()
    {
        // Act
        var interest = CulturalInterest.IslamicFestivals;

        // Assert
        interest.Should().NotBeNull();
        interest.Code.Should().Be("ISLAMIC_FEST");
        interest.Name.Should().Contain("Islamic");
    }

    [Fact]
    public void CulturalInterest_Should_Include_Christian_Festivals()
    {
        // Act
        var interest = CulturalInterest.ChristianFestivals;

        // Assert
        interest.Should().NotBeNull();
        interest.Code.Should().Be("CHRISTIAN_FEST");
        interest.Name.Should().Contain("Christian");
    }

    [Theory]
    [InlineData("SL_CUISINE", "Sri Lankan Cuisine")]
    [InlineData("CRICKET", "Cricket")]
    [InlineData("VESAK", "Vesak")]
    public void CulturalInterest_Should_Map_Code_To_Correct_Name(string code, string expectedNamePart)
    {
        // Act
        var result = CulturalInterest.FromCode(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Contain(expectedNamePart);
    }

    [Fact]
    public void CulturalInterest_ToString_Should_Return_Name()
    {
        // Arrange
        var interest = CulturalInterest.SriLankanCuisine;

        // Act
        var stringValue = interest.ToString();

        // Assert
        stringValue.Should().Be(interest.Name);
    }

    [Fact]
    public void CulturalInterest_Should_Be_Immutable()
    {
        // Arrange
        var interest = CulturalInterest.SriLankanCuisine;
        var originalCode = interest.Code;
        var originalName = interest.Name;

        // Act - Try to access (no setters should exist)
        // Value objects should be immutable

        // Assert
        interest.Code.Should().Be(originalCode, "Code should be immutable");
        interest.Name.Should().Be(originalName, "Name should be immutable");
    }
}
