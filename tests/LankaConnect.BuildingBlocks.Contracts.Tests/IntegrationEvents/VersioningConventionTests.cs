using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

namespace LankaConnect.BuildingBlocks.Contracts.Tests.IntegrationEvents;

public class VersioningConventionTests
{
    private sealed record UserRegisteredV1(string Email) : IntegrationEventBase, IIntegrationEventV1;
    private sealed record UserRegisteredV1Alt(string Email) : IntegrationEventBase, IIntegrationEventV1;

    [Fact]
    public void IIntegrationEventV1_is_a_marker_interface_with_no_members()
    {
        var members = typeof(IIntegrationEventV1).GetMembers();
        // Object.GetType / ToString / Equals / GetHashCode are inherited — not declared here.
        members.Where(m => m.DeclaringType == typeof(IIntegrationEventV1))
            .Should().BeEmpty();
    }

    [Fact]
    public void Events_implementing_IIntegrationEventV1_are_recognized_by_typeof_check()
    {
        var ev = new UserRegisteredV1("a@x.com");

        (ev is IIntegrationEventV1).Should().BeTrue();
    }

    [Fact]
    public void Two_distinct_V1_event_types_are_NOT_assignable_to_each_other()
    {
        // Per the convention: distinct schema = distinct CLR type. Sharing the
        // V1 marker is fine; sharing the concrete event class would cross
        // module boundaries.
        typeof(UserRegisteredV1).IsAssignableFrom(typeof(UserRegisteredV1Alt))
            .Should().BeFalse();
        typeof(UserRegisteredV1Alt).IsAssignableFrom(typeof(UserRegisteredV1))
            .Should().BeFalse();
    }

    [Fact]
    public void IIntegrationEventV1_marker_lives_in_Contracts_assembly()
    {
        typeof(IIntegrationEventV1).Assembly.GetName().Name
            .Should().Be("LankaConnect.BuildingBlocks.Contracts");
    }
}
