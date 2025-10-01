using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class BusinessHours : ValueObject
{
    public Dictionary<DayOfWeek, TimeRange?> WeeklyHours { get; private set; }

    // EF Core constructor - must be parameterless for owned entities
    private BusinessHours()
    {
        WeeklyHours = new Dictionary<DayOfWeek, TimeRange?>();
    }

    private BusinessHours(Dictionary<DayOfWeek, TimeRange?> weeklyHours)
    {
        WeeklyHours = weeklyHours;
    }

    public static Result<BusinessHours> Create(Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)> hours)
    {
        if (hours == null || hours.Count == 0)
            return Result<BusinessHours>.Failure("Business hours must be specified");

        var weeklyHours = new Dictionary<DayOfWeek, TimeRange?>();

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            if (hours.TryGetValue(day, out var dayHours))
            {
                if (dayHours.open.HasValue && dayHours.close.HasValue)
                {
                    var timeRangeResult = TimeRange.Create(dayHours.open.Value, dayHours.close.Value);
                    if (!timeRangeResult.IsSuccess)
                        return Result<BusinessHours>.Failure($"Invalid hours for {day}: {timeRangeResult.Error}");
                    
                    weeklyHours[day] = timeRangeResult.Value;
                }
                else
                {
                    weeklyHours[day] = null; // Closed on this day
                }
            }
            else
            {
                weeklyHours[day] = null; // Closed on this day
            }
        }

        return Result<BusinessHours>.Success(new BusinessHours(weeklyHours));
    }

    public static BusinessHours CreateAlwaysClosed()
    {
        var closedHours = new Dictionary<DayOfWeek, TimeRange?>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            closedHours[day] = null;
        }
        return new BusinessHours(closedHours);
    }

    public static BusinessHours Create24x7()
    {
        var alwaysOpenHours = new Dictionary<DayOfWeek, TimeRange?>();
        var allDay = TimeRange.Create(TimeOnly.MinValue, new TimeOnly(23, 59, 59)).Value;
        
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            alwaysOpenHours[day] = allDay;
        }
        return new BusinessHours(alwaysOpenHours);
    }

    public bool IsOpenAt(DateTime dateTime)
    {
        var dayOfWeek = dateTime.DayOfWeek;
        var timeOnly = TimeOnly.FromDateTime(dateTime);
        
        if (!WeeklyHours.TryGetValue(dayOfWeek, out var dayHours) || dayHours == null)
            return false;

        return dayHours.Contains(timeOnly);
    }

    public bool IsClosedOn(DayOfWeek dayOfWeek)
    {
        return !WeeklyHours.TryGetValue(dayOfWeek, out var hours) || hours == null;
    }

    public TimeRange? GetHoursFor(DayOfWeek dayOfWeek)
    {
        WeeklyHours.TryGetValue(dayOfWeek, out var hours);
        return hours;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(d => d))
        {
            yield return day;
            yield return WeeklyHours.TryGetValue(day, out var hours) ? (hours ?? (object)"closed") : (object)"closed";
        }
    }

    public override string ToString()
    {
        var hourStrings = new List<string>();
        
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var dayName = day.ToString().Substring(0, 3);
            if (WeeklyHours.TryGetValue(day, out var hours) && hours != null)
            {
                hourStrings.Add($"{dayName}: {hours}");
            }
            else
            {
                hourStrings.Add($"{dayName}: Closed");
            }
        }
        
        return string.Join(", ", hourStrings);
    }
}

public class TimeRange : ValueObject
{
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }

    // EF Core constructor - parameterless for owned entity support
    private TimeRange()
    {
    }

    private TimeRange(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<TimeRange> Create(TimeOnly start, TimeOnly end)
    {
        if (start >= end)
            return Result<TimeRange>.Failure("Start time must be before end time");

        return Result<TimeRange>.Success(new TimeRange(start, end));
    }

    public bool Contains(TimeOnly time)
    {
        return time >= Start && time <= End;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:HH:mm} - {End:HH:mm}";
}