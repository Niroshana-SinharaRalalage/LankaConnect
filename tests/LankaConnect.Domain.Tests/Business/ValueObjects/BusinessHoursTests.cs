using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class BusinessHoursTests
{
    [Fact]
    public void Create_WithValidHours_ShouldReturnSuccess()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };

        var result = BusinessHours.Create(hours);

        Assert.True(result.IsSuccess);
        var businessHours = result.Value;
        Assert.NotNull(businessHours.WeeklyHours[DayOfWeek.Monday]);
        Assert.NotNull(businessHours.WeeklyHours[DayOfWeek.Tuesday]);
        Assert.NotNull(businessHours.WeeklyHours[DayOfWeek.Wednesday]);
        Assert.Null(businessHours.WeeklyHours[DayOfWeek.Thursday]);
        Assert.Null(businessHours.WeeklyHours[DayOfWeek.Friday]);
        Assert.Null(businessHours.WeeklyHours[DayOfWeek.Saturday]);
        Assert.Null(businessHours.WeeklyHours[DayOfWeek.Sunday]);
    }

    [Fact]
    public void Create_WithClosedDays_ShouldSetThemAsNull()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (null, null) }, // Closed
            { DayOfWeek.Wednesday, (new TimeOnly(10, 0), null) } // Invalid - only open time
        };

        var result = BusinessHours.Create(hours);

        Assert.True(result.IsSuccess);
        var businessHours = result.Value;
        Assert.NotNull(businessHours.WeeklyHours[DayOfWeek.Monday]);
        Assert.Null(businessHours.WeeklyHours[DayOfWeek.Tuesday]);
        Assert.Null(businessHours.WeeklyHours[DayOfWeek.Wednesday]);
    }

    [Fact]
    public void Create_WithNullHours_ShouldReturnFailure()
    {
        var result = BusinessHours.Create(null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Business hours must be specified", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyHours_ShouldReturnFailure()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>();

        var result = BusinessHours.Create(hours);

        Assert.True(result.IsFailure);
        Assert.Contains("Business hours must be specified", result.Errors);
    }

    [Fact]
    public void Create_WithInvalidTimeRange_ShouldReturnFailure()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(17, 0), new TimeOnly(9, 0)) } // Close before open
        };

        var result = BusinessHours.Create(hours);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid hours for Monday", result.Errors.First());
    }

    [Fact]
    public void CreateAlwaysClosed_ShouldCreateClosedHours()
    {
        var businessHours = BusinessHours.CreateAlwaysClosed();

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            Assert.Null(businessHours.WeeklyHours[day]);
            Assert.True(businessHours.IsClosedOn(day));
        }
    }

    [Fact]
    public void Create24x7_ShouldCreateAlwaysOpenHours()
    {
        var businessHours = BusinessHours.Create24x7();

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            Assert.NotNull(businessHours.WeeklyHours[day]);
            Assert.False(businessHours.IsClosedOn(day));
        }
    }

    [Fact]
    public void IsOpenAt_WithinBusinessHours_ShouldReturnTrue()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };
        var businessHours = BusinessHours.Create(hours).Value;
        
        // Monday at 12:00 PM
        var testDateTime = new DateTime(2023, 10, 16, 12, 0, 0); // Monday

        var isOpen = businessHours.IsOpenAt(testDateTime);

        Assert.True(isOpen);
    }

    [Fact]
    public void IsOpenAt_OutsideBusinessHours_ShouldReturnFalse()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };
        var businessHours = BusinessHours.Create(hours).Value;
        
        // Monday at 8:00 AM (before opening)
        var testDateTime = new DateTime(2023, 10, 16, 8, 0, 0); // Monday

        var isOpen = businessHours.IsOpenAt(testDateTime);

        Assert.False(isOpen);
    }

    [Fact]
    public void IsOpenAt_OnClosedDay_ShouldReturnFalse()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
            // Sunday not included - should be closed
        };
        var businessHours = BusinessHours.Create(hours).Value;
        
        // Sunday at 12:00 PM
        var testDateTime = new DateTime(2023, 10, 15, 12, 0, 0); // Sunday

        var isOpen = businessHours.IsOpenAt(testDateTime);

        Assert.False(isOpen);
    }

    [Fact]
    public void IsOpenAt_AtExactOpeningTime_ShouldReturnTrue()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };
        var businessHours = BusinessHours.Create(hours).Value;
        
        // Monday at 9:00 AM (exact opening time)
        var testDateTime = new DateTime(2023, 10, 16, 9, 0, 0); // Monday

        var isOpen = businessHours.IsOpenAt(testDateTime);

        Assert.True(isOpen);
    }

    [Fact]
    public void IsOpenAt_AtExactClosingTime_ShouldReturnTrue()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };
        var businessHours = BusinessHours.Create(hours).Value;
        
        // Monday at 5:00 PM (exact closing time)
        var testDateTime = new DateTime(2023, 10, 16, 17, 0, 0); // Monday

        var isOpen = businessHours.IsOpenAt(testDateTime);

        Assert.True(isOpen);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, false)]
    [InlineData(DayOfWeek.Tuesday, false)]
    [InlineData(DayOfWeek.Wednesday, true)]
    [InlineData(DayOfWeek.Thursday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, true)]
    [InlineData(DayOfWeek.Sunday, true)]
    public void IsClosedOn_WithMixedHours_ShouldReturnExpectedResult(DayOfWeek day, bool expectedClosed)
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(10, 0), new TimeOnly(18, 0)) }
            // Other days not specified - should be closed
        };
        var businessHours = BusinessHours.Create(hours).Value;

        var isClosed = businessHours.IsClosedOn(day);

        Assert.Equal(expectedClosed, isClosed);
    }

    [Fact]
    public void GetHoursFor_WithExistingDay_ShouldReturnHours()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };
        var businessHours = BusinessHours.Create(hours).Value;

        var mondayHours = businessHours.GetHoursFor(DayOfWeek.Monday);

        Assert.NotNull(mondayHours);
        Assert.Equal(new TimeOnly(9, 0), mondayHours.Start);
        Assert.Equal(new TimeOnly(17, 0), mondayHours.End);
    }

    [Fact]
    public void GetHoursFor_WithClosedDay_ShouldReturnNull()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };
        var businessHours = BusinessHours.Create(hours).Value;

        var sundayHours = businessHours.GetHoursFor(DayOfWeek.Sunday);

        Assert.Null(sundayHours);
    }

    [Fact]
    public void Equality_WithSameHours_ShouldBeEqual()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };

        var businessHours1 = BusinessHours.Create(hours).Value;
        var businessHours2 = BusinessHours.Create(hours).Value;

        Assert.Equal(businessHours1, businessHours2);
    }

    [Fact]
    public void Equality_WithDifferentHours_ShouldNotBeEqual()
    {
        var hours1 = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };

        var hours2 = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(10, 0), new TimeOnly(18, 0)) }
        };

        var businessHours1 = BusinessHours.Create(hours1).Value;
        var businessHours2 = BusinessHours.Create(hours2).Value;

        Assert.NotEqual(businessHours1, businessHours2);
    }

    [Fact]
    public void ToString_WithMixedHours_ShouldFormatCorrectly()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (null, null) } // Closed
        };
        var businessHours = BusinessHours.Create(hours).Value;

        var result = businessHours.ToString();

        Assert.Contains("Mon:", result);
        Assert.Contains("09:00 - 17:00", result);
        Assert.Contains("Tue: Closed", result);
    }

    [Fact]
    public void GetHashCode_WithSameHours_ShouldBeEqual()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) }
        };

        var businessHours1 = BusinessHours.Create(hours).Value;
        var businessHours2 = BusinessHours.Create(hours).Value;

        Assert.Equal(businessHours1.GetHashCode(), businessHours2.GetHashCode());
    }

    [Fact]
    public void Create_WithAllDaysSpecified_ShouldSetAllDays()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            hours[day] = (new TimeOnly(8, 0), new TimeOnly(20, 0));
        }

        var result = BusinessHours.Create(hours);

        Assert.True(result.IsSuccess);
        var businessHours = result.Value;
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            Assert.NotNull(businessHours.WeeklyHours[day]);
            Assert.False(businessHours.IsClosedOn(day));
        }
    }
}

public class TimeRangeTests
{
    [Fact]
    public void Create_WithValidTimes_ShouldReturnSuccess()
    {
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);

        var result = TimeRange.Create(start, end);

        Assert.True(result.IsSuccess);
        var range = result.Value;
        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
    }

    [Fact]
    public void Create_WithStartEqualToEnd_ShouldReturnFailure()
    {
        var time = new TimeOnly(12, 0);

        var result = TimeRange.Create(time, time);

        Assert.True(result.IsFailure);
        Assert.Contains("Start time must be before end time", result.Errors);
    }

    [Fact]
    public void Create_WithStartAfterEnd_ShouldReturnFailure()
    {
        var start = new TimeOnly(17, 0);
        var end = new TimeOnly(9, 0);

        var result = TimeRange.Create(start, end);

        Assert.True(result.IsFailure);
        Assert.Contains("Start time must be before end time", result.Errors);
    }

    [Theory]
    [InlineData(8, 0, false)]   // Before start
    [InlineData(9, 0, true)]    // At start
    [InlineData(12, 0, true)]   // In middle
    [InlineData(17, 0, true)]   // At end
    [InlineData(18, 0, false)]  // After end
    public void Contains_WithVariousTimes_ShouldReturnExpectedResult(int hour, int minute, bool expected)
    {
        var range = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var testTime = new TimeOnly(hour, minute);

        var contains = range.Contains(testTime);

        Assert.Equal(expected, contains);
    }

    [Fact]
    public void Equality_WithSameTimes_ShouldBeEqual()
    {
        var range1 = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var range2 = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;

        Assert.Equal(range1, range2);
    }

    [Fact]
    public void Equality_WithDifferentTimes_ShouldNotBeEqual()
    {
        var range1 = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var range2 = TimeRange.Create(new TimeOnly(10, 0), new TimeOnly(18, 0)).Value;

        Assert.NotEqual(range1, range2);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var range = TimeRange.Create(new TimeOnly(9, 30), new TimeOnly(17, 45)).Value;

        var result = range.ToString();

        Assert.Equal("09:30 - 17:45", result);
    }

    [Fact]
    public void GetHashCode_WithSameTimes_ShouldBeEqual()
    {
        var range1 = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var range2 = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;

        Assert.Equal(range1.GetHashCode(), range2.GetHashCode());
    }
}