using LankaConnect.Modules.Scheduling.Domain.ValueObjects;

namespace LankaConnect.Modules.Scheduling.Domain.Tests.ValueObjects;

public class RecurrenceRuleTests
{
    [Fact]
    public void None_IsNotRecurring()
    {
        RecurrenceRule.None.IsRecurring.Should().BeFalse();
        RecurrenceRule.None.Frequency.Should().Be(RecurrenceFrequency.None);
    }

    [Fact]
    public void Create_WithWeeklyInterval2_Succeeds()
    {
        var result = RecurrenceRule.Create(RecurrenceFrequency.Weekly, 2, new DateTime(2027, 1, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsRecurring.Should().BeTrue();
        result.Value.Interval.Should().Be(2);
    }

    [Fact]
    public void Create_WithInterval0_Fails()
    {
        var result = RecurrenceRule.Create(RecurrenceFrequency.Daily, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Recurrence.InvalidInterval");
    }

    [Fact]
    public void Create_NoneWithInterval2_Fails()
    {
        var result = RecurrenceRule.Create(RecurrenceFrequency.None, 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Recurrence.NoneWithExtras");
    }

    [Fact]
    public void Create_NoneWithUntilDate_Fails()
    {
        var result = RecurrenceRule.Create(RecurrenceFrequency.None, 1, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Recurrence.NoneWithExtras");
    }
}
