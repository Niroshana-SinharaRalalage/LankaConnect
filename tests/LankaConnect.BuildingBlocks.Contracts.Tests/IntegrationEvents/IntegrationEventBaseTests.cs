using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

namespace LankaConnect.BuildingBlocks.Contracts.Tests.IntegrationEvents;

public class IntegrationEventBaseTests
{
    // Fixtures — concrete events derived from IntegrationEventBase only for tests.
    private sealed record TestUserRegisteredV1(string Email) : IntegrationEventBase, IIntegrationEventV1;
    private sealed record TestUserRegisteredV2(string Email, Guid TenantId) : IntegrationEventBase;
    private sealed record VersionedEvent : IntegrationEventBase
    {
        public override int Version => 7;
    }

    [Fact]
    public void EventId_default_is_unique_per_instance()
    {
        var a = new TestUserRegisteredV1("a@x.com");
        var b = new TestUserRegisteredV1("b@x.com");

        a.EventId.Should().NotBe(Guid.Empty);
        b.EventId.Should().NotBe(Guid.Empty);
        a.EventId.Should().NotBe(b.EventId);
    }

    [Fact]
    public void EventId_can_be_overridden_via_init()
    {
        var explicitId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var ev = new TestUserRegisteredV1("a@x.com") { EventId = explicitId };

        ev.EventId.Should().Be(explicitId);
    }

    [Fact]
    public void OccurredOnUtc_default_is_close_to_now()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-2);
        var ev = new TestUserRegisteredV1("a@x.com");
        var after = DateTimeOffset.UtcNow.AddSeconds(2);

        ev.OccurredOnUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void EventType_returns_assembly_qualified_name_of_concrete_type()
    {
        var ev = new TestUserRegisteredV1("a@x.com");

        ev.EventType.Should().Contain("TestUserRegisteredV1");
        ev.EventType.Should().Contain("LankaConnect.BuildingBlocks.Contracts.Tests");
        ev.EventType.Should().Contain("Version=");
    }

    [Fact]
    public void Version_defaults_to_1_when_concrete_type_does_not_override()
    {
        var ev = new TestUserRegisteredV2("a@x.com", Guid.NewGuid());

        ev.Version.Should().Be(1);
    }

    [Fact]
    public void Version_can_be_overridden_by_concrete_type()
    {
        var ev = new VersionedEvent();

        ev.Version.Should().Be(7);
    }

    [Fact]
    public void Records_have_value_equality_for_same_payload_when_metadata_matches()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var a = new TestUserRegisteredV1("a@x.com") { EventId = id, OccurredOnUtc = occurredAt };
        var b = new TestUserRegisteredV1("a@x.com") { EventId = id, OccurredOnUtc = occurredAt };

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Records_are_distinct_when_payload_differs()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var a = new TestUserRegisteredV1("a@x.com") { EventId = id, OccurredOnUtc = occurredAt };
        var b = new TestUserRegisteredV1("b@x.com") { EventId = id, OccurredOnUtc = occurredAt };

        a.Should().NotBe(b);
    }
}
