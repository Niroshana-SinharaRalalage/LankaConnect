using FluentAssertions;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.ValueObjects;

/// <summary>
/// TDD RED Phase: Tests for AnalysisPeriod ValueObject
/// These tests should FAIL until we implement the AnalysisPeriod class
/// Focus on cultural intelligence-aware analysis period validation
/// </summary>
public class AnalysisPeriodTests
{
    [Fact]
    public void AnalysisPeriod_Create_ShouldCreateValidPeriodFromTimeSpan()
    {
        // Arrange
        var timeSpan = TimeSpan.FromHours(24);
        
        // Act
        var period = AnalysisPeriod.Create(timeSpan);
        
        // Assert
        period.Should().NotBeNull();
        period.Duration.Should().Be(timeSpan);
        period.TotalHours.Should().Be(24);
    }
    
    [Fact]
    public void AnalysisPeriod_CreateFromDays_ShouldCreateValidDaysPeriod()
    {
        // Arrange
        var days = 7;
        
        // Act
        var period = AnalysisPeriod.FromDays(days);
        
        // Assert
        period.Should().NotBeNull();
        period.Duration.Should().Be(TimeSpan.FromDays(days));
        period.TotalDays.Should().Be(days);
    }
    
    [Fact]
    public void AnalysisPeriod_CreateFromHours_ShouldCreateValidHoursPeriod()
    {
        // Arrange
        var hours = 48;
        
        // Act
        var period = AnalysisPeriod.FromHours(hours);
        
        // Assert
        period.Should().NotBeNull();
        period.Duration.Should().Be(TimeSpan.FromHours(hours));
        period.TotalHours.Should().Be(hours);
    }
    
    [Fact]
    public void AnalysisPeriod_CreateFromMinutes_ShouldCreateValidMinutesPeriod()
    {
        // Arrange
        var minutes = 720; // 12 hours
        
        // Act
        var period = AnalysisPeriod.FromMinutes(minutes);
        
        // Assert
        period.Should().NotBeNull();
        period.Duration.Should().Be(TimeSpan.FromMinutes(minutes));
        period.TotalMinutes.Should().Be(minutes);
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-24)]
    [InlineData(-100)]
    public void AnalysisPeriod_CreateWithNegativeDuration_ShouldThrowArgumentException(double hours)
    {
        // Arrange
        var timeSpan = TimeSpan.FromHours(hours);
        
        // Act & Assert
        var act = () => AnalysisPeriod.Create(timeSpan);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Analysis period cannot be negative*");
    }
    
    [Fact]
    public void AnalysisPeriod_CreateWithZeroDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var timeSpan = TimeSpan.Zero;
        
        // Act & Assert
        var act = () => AnalysisPeriod.Create(timeSpan);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Analysis period cannot be zero*");
    }
    
    [Fact]
    public void AnalysisPeriod_CreateWithExcessiveDuration_ShouldThrowArgumentException()
    {
        // Arrange - More than 365 days should be invalid
        var timeSpan = TimeSpan.FromDays(400);
        
        // Act & Assert
        var act = () => AnalysisPeriod.Create(timeSpan);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Analysis period cannot exceed 365 days*");
    }
    
    [Fact]
    public void AnalysisPeriod_Equality_ShouldBeEqualForSameDurations()
    {
        // Arrange
        var period1 = AnalysisPeriod.FromHours(24);
        var period2 = AnalysisPeriod.FromDays(1);
        
        // Act & Assert
        period1.Should().Be(period2);
        (period1 == period2).Should().BeTrue();
        period1.GetHashCode().Should().Be(period2.GetHashCode());
    }
    
    [Fact]
    public void AnalysisPeriod_Inequality_ShouldNotBeEqualForDifferentDurations()
    {
        // Arrange
        var period1 = AnalysisPeriod.FromHours(24);
        var period2 = AnalysisPeriod.FromHours(48);
        
        // Act & Assert
        period1.Should().NotBe(period2);
        (period1 != period2).Should().BeTrue();
    }
    
    [Fact]
    public void AnalysisPeriod_Comparison_ShouldCompareCorrectly()
    {
        // Arrange
        var shortPeriod = AnalysisPeriod.FromHours(12);
        var longPeriod = AnalysisPeriod.FromHours(24);
        
        // Act & Assert
        (shortPeriod < longPeriod).Should().BeTrue();
        (longPeriod > shortPeriod).Should().BeTrue();
        shortPeriod.CompareTo(longPeriod).Should().BeLessThan(0);
        longPeriod.CompareTo(shortPeriod).Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void AnalysisPeriod_ToString_ShouldProvideReadableFormat()
    {
        // Arrange
        var period = AnalysisPeriod.FromDays(7);
        
        // Act
        var result = period.ToString();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("7");
        result.Should().Contain("day", Exactly.Once().IgnoreCase());
    }
    
    [Fact]
    public void AnalysisPeriod_CulturalEventPeriod_ShouldCreateValidCulturalAnalysisPeriod()
    {
        // Arrange & Act - Cultural events typically require 30-day analysis periods
        var culturalPeriod = AnalysisPeriod.CulturalEventPeriod();
        
        // Assert
        culturalPeriod.Should().NotBeNull();
        culturalPeriod.TotalDays.Should().Be(30);
        culturalPeriod.IsCulturalAnalysisPeriod.Should().BeTrue();
    }
    
    [Fact]
    public void AnalysisPeriod_SecurityIncidentPeriod_ShouldCreateValidSecurityAnalysisPeriod()
    {
        // Arrange & Act - Security incidents typically require 7-day analysis periods
        var securityPeriod = AnalysisPeriod.SecurityIncidentPeriod();
        
        // Assert
        securityPeriod.Should().NotBeNull();
        securityPeriod.TotalDays.Should().Be(7);
        securityPeriod.IsSecurityAnalysisPeriod.Should().BeTrue();
    }
    
    [Fact]
    public void AnalysisPeriod_RealTimeMonitoringPeriod_ShouldCreateValidRealTimePeriod()
    {
        // Arrange & Act - Real-time monitoring typically uses 1-hour periods
        var realTimePeriod = AnalysisPeriod.RealTimeMonitoringPeriod();
        
        // Assert
        realTimePeriod.Should().NotBeNull();
        realTimePeriod.TotalHours.Should().Be(1);
        realTimePeriod.IsRealTimeAnalysisPeriod.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(1, false)]      // 1 day - not cultural
    [InlineData(7, false)]      // 7 days - security period
    [InlineData(30, true)]      // 30 days - cultural period
    [InlineData(60, false)]     // 60 days - not standard cultural
    [InlineData(90, true)]      // 90 days - quarterly cultural analysis
    public void AnalysisPeriod_IsCulturalAnalysisPeriod_ShouldDetectCulturalPeriods(int days, bool expectedCultural)
    {
        // Arrange
        var period = AnalysisPeriod.FromDays(days);
        
        // Act & Assert
        period.IsCulturalAnalysisPeriod.Should().Be(expectedCultural);
    }
    
    [Fact]
    public void AnalysisPeriod_IsWithinRange_ShouldValidateRangeCorrectly()
    {
        // Arrange
        var period = AnalysisPeriod.FromDays(15);
        var startDate = DateTime.UtcNow.AddDays(-20);
        var endDate = DateTime.UtcNow.AddDays(-5);
        
        // Act
        var isWithinRange = period.IsWithinRange(startDate, endDate);
        
        // Assert
        isWithinRange.Should().BeTrue();
    }
    
    [Fact]
    public void AnalysisPeriod_GetCulturallyOptimizedPeriod_ShouldAdjustForCulturalEvents()
    {
        // Arrange
        var basePeriod = AnalysisPeriod.FromDays(7);
        var culturalEventType = "Vesak_Day";
        
        // Act
        var optimizedPeriod = basePeriod.GetCulturallyOptimizedPeriod(culturalEventType);
        
        // Assert
        optimizedPeriod.Should().NotBeNull();
        optimizedPeriod.TotalDays.Should().BeGreaterThan(basePeriod.TotalDays);
        optimizedPeriod.CulturalContext.Should().Be(culturalEventType);
    }
    
    [Fact]
    public void AnalysisPeriod_ExpandForCulturalSignificance_ShouldExtendPeriodForImportantEvents()
    {
        // Arrange
        var basePeriod = AnalysisPeriod.FromDays(14);
        var significanceMultiplier = 2.0;
        
        // Act
        var expandedPeriod = basePeriod.ExpandForCulturalSignificance(significanceMultiplier);
        
        // Assert
        expandedPeriod.Should().NotBeNull();
        expandedPeriod.TotalDays.Should().Be(basePeriod.TotalDays * significanceMultiplier);
        expandedPeriod.HasCulturalExpansion.Should().BeTrue();
    }
    
    [Fact] 
    public void AnalysisPeriod_GetAnalysisWindows_ShouldCreateNonOverlappingWindows()
    {
        // Arrange
        var period = AnalysisPeriod.FromDays(30);
        var windowSize = AnalysisPeriod.FromDays(7);
        
        // Act
        var windows = period.GetAnalysisWindows(windowSize);
        
        // Assert
        windows.Should().NotBeNull();
        windows.Should().HaveCount(4); // 30 days / 7 days = ~4 windows
        windows.Should().OnlyHaveUniqueItems();
    }
}