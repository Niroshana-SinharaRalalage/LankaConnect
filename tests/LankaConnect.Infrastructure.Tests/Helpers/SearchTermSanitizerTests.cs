using FluentAssertions;
using LankaConnect.Infrastructure.Helpers;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Helpers;

/// <summary>
/// Tests for SearchTermSanitizer helper class.
/// Phase 6A.XX: Hyphen negation bug fix - tests ensure search terms with hyphens
/// are sanitized to prevent PostgreSQL's websearch_to_tsquery from interpreting
/// them as negation operators.
/// </summary>
public class SearchTermSanitizerTests
{
    #region Hyphen Sanitization Tests

    [Theory]
    [InlineData("Sample group tier testing - Varuni", "Sample group tier testing Varuni")]
    [InlineData("Event Name - Part 2", "Event Name Part 2")]
    [InlineData("A - B - C", "A B C")]
    [InlineData("Music - Rock - Live", "Music Rock Live")]
    public void SanitizeForWebSearch_RemovesSpaceHyphenSpacePattern(string input, string expected)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Sample -Varuni", "Sample Varuni")]
    [InlineData("Music -rock", "Music rock")]
    public void SanitizeForWebSearch_RemovesSpaceHyphenPattern(string input, string expected)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Sample- Varuni", "Sample Varuni")]
    [InlineData("Music- rock", "Music rock")]
    public void SanitizeForWebSearch_RemovesHyphenSpacePattern(string input, string expected)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Preserve Valid Patterns Tests

    [Theory]
    [InlineData("well-known event")]
    [InlineData("self-service portal")]
    [InlineData("re-register")]
    public void SanitizeForWebSearch_PreservesHyphenatedCompoundWords(string input)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(input, "hyphenated compound words without surrounding spaces should be preserved");
    }

    [Theory]
    [InlineData("2023-2024 events")]
    [InlineData("January-February festival")]
    [InlineData("123-456-7890")]
    public void SanitizeForWebSearch_PreservesHyphenatedNumbersAndDates(string input)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(input, "hyphenated numbers and date ranges should be preserved");
    }

    [Theory]
    [InlineData("music OR dance")]
    [InlineData("festival OR concert")]
    public void SanitizeForWebSearch_PreservesOROperator(string input)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(input, "OR operator should be preserved for websearch functionality");
    }

    [Theory]
    [InlineData("\"exact phrase match\"")]
    [InlineData("\"Sri Lankan Gossip\"")]
    public void SanitizeForWebSearch_PreservesQuotedPhrases(string input)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(input, "quoted phrases should be preserved for websearch functionality");
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SanitizeForWebSearch_HandlesEmptyOrNullInput(string? input)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input!);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("  Sample - Varuni  ", "Sample Varuni")]
    [InlineData("   A - B   ", "A B")]
    public void SanitizeForWebSearch_TrimsWhitespaceAndSanitizes(string input, string expected)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeForWebSearch_HandlesMixedPatterns()
    {
        // Arrange
        var input = "Sri Lankan - Gossip - well-known event - 2024";

        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        // " - " patterns are removed, but "well-known" is preserved
        result.Should().Be("Sri Lankan Gossip well-known event 2024");
    }

    [Fact]
    public void SanitizeForWebSearch_HandlesConsecutiveHyphens()
    {
        // Arrange
        var input = "Event -- Name";

        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        // Double hyphen without proper space pattern should be handled gracefully
        result.Should().NotContain("--");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void SanitizeForWebSearch_FixesVaruniSearchBug()
    {
        // Arrange - This is the exact bug scenario
        var buggyInput = "Sample group tier testing - Varuni";

        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(buggyInput);

        // Assert
        // After sanitization, websearch_to_tsquery should produce:
        // 'sampl' & 'group' & 'tier' & 'test' & 'varuni' (AND, not NOT)
        result.Should().Be("Sample group tier testing Varuni");
        result.Should().NotContain(" - ", "hyphen separator should be removed to prevent negation");
    }

    [Theory]
    [InlineData("Sample group tier testing - Varu")]
    [InlineData("Sri Lankan Gossip Sesh - with Varuni")]
    [InlineData("Music Festival - Live - Band - 2024")]
    public void SanitizeForWebSearch_HandlesVariousEventTitlePatterns(string input)
    {
        // Act
        var result = SearchTermSanitizer.SanitizeForWebSearch(input);

        // Assert
        result.Should().NotContain(" - ", "all ' - ' patterns should be sanitized");
        result.Should().NotContain(" -", "all ' -' patterns should be sanitized");
        result.Should().NotContain("- ", "all '- ' patterns should be sanitized");
    }

    #endregion
}
