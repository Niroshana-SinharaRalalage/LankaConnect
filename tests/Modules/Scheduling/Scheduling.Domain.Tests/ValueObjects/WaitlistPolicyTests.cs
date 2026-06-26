using LankaConnect.Modules.Scheduling.Domain.ValueObjects;

namespace LankaConnect.Modules.Scheduling.Domain.Tests.ValueObjects;

public class WaitlistPolicyTests
{
    [Fact]
    public void NotAccepted_RejectsAllEntries()
    {
        WaitlistPolicy.NotAccepted.AcceptsNewEntries(0).Should().BeFalse();
        WaitlistPolicy.NotAccepted.AcceptsNewEntries(100).Should().BeFalse();
    }

    [Fact]
    public void Accepted_Unbounded_AlwaysAccepts()
    {
        var policy = WaitlistPolicy.Accepted(maxSize: 0).Value;

        policy.AcceptsNewEntries(0).Should().BeTrue();
        policy.AcceptsNewEntries(int.MaxValue - 1).Should().BeTrue();
    }

    [Fact]
    public void Accepted_Bounded_RespectsLimit()
    {
        var policy = WaitlistPolicy.Accepted(maxSize: 3).Value;

        policy.AcceptsNewEntries(0).Should().BeTrue();
        policy.AcceptsNewEntries(2).Should().BeTrue();
        policy.AcceptsNewEntries(3).Should().BeFalse();
        policy.AcceptsNewEntries(4).Should().BeFalse();
    }

    [Fact]
    public void Accepted_NegativeMaxSize_Fails()
    {
        var result = WaitlistPolicy.Accepted(maxSize: -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Waitlist.NegativeMaxSize");
    }
}
