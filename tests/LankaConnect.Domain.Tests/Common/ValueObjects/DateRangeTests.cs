using FluentAssertions;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.ValueObjects;

/// <summary>
/// TDD RED Phase: Test for DateRange value object
/// These tests should FAIL until we implement the DateRange class
/// </summary>
public class DateRangeTests
{
    [Fact]
    public void DateRange_Creation_ShouldInitializeWithValidDates()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        
        // Act
        var dateRange = DateRange.Create(startDate, endDate);
        
        // Assert
        dateRange.Should().NotBeNull();
        dateRange.StartDate.Should().Be(startDate);
        dateRange.EndDate.Should().Be(endDate);
        dateRange.Duration.Should().Be(TimeSpan.FromDays(30));
    }
    
    [Fact]
    public void DateRange_WithEndBeforeStart_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 31);
        var endDate = new DateTime(2025, 1, 1);
        
        // Act & Assert
        var act = () => DateRange.Create(startDate, endDate);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*end date must be greater than or equal to start date*");
    }
    
    [Fact]
    public void DateRange_Contains_ShouldDetectDateWithinRange()
    {
        // Arrange
        var dateRange = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var testDate = new DateTime(2025, 1, 15);
        
        // Act
        var contains = dateRange.Contains(testDate);
        
        // Assert
        contains.Should().BeTrue();
    }
    
    [Fact]
    public void DateRange_Contains_ShouldRejectDateOutsideRange()
    {
        // Arrange
        var dateRange = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var testDate = new DateTime(2025, 2, 1);
        
        // Act
        var contains = dateRange.Contains(testDate);
        
        // Assert
        contains.Should().BeFalse();
    }
    
    [Fact]
    public void DateRange_Overlaps_ShouldDetectOverlappingRanges()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 15));
        var range2 = DateRange.Create(new DateTime(2025, 1, 10), new DateTime(2025, 1, 25));
        
        // Act
        var overlaps = range1.Overlaps(range2);
        
        // Assert
        overlaps.Should().BeTrue();
    }
    
    [Fact]
    public void DateRange_Overlaps_ShouldRejectNonOverlappingRanges()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 15));
        var range2 = DateRange.Create(new DateTime(2025, 1, 20), new DateTime(2025, 1, 31));
        
        // Act
        var overlaps = range1.Overlaps(range2);
        
        // Assert
        overlaps.Should().BeFalse();
    }
    
    [Fact]
    public void DateRange_Intersection_ShouldReturnOverlappingPortion()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 15));
        var range2 = DateRange.Create(new DateTime(2025, 1, 10), new DateTime(2025, 1, 25));
        
        // Act
        var intersection = range1.Intersection(range2);
        
        // Assert
        intersection.Should().NotBeNull();
        intersection.StartDate.Should().Be(new DateTime(2025, 1, 10));
        intersection.EndDate.Should().Be(new DateTime(2025, 1, 15));
    }
    
    [Fact]
    public void DateRange_CulturalCalendar_ShouldSupportPoyadayRanges()
    {
        // Arrange - Vesak Poya period (cultural intelligence feature)
        var vesakStart = new DateTime(2025, 5, 12); // Vesak Day
        var vesakEnd = new DateTime(2025, 5, 14);   // 3-day cultural period
        
        // Act
        var vesakPeriod = DateRange.Create(vesakStart, vesakEnd);
        vesakPeriod.SetCulturalContext("Vesak_Poya", "Sri_Lankan_Buddhism");
        
        // Assert
        vesakPeriod.CulturalEventType.Should().Be("Vesak_Poya");
        vesakPeriod.CulturalRegion.Should().Be("Sri_Lankan_Buddhism");
        vesakPeriod.IsCulturallySignificant.Should().BeTrue();
    }
    
    [Fact]
    public void DateRange_DisasterRecovery_ShouldSupportBackupWindows()
    {
        // Arrange - Disaster recovery backup window
        var backupStart = new DateTime(2025, 1, 1, 2, 0, 0); // 2 AM
        var backupEnd = new DateTime(2025, 1, 1, 6, 0, 0);   // 6 AM
        
        // Act
        var backupWindow = DateRange.Create(backupStart, backupEnd);
        backupWindow.SetBackupContext("Daily_Full_Backup", "Low_Traffic_Window");
        
        // Assert
        backupWindow.BackupType.Should().Be("Daily_Full_Backup");
        backupWindow.TrafficProfile.Should().Be("Low_Traffic_Window");
        backupWindow.Duration.Should().Be(TimeSpan.FromHours(4));
    }
    
    [Theory]
    [InlineData(1, 31, 30)] // January (31 days minus 1 day)
    [InlineData(2, 28, 27)] // February non-leap year  
    [InlineData(12, 31, 30)] // December
    public void DateRange_MonthlyRanges_ShouldCalculateCorrectDurations(int month, int lastDay, int expectedDays)
    {
        // Arrange
        var startDate = new DateTime(2025, month, 1);
        var endDate = new DateTime(2025, month, lastDay);
        
        // Act
        var monthRange = DateRange.Create(startDate, endDate);
        
        // Assert
        monthRange.Duration.Days.Should().Be(expectedDays);
        monthRange.IsMonthlyRange.Should().BeTrue();
    }
    
    [Fact]
    public void DateRange_ValueObjectEquality_ShouldCompareByValue()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var range2 = DateRange.Create(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        
        // Act & Assert
        range1.Should().BeEquivalentTo(range2);
        (range1 == range2).Should().BeTrue();
        range1.GetHashCode().Should().Be(range2.GetHashCode());
    }
}