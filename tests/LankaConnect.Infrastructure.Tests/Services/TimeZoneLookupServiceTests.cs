using FluentAssertions;
using LankaConnect.Infrastructure.Services;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services;

/// <summary>
/// Phase 6A.97: Unit tests for TimeZoneLookupService
/// Tests US state to IANA timezone mapping for event date/time display
/// </summary>
public class TimeZoneLookupServiceTests
{
    private readonly TimeZoneLookupService _service;

    public TimeZoneLookupServiceTests()
    {
        _service = new TimeZoneLookupService();
    }

    #region GetTimeZoneFromState Tests - Eastern Time States

    [Theory]
    [InlineData("OH", "America/New_York")]
    [InlineData("Ohio", "America/New_York")]
    [InlineData("oh", "America/New_York")]  // Case insensitive
    [InlineData("OHIO", "America/New_York")]
    [InlineData("NY", "America/New_York")]
    [InlineData("New York", "America/New_York")]
    [InlineData("PA", "America/New_York")]
    [InlineData("Pennsylvania", "America/New_York")]
    [InlineData("FL", "America/New_York")]
    [InlineData("Florida", "America/New_York")]
    [InlineData("GA", "America/New_York")]
    [InlineData("Georgia", "America/New_York")]
    [InlineData("NC", "America/New_York")]
    [InlineData("North Carolina", "America/New_York")]
    [InlineData("VA", "America/New_York")]
    [InlineData("Virginia", "America/New_York")]
    [InlineData("MI", "America/New_York")]
    [InlineData("Michigan", "America/New_York")]
    [InlineData("MA", "America/New_York")]
    [InlineData("Massachusetts", "America/New_York")]
    [InlineData("NJ", "America/New_York")]
    [InlineData("New Jersey", "America/New_York")]
    [InlineData("MD", "America/New_York")]
    [InlineData("Maryland", "America/New_York")]
    [InlineData("CT", "America/New_York")]
    [InlineData("Connecticut", "America/New_York")]
    public void GetTimeZoneFromState_EasternTimeStates_ReturnsNewYork(string state, string expectedTimezone)
    {
        // Act
        var result = _service.GetTimeZoneFromState(state);

        // Assert
        result.Should().Be(expectedTimezone);
    }

    #endregion

    #region GetTimeZoneFromState Tests - Central Time States

    [Theory]
    [InlineData("IL", "America/Chicago")]
    [InlineData("Illinois", "America/Chicago")]
    [InlineData("TX", "America/Chicago")]
    [InlineData("Texas", "America/Chicago")]
    [InlineData("MN", "America/Chicago")]
    [InlineData("Minnesota", "America/Chicago")]
    [InlineData("WI", "America/Chicago")]
    [InlineData("Wisconsin", "America/Chicago")]
    [InlineData("MO", "America/Chicago")]
    [InlineData("Missouri", "America/Chicago")]
    [InlineData("LA", "America/Chicago")]
    [InlineData("Louisiana", "America/Chicago")]
    [InlineData("AL", "America/Chicago")]
    [InlineData("Alabama", "America/Chicago")]
    [InlineData("OK", "America/Chicago")]
    [InlineData("Oklahoma", "America/Chicago")]
    public void GetTimeZoneFromState_CentralTimeStates_ReturnsChicago(string state, string expectedTimezone)
    {
        // Act
        var result = _service.GetTimeZoneFromState(state);

        // Assert
        result.Should().Be(expectedTimezone);
    }

    #endregion

    #region GetTimeZoneFromState Tests - Mountain Time States

    [Theory]
    [InlineData("CO", "America/Denver")]
    [InlineData("Colorado", "America/Denver")]
    [InlineData("NM", "America/Denver")]
    [InlineData("New Mexico", "America/Denver")]
    [InlineData("UT", "America/Denver")]
    [InlineData("Utah", "America/Denver")]
    [InlineData("WY", "America/Denver")]
    [InlineData("Wyoming", "America/Denver")]
    [InlineData("MT", "America/Denver")]
    [InlineData("Montana", "America/Denver")]
    public void GetTimeZoneFromState_MountainTimeStates_ReturnsDenver(string state, string expectedTimezone)
    {
        // Act
        var result = _service.GetTimeZoneFromState(state);

        // Assert
        result.Should().Be(expectedTimezone);
    }

    #endregion

    #region GetTimeZoneFromState Tests - Pacific Time States

    [Theory]
    [InlineData("CA", "America/Los_Angeles")]
    [InlineData("California", "America/Los_Angeles")]
    [InlineData("WA", "America/Los_Angeles")]
    [InlineData("Washington", "America/Los_Angeles")]
    [InlineData("OR", "America/Los_Angeles")]
    [InlineData("Oregon", "America/Los_Angeles")]
    [InlineData("NV", "America/Los_Angeles")]
    [InlineData("Nevada", "America/Los_Angeles")]
    public void GetTimeZoneFromState_PacificTimeStates_ReturnsLosAngeles(string state, string expectedTimezone)
    {
        // Act
        var result = _service.GetTimeZoneFromState(state);

        // Assert
        result.Should().Be(expectedTimezone);
    }

    #endregion

    #region GetTimeZoneFromState Tests - Special Timezones

    [Theory]
    [InlineData("AZ", "America/Phoenix")]
    [InlineData("Arizona", "America/Phoenix")]
    public void GetTimeZoneFromState_Arizona_ReturnsPhoenix(string state, string expectedTimezone)
    {
        // Arizona doesn't observe DST (except Navajo Nation)
        var result = _service.GetTimeZoneFromState(state);
        result.Should().Be(expectedTimezone);
    }

    [Theory]
    [InlineData("AK", "America/Anchorage")]
    [InlineData("Alaska", "America/Anchorage")]
    public void GetTimeZoneFromState_Alaska_ReturnsAnchorage(string state, string expectedTimezone)
    {
        var result = _service.GetTimeZoneFromState(state);
        result.Should().Be(expectedTimezone);
    }

    [Theory]
    [InlineData("HI", "Pacific/Honolulu")]
    [InlineData("Hawaii", "Pacific/Honolulu")]
    public void GetTimeZoneFromState_Hawaii_ReturnsHonolulu(string state, string expectedTimezone)
    {
        var result = _service.GetTimeZoneFromState(state);
        result.Should().Be(expectedTimezone);
    }

    #endregion

    #region GetTimeZoneFromState Tests - Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTimeZoneFromState_NullOrEmpty_ReturnsDefaultEastern(string? state)
    {
        // Act
        var result = _service.GetTimeZoneFromState(state);

        // Assert
        result.Should().Be("America/New_York", "Default should be Eastern time for unknown states");
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("XYZ")]
    [InlineData("Invalid State")]
    public void GetTimeZoneFromState_UnknownState_ReturnsDefaultEastern(string state)
    {
        // Act
        var result = _service.GetTimeZoneFromState(state);

        // Assert
        result.Should().Be("America/New_York", "Default should be Eastern time for unknown states");
    }

    [Fact]
    public void GetTimeZoneFromState_WithLeadingTrailingWhitespace_ShouldTrim()
    {
        // Act
        var result = _service.GetTimeZoneFromState("  Ohio  ");

        // Assert
        result.Should().Be("America/New_York");
    }

    #endregion

    #region GetTimezoneAbbreviation Tests

    [Fact]
    public void GetTimezoneAbbreviation_EasternTimeWinter_ReturnsEST()
    {
        // January is standard time (not DST)
        var winterDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetTimezoneAbbreviation("America/New_York", winterDate);

        // Assert
        result.Should().Be("EST");
    }

    [Fact]
    public void GetTimezoneAbbreviation_EasternTimeSummer_ReturnsEDT()
    {
        // July is daylight saving time
        var summerDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetTimezoneAbbreviation("America/New_York", summerDate);

        // Assert
        result.Should().Be("EDT");
    }

    [Fact]
    public void GetTimezoneAbbreviation_PacificTimeWinter_ReturnsPST()
    {
        var winterDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetTimezoneAbbreviation("America/Los_Angeles", winterDate);

        // Assert
        result.Should().Be("PST");
    }

    [Fact]
    public void GetTimezoneAbbreviation_PacificTimeSummer_ReturnsPDT()
    {
        var summerDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetTimezoneAbbreviation("America/Los_Angeles", summerDate);

        // Assert
        result.Should().Be("PDT");
    }

    [Fact]
    public void GetTimezoneAbbreviation_CentralTimeWinter_ReturnsCST()
    {
        var winterDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetTimezoneAbbreviation("America/Chicago", winterDate);

        // Assert
        result.Should().Be("CST");
    }

    [Fact]
    public void GetTimezoneAbbreviation_MountainTimeWinter_ReturnsMST()
    {
        var winterDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetTimezoneAbbreviation("America/Denver", winterDate);

        // Assert
        result.Should().Be("MST");
    }

    [Fact]
    public void GetTimezoneAbbreviation_Arizona_AlwaysMST()
    {
        // Arizona doesn't observe DST
        var winterDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var summerDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var winterResult = _service.GetTimezoneAbbreviation("America/Phoenix", winterDate);
        var summerResult = _service.GetTimezoneAbbreviation("America/Phoenix", summerDate);

        // Assert - Arizona is always MST (no DST)
        winterResult.Should().Be("MST");
        summerResult.Should().Be("MST");
    }

    [Fact]
    public void GetTimezoneAbbreviation_Hawaii_AlwaysHST()
    {
        // Hawaii doesn't observe DST
        var winterDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var summerDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var winterResult = _service.GetTimezoneAbbreviation("Pacific/Honolulu", winterDate);
        var summerResult = _service.GetTimezoneAbbreviation("Pacific/Honolulu", summerDate);

        // Assert - Hawaii is always HST (no DST)
        winterResult.Should().Be("HST");
        summerResult.Should().Be("HST");
    }

    #endregion

    #region IsValidTimeZone Tests

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("America/Chicago")]
    [InlineData("America/Denver")]
    [InlineData("America/Los_Angeles")]
    [InlineData("America/Phoenix")]
    [InlineData("America/Anchorage")]
    [InlineData("Pacific/Honolulu")]
    public void IsValidTimeZone_ValidTimezones_ReturnsTrue(string timeZoneId)
    {
        // Act
        var result = _service.IsValidTimeZone(timeZoneId);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Invalid/Timezone")]
    [InlineData("Not_A_Timezone")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidTimeZone_InvalidTimezones_ReturnsFalse(string? timeZoneId)
    {
        // Act
        var result = _service.IsValidTimeZone(timeZoneId!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DefaultTimeZoneId Tests

    [Fact]
    public void DefaultTimeZoneId_ShouldBeEasternTime()
    {
        // Assert
        _service.DefaultTimeZoneId.Should().Be("America/New_York");
    }

    #endregion
}
