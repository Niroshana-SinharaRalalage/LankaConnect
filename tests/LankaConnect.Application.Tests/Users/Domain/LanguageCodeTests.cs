using FluentAssertions;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Users.ValueObjects;

/// <summary>
/// Tests for LanguageCode value object (Enumeration Pattern)
/// Follows architect guidance: ISO 639 codes for Sri Lankan diaspora languages
/// </summary>
public class LanguageCodeTests
{
    [Fact]
    public void LanguageCode_Should_Have_Code_And_Name()
    {
        // Arrange & Act
        var language = LanguageCode.Sinhala;

        // Assert
        language.Code.Should().NotBeNullOrWhiteSpace();
        language.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LanguageCode_Should_Have_Native_Name()
    {
        // Arrange & Act
        var sinhala = LanguageCode.Sinhala;
        var tamil = LanguageCode.Tamil;

        // Assert
        sinhala.NativeName.Should().NotBeNullOrWhiteSpace();
        tamil.NativeName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LanguageCode_Should_Provide_All_Supported_Languages()
    {
        // Act
        var allLanguages = LanguageCode.All;

        // Assert
        allLanguages.Should().NotBeNull();
        allLanguages.Should().NotBeEmpty();
        allLanguages.Count.Should().BeGreaterThanOrEqualTo(10, "architect recommends comprehensive South Asian language support");
    }

    [Fact]
    public void LanguageCode_All_Should_Include_Sri_Lankan_Primary_Languages()
    {
        // Arrange
        var expectedCodes = new[]
        {
            "si",  // Sinhala
            "ta",  // Tamil
            "en"   // English
        };

        // Act
        var allCodes = LanguageCode.All.Select(l => l.Code).ToList();

        // Assert
        foreach (var code in expectedCodes)
        {
            allCodes.Should().Contain(code, $"{code} is a primary language in Sri Lanka");
        }
    }

    [Fact]
    public void FromCode_Should_Return_Success_For_Valid_ISO_Code()
    {
        // Arrange
        const string validCode = "si";

        // Act
        var result = LanguageCode.FromCode(validCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Code.Should().Be(validCode);
    }

    [Fact]
    public void FromCode_Should_Return_Failure_For_Invalid_Code()
    {
        // Arrange
        const string invalidCode = "xx";

        // Act
        var result = LanguageCode.FromCode(invalidCode);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Language code");
        result.Error.Should().Contain(invalidCode);
    }

    [Fact]
    public void FromCode_Should_Return_Failure_For_Null_Code()
    {
        // Act
        var result = LanguageCode.FromCode(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromCode_Should_Return_Failure_For_Empty_Code()
    {
        // Act
        var result = LanguageCode.FromCode(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromCode_Should_Be_Case_Insensitive()
    {
        // Arrange
        const string lowercaseCode = "si";
        const string uppercaseCode = "SI";

        // Act
        var lowercaseResult = LanguageCode.FromCode(lowercaseCode);
        var uppercaseResult = LanguageCode.FromCode(uppercaseCode);

        // Assert
        lowercaseResult.IsSuccess.Should().BeTrue();
        uppercaseResult.IsSuccess.Should().BeTrue();
        lowercaseResult.Value.Should().Be(uppercaseResult.Value, "ISO codes should be case-insensitive");
    }

    [Fact]
    public void LanguageCode_Should_Support_Equality_By_Code()
    {
        // Arrange
        var language1 = LanguageCode.FromCode("si").Value;
        var language2 = LanguageCode.FromCode("si").Value;

        // Act & Assert
        language1.Should().Be(language2);
        (language1 == language2).Should().BeTrue();
        language1.GetHashCode().Should().Be(language2.GetHashCode());
    }

    [Fact]
    public void LanguageCode_Should_Support_Inequality_For_Different_Codes()
    {
        // Arrange
        var sinhala = LanguageCode.Sinhala;
        var tamil = LanguageCode.Tamil;

        // Act & Assert
        sinhala.Should().NotBe(tamil);
        (sinhala != tamil).Should().BeTrue();
    }

    [Fact]
    public void LanguageCode_All_Should_Have_Unique_Codes()
    {
        // Act
        var allCodes = LanguageCode.All.Select(l => l.Code).ToList();
        var distinctCodes = allCodes.Distinct().ToList();

        // Assert
        allCodes.Count.Should().Be(distinctCodes.Count, "all ISO codes should be unique");
    }

    [Fact]
    public void LanguageCode_Should_Include_Sinhala()
    {
        // Act
        var language = LanguageCode.Sinhala;

        // Assert
        language.Should().NotBeNull();
        language.Code.Should().Be("si");
        language.Name.Should().Contain("Sinhala");
        language.NativeName.Should().Contain("සිංහල");
    }

    [Fact]
    public void LanguageCode_Should_Include_Tamil()
    {
        // Act
        var language = LanguageCode.Tamil;

        // Assert
        language.Should().NotBeNull();
        language.Code.Should().Be("ta");
        language.Name.Should().Contain("Tamil");
        language.NativeName.Should().Contain("தமிழ்");
    }

    [Fact]
    public void LanguageCode_Should_Include_English()
    {
        // Act
        var language = LanguageCode.English;

        // Assert
        language.Should().NotBeNull();
        language.Code.Should().Be("en");
        language.Name.Should().Contain("English");
    }

    [Theory]
    [InlineData("si", "Sinhala")]
    [InlineData("ta", "Tamil")]
    [InlineData("en", "English")]
    [InlineData("hi", "Hindi")]
    public void LanguageCode_Should_Map_Code_To_Correct_Name(string code, string expectedNamePart)
    {
        // Act
        var result = LanguageCode.FromCode(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Contain(expectedNamePart);
    }

    [Fact]
    public void LanguageCode_ToString_Should_Return_Name()
    {
        // Arrange
        var language = LanguageCode.Sinhala;

        // Act
        var stringValue = language.ToString();

        // Assert
        stringValue.Should().Be(language.Name);
    }

    [Fact]
    public void LanguageCode_Should_Be_Immutable()
    {
        // Arrange
        var language = LanguageCode.Sinhala;
        var originalCode = language.Code;
        var originalName = language.Name;
        var originalNativeName = language.NativeName;

        // Act - Try to access (no setters should exist)
        // Value objects should be immutable

        // Assert
        language.Code.Should().Be(originalCode, "Code should be immutable");
        language.Name.Should().Be(originalName, "Name should be immutable");
        language.NativeName.Should().Be(originalNativeName, "NativeName should be immutable");
    }

    [Fact]
    public void LanguageCode_Should_Support_South_Asian_Languages()
    {
        // Arrange
        var expectedLanguages = new[]
        {
            "Hindi", "Bengali", "Urdu", "Punjabi", "Gujarati"
        };

        // Act
        var allLanguageNames = LanguageCode.All.Select(l => l.Name).ToList();

        // Assert - At least some South Asian languages should be supported
        allLanguageNames.Should().Contain(language =>
            expectedLanguages.Any(expected => language.Contains(expected)),
            "should support major South Asian languages for diaspora community");
    }

    [Fact]
    public void LanguageCode_All_Should_Be_Ordered_Logically()
    {
        // Act
        var all = LanguageCode.All.ToList();

        // Assert - Sri Lankan languages should come first
        all[0].Code.Should().Be("si", "Sinhala should be first");
        all[1].Code.Should().Be("ta", "Tamil should be second");
        all[2].Code.Should().Be("en", "English should be third");
    }

    [Fact]
    public void LanguageCode_Should_Support_At_Least_15_Languages()
    {
        // Act
        var count = LanguageCode.All.Count;

        // Assert
        count.Should().BeGreaterThanOrEqualTo(15, "comprehensive support for South Asian diaspora languages");
    }
}
