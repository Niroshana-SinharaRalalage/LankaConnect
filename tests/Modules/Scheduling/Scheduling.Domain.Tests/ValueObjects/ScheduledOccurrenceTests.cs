using LankaConnect.Modules.Scheduling.Domain.ValueObjects;

namespace LankaConnect.Modules.Scheduling.Domain.Tests.ValueObjects;

public class ScheduledOccurrenceTests
{
    [Fact]
    public void Create_WithStartBeforeEnd_Succeeds()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(2);

        var result = ScheduledOccurrence.Create(start, end, "America/New_York");

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(start);
        result.Value.EndDate.Should().Be(end);
        result.Value.TimeZoneId.Should().Be("America/New_York");
    }

    [Fact]
    public void Create_WithEndBeforeStart_Fails()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(-1);

        var result = ScheduledOccurrence.Create(start, end);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Occurrence.InvalidDateRange");
    }

    [Fact]
    public void Create_WithBothDatesNull_Succeeds()
    {
        var result = ScheduledOccurrence.Create(null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasCommittedSchedule.Should().BeFalse();
    }

    [Fact]
    public void Tbd_HasNoDates()
    {
        var tbd = ScheduledOccurrence.Tbd("America/New_York");

        tbd.StartDate.Should().BeNull();
        tbd.EndDate.Should().BeNull();
        tbd.HasCommittedSchedule.Should().BeFalse();
    }

    [Fact]
    public void IsUpcoming_WhenStartInFuture_True()
    {
        var now = DateTime.UtcNow;
        var occ = ScheduledOccurrence.Create(now.AddHours(1), now.AddHours(2)).Value;

        occ.IsUpcoming(now).Should().BeTrue();
    }

    [Fact]
    public void IsPast_WhenEndInPast_True()
    {
        var now = DateTime.UtcNow;
        var occ = ScheduledOccurrence.Create(now.AddHours(-2), now.AddHours(-1)).Value;

        occ.IsPast(now).Should().BeTrue();
    }

    [Fact]
    public void Equality_BySameDatesAndTimezone()
    {
        var start = new DateTime(2026, 6, 26, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(2);

        var a = ScheduledOccurrence.Create(start, end, "UTC").Value;
        var b = ScheduledOccurrence.Create(start, end, "UTC").Value;

        a.Should().Be(b);
    }
}
