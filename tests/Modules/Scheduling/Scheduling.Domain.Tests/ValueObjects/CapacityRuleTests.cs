using LankaConnect.Modules.Scheduling.Domain.ValueObjects;

namespace LankaConnect.Modules.Scheduling.Domain.Tests.ValueObjects;

public class CapacityRuleTests
{
    [Fact]
    public void Create_WithPositiveTotal_Succeeds()
    {
        var result = CapacityRule.Create(100);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositive_Fails(int total)
    {
        var result = CapacityRule.Create(total);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Capacity.NotPositive");
    }

    [Theory]
    [InlineData(0, 5, true)]
    [InlineData(95, 5, true)]
    [InlineData(96, 5, false)]
    [InlineData(100, 1, false)]
    public void HasRoomFor_ChecksAvailability(int reserved, int additional, bool expected)
    {
        var rule = CapacityRule.Create(100).Value;

        rule.HasRoomFor(reserved, additional).Should().Be(expected);
    }

    [Fact]
    public void IsFull_AtTotal_True()
    {
        var rule = CapacityRule.Create(10).Value;

        rule.IsFull(10).Should().BeTrue();
        rule.IsFull(11).Should().BeTrue();
        rule.IsFull(9).Should().BeFalse();
    }

    [Fact]
    public void Remaining_ClampsToZero()
    {
        var rule = CapacityRule.Create(10).Value;

        rule.Remaining(8).Should().Be(2);
        rule.Remaining(10).Should().Be(0);
        rule.Remaining(15).Should().Be(0);
    }
}
