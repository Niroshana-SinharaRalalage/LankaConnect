using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing temple schedule integration for cultural events
/// </summary>
public sealed class TempleScheduleIntegration : ValueObject
{
    public string TempleId { get; }
    public string TempleName { get; }
    public DiasporaLocation Location { get; }
    public string ScheduleDetails { get; }
    public DateTime EventTime { get; }

    private TempleScheduleIntegration(
        string templeId,
        string templeName,
        DiasporaLocation location,
        string scheduleDetails,
        DateTime eventTime)
    {
        TempleId = templeId;
        TempleName = templeName;
        Location = location;
        ScheduleDetails = scheduleDetails;
        EventTime = eventTime;
    }

    public static Result<TempleScheduleIntegration> Create(
        string templeId,
        string templeName,
        DiasporaLocation location,
        string scheduleDetails,
        DateTime eventTime)
    {
        if (string.IsNullOrWhiteSpace(templeId))
            return Result<TempleScheduleIntegration>.Failure("Temple ID cannot be empty");

        if (string.IsNullOrWhiteSpace(templeName))
            return Result<TempleScheduleIntegration>.Failure("Temple name cannot be empty");

        return Result<TempleScheduleIntegration>.Success(
            new TempleScheduleIntegration(templeId, templeName, location, scheduleDetails, eventTime));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TempleId;
        yield return TempleName;
        yield return Location;
        yield return ScheduleDetails;
        yield return EventTime;
    }
}

/// <summary>
/// Value object representing a family identifier for coordinated cultural calendars
/// </summary>
public sealed class FamilyId : ValueObject
{
    public Guid Value { get; }

    private FamilyId(Guid value)
    {
        Value = value;
    }

    public static Result<FamilyId> Create(Guid value)
    {
        if (value == Guid.Empty)
            return Result<FamilyId>.Failure("Family ID cannot be empty");

        return Result<FamilyId>.Success(new FamilyId(value));
    }

    public static FamilyId NewId()
    {
        return new FamilyId(Guid.NewGuid());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}