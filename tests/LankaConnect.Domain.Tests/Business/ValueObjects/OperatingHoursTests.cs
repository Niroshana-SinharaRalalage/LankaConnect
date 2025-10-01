using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class OperatingHoursTests
{
    [Fact]
    public void Create_WithValidTimeSpans_ShouldReturnSuccess()
    {
        var dayOfWeek = DayOfWeek.Monday;
        var openTime = TimeSpan.FromHours(9);
        var closeTime = TimeSpan.FromHours(17);

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsSuccess);
        var hours = result.Value;
        Assert.Equal(dayOfWeek, hours.DayOfWeek);
        Assert.Equal(openTime, hours.OpenTime);
        Assert.Equal(closeTime, hours.CloseTime);
        Assert.False(hours.IsClosed);
    }

    [Fact]
    public void Create_WithValidTimeStrings_ShouldReturnSuccess()
    {
        var dayOfWeek = DayOfWeek.Tuesday;
        var openTime = "09:00";
        var closeTime = "17:30";

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsSuccess);
        var hours = result.Value;
        Assert.Equal(dayOfWeek, hours.DayOfWeek);
        Assert.Equal(TimeSpan.Parse(openTime), hours.OpenTime);
        Assert.Equal(TimeSpan.Parse(closeTime), hours.CloseTime);
        Assert.False(hours.IsClosed);
    }

    [Fact]
    public void CreateClosed_ShouldReturnClosedHours()
    {
        var dayOfWeek = DayOfWeek.Sunday;

        var result = OperatingHours.CreateClosed(dayOfWeek);

        Assert.True(result.IsSuccess);
        var hours = result.Value;
        Assert.Equal(dayOfWeek, hours.DayOfWeek);
        Assert.Null(hours.OpenTime);
        Assert.Null(hours.CloseTime);
        Assert.True(hours.IsClosed);
    }

    [Fact]
    public void Create_WithOpenTimeEqualToCloseTime_ShouldReturnFailure()
    {
        var dayOfWeek = DayOfWeek.Wednesday;
        var time = TimeSpan.FromHours(12);

        var result = OperatingHours.Create(dayOfWeek, time, time);

        Assert.True(result.IsFailure);
        Assert.Contains("Opening time must be before closing time", result.Errors);
    }

    [Fact]
    public void Create_WithOpenTimeAfterCloseTime_ShouldReturnFailure()
    {
        var dayOfWeek = DayOfWeek.Thursday;
        var openTime = TimeSpan.FromHours(18);
        var closeTime = TimeSpan.FromHours(12);

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Opening time must be before closing time", result.Errors);
    }

    [Fact]
    public void Create_WithNegativeOpenTime_ShouldReturnFailure()
    {
        var dayOfWeek = DayOfWeek.Friday;
        var openTime = TimeSpan.FromHours(-1);
        var closeTime = TimeSpan.FromHours(17);

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Opening time must be within a 24-hour period", result.Errors);
    }

    [Fact]
    public void Create_WithOpenTimeBeyond24Hours_ShouldReturnFailure()
    {
        var dayOfWeek = DayOfWeek.Saturday;
        var openTime = TimeSpan.FromHours(25);
        var closeTime = TimeSpan.FromHours(26);

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Opening time must be within a 24-hour period", result.Errors);
    }

    [Fact]
    public void Create_WithCloseTimeAtZero_ShouldReturnFailure()
    {
        var dayOfWeek = DayOfWeek.Monday;
        var openTime = TimeSpan.FromHours(9);
        var closeTime = TimeSpan.Zero;

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Closing time must be within a 24-hour period", result.Errors);
    }

    [Fact]
    public void Create_WithCloseTimeBeyond24Hours_ShouldReturnFailure()
    {
        var dayOfWeek = DayOfWeek.Tuesday;
        var openTime = TimeSpan.FromHours(9);
        var closeTime = TimeSpan.FromDays(1).Add(TimeSpan.FromHours(1));

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Closing time must be within a 24-hour period", result.Errors);
    }

    [Fact]
    public void Create_WithMidnightCloseTime_ShouldReturnSuccess()
    {
        var dayOfWeek = DayOfWeek.Wednesday;
        var openTime = TimeSpan.FromHours(9);
        var closeTime = TimeSpan.FromDays(1); // 24:00:00 (midnight next day)

        var result = OperatingHours.Create(dayOfWeek, openTime, closeTime);

        Assert.True(result.IsSuccess);
        var hours = result.Value;
        Assert.Equal(closeTime, hours.CloseTime);
    }

    [Theory]
    [InlineData("invalid-time")]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("")]
    public void Create_WithInvalidOpenTimeString_ShouldReturnFailure(string invalidTime)
    {
        var result = OperatingHours.Create(DayOfWeek.Monday, invalidTime, "17:00");

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid opening time format", result.Errors);
    }

    [Theory]
    [InlineData("invalid-time")]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("")]
    public void Create_WithInvalidCloseTimeString_ShouldReturnFailure(string invalidTime)
    {
        var result = OperatingHours.Create(DayOfWeek.Monday, "09:00", invalidTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid closing time format", result.Errors);
    }

    [Theory]
    [InlineData(8, 0, false)]   // 8:00 AM - before opening, should be closed
    [InlineData(9, 0, true)]    // 9:00 AM - opening time, should be open
    [InlineData(12, 30, true)]  // 12:30 PM - mid-day, should be open
    [InlineData(17, 0, true)]   // 5:00 PM - closing time, should be open
    [InlineData(18, 0, false)]  // 6:00 PM - after closing, should be closed
    [InlineData(7, 0, false)]   // 7:00 AM - before opening, should be closed
    public void IsOpenAt_WithVariousTimes_ShouldReturnExpectedResult(int hour, int minute, bool expectedOpen)
    {
        var hours = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;
        var testTime = TimeSpan.FromHours(hour).Add(TimeSpan.FromMinutes(minute));

        var isOpen = hours.IsOpenAt(testTime);

        Assert.Equal(expectedOpen, isOpen);
    }

    [Fact]
    public void IsOpenAt_WhenClosed_ShouldReturnFalse()
    {
        var hours = OperatingHours.CreateClosed(DayOfWeek.Sunday).Value;
        var testTime = TimeSpan.FromHours(12);

        var isOpen = hours.IsOpenAt(testTime);

        Assert.False(isOpen);
    }

    [Fact]
    public void IsCurrentlyOpen_WhenCorrectDay_ShouldCheckCurrentTime()
    {
        var currentDay = DateTime.Now.DayOfWeek;
        var currentTime = DateTime.Now.TimeOfDay;
        
        // Create hours that should be open now (extend range to ensure it covers current time)
        var openTime = currentTime.Subtract(TimeSpan.FromHours(1));
        var closeTime = currentTime.Add(TimeSpan.FromHours(1));
        
        // Ensure times are within valid range
        if (openTime < TimeSpan.Zero) openTime = TimeSpan.Zero;
        if (closeTime > TimeSpan.FromDays(1)) closeTime = TimeSpan.FromDays(1);

        var hours = OperatingHours.Create(currentDay, openTime, closeTime).Value;

        var isCurrentlyOpen = hours.IsCurrentlyOpen();

        Assert.True(isCurrentlyOpen);
    }

    [Fact]
    public void IsCurrentlyOpen_WhenDifferentDay_ShouldReturnFalse()
    {
        var differentDay = DateTime.Now.DayOfWeek == DayOfWeek.Monday ? DayOfWeek.Tuesday : DayOfWeek.Monday;
        var hours = OperatingHours.Create(differentDay, TimeSpan.FromHours(0), TimeSpan.FromDays(1)).Value;

        var isCurrentlyOpen = hours.IsCurrentlyOpen();

        Assert.False(isCurrentlyOpen);
    }

    [Fact]
    public void IsCurrentlyOpen_WhenClosed_ShouldReturnFalse()
    {
        var currentDay = DateTime.Now.DayOfWeek;
        var hours = OperatingHours.CreateClosed(currentDay).Value;

        var isCurrentlyOpen = hours.IsCurrentlyOpen();

        Assert.False(isCurrentlyOpen);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var hours1 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;
        var hours2 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;

        Assert.Equal(hours1, hours2);
    }

    [Fact]
    public void Equality_WithDifferentDays_ShouldNotBeEqual()
    {
        var hours1 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;
        var hours2 = OperatingHours.Create(DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;

        Assert.NotEqual(hours1, hours2);
    }

    [Fact]
    public void Equality_WithDifferentTimes_ShouldNotBeEqual()
    {
        var hours1 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;
        var hours2 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(10), TimeSpan.FromHours(18)).Value;

        Assert.NotEqual(hours1, hours2);
    }

    [Fact]
    public void Equality_BothClosed_ShouldBeEqual()
    {
        var hours1 = OperatingHours.CreateClosed(DayOfWeek.Sunday).Value;
        var hours2 = OperatingHours.CreateClosed(DayOfWeek.Sunday).Value;

        Assert.Equal(hours1, hours2);
    }

    [Fact]
    public void ToString_WithOpenHours_ShouldFormatCorrectly()
    {
        var hours = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;

        var result = hours.ToString();

        Assert.Contains("Monday", result);
        Assert.Contains("09:00", result);
        Assert.Contains("17:00", result);
    }

    [Fact]
    public void ToString_WhenClosed_ShouldShowClosed()
    {
        var hours = OperatingHours.CreateClosed(DayOfWeek.Sunday).Value;

        var result = hours.ToString();

        Assert.Contains("Sunday", result);
        Assert.Contains("Closed", result);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var hours1 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;
        var hours2 = OperatingHours.Create(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17)).Value;

        Assert.Equal(hours1.GetHashCode(), hours2.GetHashCode());
    }

    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void Create_WithAllDaysOfWeek_ShouldReturnSuccess(DayOfWeek dayOfWeek)
    {
        var result = OperatingHours.Create(dayOfWeek, TimeSpan.FromHours(8), TimeSpan.FromHours(20));

        Assert.True(result.IsSuccess);
        Assert.Equal(dayOfWeek, result.Value.DayOfWeek);
    }
}