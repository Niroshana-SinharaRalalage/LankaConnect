using FluentAssertions;
using LankaConnect.Infrastructure.Common;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Common;

/// <summary>
/// Unit tests for USStateHelper - Phase 6A Event Notifications fix.
/// Verifies state name normalization for metro area matching.
/// </summary>
public class USStateHelperTests
{
    #region NormalizeToAbbreviation Tests

    [Theory]
    [InlineData("California", "CA")]
    [InlineData("california", "CA")]
    [InlineData("CALIFORNIA", "CA")]
    [InlineData("New York", "NY")]
    [InlineData("new york", "NY")]
    [InlineData("Texas", "TX")]
    [InlineData("Florida", "FL")]
    [InlineData("Illinois", "IL")]
    public void NormalizeToAbbreviation_FullStateName_ReturnsAbbreviation(string input, string expected)
    {
        // Act
        var result = USStateHelper.NormalizeToAbbreviation(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("CA", "CA")]
    [InlineData("ca", "CA")]
    [InlineData("NY", "NY")]
    [InlineData("ny", "NY")]
    [InlineData("TX", "TX")]
    public void NormalizeToAbbreviation_AlreadyAbbreviation_ReturnsUppercase(string input, string expected)
    {
        // Act
        var result = USStateHelper.NormalizeToAbbreviation(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeToAbbreviation_NullOrEmpty_ReturnsNull(string? input)
    {
        // Act
        var result = USStateHelper.NormalizeToAbbreviation(input);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("InvalidState")]
    [InlineData("XX")]
    [InlineData("Sri Lanka")]
    [InlineData("Ontario")]
    public void NormalizeToAbbreviation_InvalidState_ReturnsNull(string input)
    {
        // Act
        var result = USStateHelper.NormalizeToAbbreviation(input);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("  California  ", "CA")]
    [InlineData("  CA  ", "CA")]
    public void NormalizeToAbbreviation_WithWhitespace_TrimsAndNormalizes(string input, string expected)
    {
        // Act
        var result = USStateHelper.NormalizeToAbbreviation(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region StatesMatch Tests

    [Theory]
    [InlineData("California", "CA", true)]
    [InlineData("california", "CA", true)]
    [InlineData("CA", "CA", true)]
    [InlineData("ca", "CA", true)]
    [InlineData("New York", "NY", true)]
    [InlineData("Texas", "TX", true)]
    public void StatesMatch_MatchingStates_ReturnsTrue(string inputState, string dbAbbreviation, bool expected)
    {
        // Act
        var result = USStateHelper.StatesMatch(inputState, dbAbbreviation);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("California", "NY", false)]
    [InlineData("CA", "NY", false)]
    [InlineData("Texas", "CA", false)]
    public void StatesMatch_DifferentStates_ReturnsFalse(string inputState, string dbAbbreviation, bool expected)
    {
        // Act
        var result = USStateHelper.StatesMatch(inputState, dbAbbreviation);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "CA", false)]
    [InlineData("", "CA", false)]
    [InlineData("California", null, false)]
    [InlineData("California", "", false)]
    [InlineData(null, null, false)]
    public void StatesMatch_NullOrEmpty_ReturnsFalse(string? inputState, string? dbAbbreviation, bool expected)
    {
        // Act
        var result = USStateHelper.StatesMatch(inputState, dbAbbreviation);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void StatesMatch_NonUSState_FallsBackToExactMatch()
    {
        // Arrange - Non-US state should fall back to exact comparison
        var inputState = "Ontario";
        var dbValue = "Ontario";

        // Act
        var result = USStateHelper.StatesMatch(inputState, dbValue);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region All 50 States Coverage

    [Fact]
    public void NormalizeToAbbreviation_AllUSStates_AreSupported()
    {
        // Verify all 50 US states are mapped correctly
        var expectedMappings = new Dictionary<string, string>
        {
            { "Alabama", "AL" }, { "Alaska", "AK" }, { "Arizona", "AZ" }, { "Arkansas", "AR" },
            { "California", "CA" }, { "Colorado", "CO" }, { "Connecticut", "CT" }, { "Delaware", "DE" },
            { "Florida", "FL" }, { "Georgia", "GA" }, { "Hawaii", "HI" }, { "Idaho", "ID" },
            { "Illinois", "IL" }, { "Indiana", "IN" }, { "Iowa", "IA" }, { "Kansas", "KS" },
            { "Kentucky", "KY" }, { "Louisiana", "LA" }, { "Maine", "ME" }, { "Maryland", "MD" },
            { "Massachusetts", "MA" }, { "Michigan", "MI" }, { "Minnesota", "MN" }, { "Mississippi", "MS" },
            { "Missouri", "MO" }, { "Montana", "MT" }, { "Nebraska", "NE" }, { "Nevada", "NV" },
            { "New Hampshire", "NH" }, { "New Jersey", "NJ" }, { "New Mexico", "NM" }, { "New York", "NY" },
            { "North Carolina", "NC" }, { "North Dakota", "ND" }, { "Ohio", "OH" }, { "Oklahoma", "OK" },
            { "Oregon", "OR" }, { "Pennsylvania", "PA" }, { "Rhode Island", "RI" }, { "South Carolina", "SC" },
            { "South Dakota", "SD" }, { "Tennessee", "TN" }, { "Texas", "TX" }, { "Utah", "UT" },
            { "Vermont", "VT" }, { "Virginia", "VA" }, { "Washington", "WA" }, { "West Virginia", "WV" },
            { "Wisconsin", "WI" }, { "Wyoming", "WY" }
        };

        foreach (var mapping in expectedMappings)
        {
            var result = USStateHelper.NormalizeToAbbreviation(mapping.Key);
            result.Should().Be(mapping.Value, because: $"{mapping.Key} should map to {mapping.Value}");
        }
    }

    #endregion
}
