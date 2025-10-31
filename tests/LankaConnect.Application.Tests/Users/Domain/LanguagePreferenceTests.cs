using FluentAssertions;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Users.ValueObjects;

/// <summary>
/// Tests for LanguagePreference value object (composite of LanguageCode + ProficiencyLevel)
/// Follows architect guidance: combines language with proficiency for user language preferences
/// </summary>
public class LanguagePreferenceTests
{
    [Fact]
    public void LanguagePreference_Should_Have_LanguageCode_And_ProficiencyLevel()
    {
        // Arrange & Act
        var preference = LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value;

        // Assert
        preference.Language.Should().NotBeNull();
        preference.Proficiency.Should().Be(ProficiencyLevel.Native);
    }

    [Fact]
    public void Create_Should_Return_Success_For_Valid_Inputs()
    {
        // Arrange
        var language = LanguageCode.Tamil;
        var proficiency = ProficiencyLevel.Advanced;

        // Act
        var result = LanguagePreference.Create(language, proficiency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be(language);
        result.Value.Proficiency.Should().Be(proficiency);
    }

    [Fact]
    public void Create_Should_Return_Failure_For_Null_LanguageCode()
    {
        // Act
        var result = LanguagePreference.Create(null!, ProficiencyLevel.Intermediate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Language");
    }

    [Fact]
    public void LanguagePreference_Should_Support_Equality_By_Language_And_Proficiency()
    {
        // Arrange
        var pref1 = LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Native).Value;
        var pref2 = LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Native).Value;
        var pref3 = LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value;

        // Assert
        pref1.Should().Be(pref2, "same language and proficiency should be equal");
        pref1.Should().NotBe(pref3, "different proficiency should not be equal");
    }

    [Fact]
    public void LanguagePreference_Should_Support_All_ProficiencyLevels()
    {
        // Arrange
        var language = LanguageCode.Hindi;

        // Act
        var basic = LanguagePreference.Create(language, ProficiencyLevel.Basic).Value;
        var intermediate = LanguagePreference.Create(language, ProficiencyLevel.Intermediate).Value;
        var advanced = LanguagePreference.Create(language, ProficiencyLevel.Advanced).Value;
        var native = LanguagePreference.Create(language, ProficiencyLevel.Native).Value;

        // Assert
        basic.Proficiency.Should().Be(ProficiencyLevel.Basic);
        intermediate.Proficiency.Should().Be(ProficiencyLevel.Intermediate);
        advanced.Proficiency.Should().Be(ProficiencyLevel.Advanced);
        native.Proficiency.Should().Be(ProficiencyLevel.Native);
    }

    [Fact]
    public void LanguagePreference_ToString_Should_Return_Meaningful_String()
    {
        // Arrange
        var preference = LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value;

        // Act
        var stringValue = preference.ToString();

        // Assert
        stringValue.Should().Contain("Sinhala");
        stringValue.Should().Contain("Native");
    }

    [Fact]
    public void LanguagePreference_Should_Be_Immutable()
    {
        // Arrange
        var preference = LanguagePreference.Create(LanguageCode.Tamil, ProficiencyLevel.Advanced).Value;
        var originalLanguage = preference.Language;
        var originalProficiency = preference.Proficiency;

        // Assert - no setters should exist
        preference.Language.Should().Be(originalLanguage);
        preference.Proficiency.Should().Be(originalProficiency);
    }

    [Theory]
    [InlineData(ProficiencyLevel.Basic)]
    [InlineData(ProficiencyLevel.Intermediate)]
    [InlineData(ProficiencyLevel.Advanced)]
    [InlineData(ProficiencyLevel.Native)]
    public void LanguagePreference_Should_Work_With_All_Proficiency_Levels(ProficiencyLevel level)
    {
        // Arrange & Act
        var result = LanguagePreference.Create(LanguageCode.English, level);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Proficiency.Should().Be(level);
    }

    [Fact]
    public void LanguagePreference_Equality_Should_Be_Case_Sensitive_For_Language()
    {
        // Arrange
        var pref1 = LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value;
        var pref2 = LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value;

        // Act & Assert
        pref1.Should().Be(pref2);
        pref1.GetHashCode().Should().Be(pref2.GetHashCode());
    }

    [Fact]
    public void LanguagePreference_Should_Support_Multiple_Languages()
    {
        // Arrange & Act
        var sinhala = LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value;
        var tamil = LanguagePreference.Create(LanguageCode.Tamil, ProficiencyLevel.Advanced).Value;
        var english = LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Intermediate).Value;

        // Assert
        sinhala.Should().NotBe(tamil);
        tamil.Should().NotBe(english);
        sinhala.Should().NotBe(english);
    }
}
