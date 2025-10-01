using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class OperatingHours : ValueObject
{
    public DayOfWeek DayOfWeek { get; }
    public TimeSpan? OpenTime { get; }
    public TimeSpan? CloseTime { get; }
    public bool IsClosed { get; }

    private OperatingHours(DayOfWeek dayOfWeek, TimeSpan? openTime, TimeSpan? closeTime, bool isClosed)
    {
        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;
    }

    public static Result<OperatingHours> CreateClosed(DayOfWeek dayOfWeek)
    {
        return Result<OperatingHours>.Success(new OperatingHours(dayOfWeek, null, null, true));
    }

    public static Result<OperatingHours> Create(DayOfWeek dayOfWeek, TimeSpan openTime, TimeSpan closeTime)
    {
        // Validate individual time ranges first
        if (openTime < TimeSpan.Zero || openTime >= TimeSpan.FromDays(1))
            return Result<OperatingHours>.Failure("Opening time must be within a 24-hour period");

        if (closeTime <= TimeSpan.Zero || closeTime > TimeSpan.FromDays(1))
            return Result<OperatingHours>.Failure("Closing time must be within a 24-hour period");

        // Then validate the relationship between open and close times
        if (openTime >= closeTime)
            return Result<OperatingHours>.Failure("Opening time must be before closing time");

        return Result<OperatingHours>.Success(new OperatingHours(dayOfWeek, openTime, closeTime, false));
    }

    public static Result<OperatingHours> Create(DayOfWeek dayOfWeek, string openTime, string closeTime)
    {
        if (!TimeSpan.TryParse(openTime, out var parsedOpenTime))
            return Result<OperatingHours>.Failure("Invalid opening time format");

        if (!TimeSpan.TryParse(closeTime, out var parsedCloseTime))
            return Result<OperatingHours>.Failure("Invalid closing time format");

        return Create(dayOfWeek, parsedOpenTime, parsedCloseTime);
    }

    public bool IsOpenAt(TimeSpan time)
    {
        if (IsClosed) return false;
        if (OpenTime == null || CloseTime == null) return false;

        return time >= OpenTime && time <= CloseTime;
    }

    public bool IsCurrentlyOpen()
    {
        if (DateTime.Now.DayOfWeek != DayOfWeek) return false;
        return IsOpenAt(DateTime.Now.TimeOfDay);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return IsClosed;
        
        if (OpenTime.HasValue)
            yield return OpenTime.Value;
        if (CloseTime.HasValue)
            yield return CloseTime.Value;
    }

    public override string ToString()
    {
        if (IsClosed)
            return $"{DayOfWeek}: Closed";

        return $"{DayOfWeek}: {OpenTime:hh\\:mm} - {CloseTime:hh\\:mm}";
    }
}