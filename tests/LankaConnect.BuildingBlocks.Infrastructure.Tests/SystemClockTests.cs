using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Infrastructure;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

/// <summary>W1G verification: SystemClock returns a sensible UtcNow.</summary>
public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsCurrentUtcTime_WithinSeconds()
    {
        IClock clock = SystemClock.Instance;
        var before = DateTime.UtcNow;

        var actual = clock.UtcNow;

        var after = DateTime.UtcNow;
        actual.Kind.Should().Be(DateTimeKind.Utc, "all clocks must be UTC per ADR convention");
        actual.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Instance_IsSingleton()
    {
        SystemClock.Instance.Should().BeSameAs(SystemClock.Instance);
    }
}
