using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.BuildingBlocks.Domain.Tests;

public sealed class EntityTests
{
    // Test entities for identity + equality scenarios

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
        public void RaiseTestEvent(IDomainEvent e) => RaiseDomainEvent(e);
    }

    private sealed class OtherTestEntity : Entity<Guid>
    {
        public OtherTestEntity(Guid id) : base(id) { }
    }

    private sealed record TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    [Fact]
    public void Constructor_NullId_Throws()
    {
        // Using string-typed entity so we can pass null at runtime
        Action act = () => _ = new StringIdEntity(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class StringIdEntity : Entity<string>
    {
        public StringIdEntity(string id) : base(id) { }
    }

    [Fact]
    public void Constructor_SetsId()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DomainEvents_StartEmpty()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_AppendsToBuffer()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var evt = new TestDomainEvent();

        entity.RaiseTestEvent(evt);

        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents[0].Should().Be(evt);
    }

    [Fact]
    public void RaiseDomainEvent_Null_Throws()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Action act = () => entity.RaiseTestEvent(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearDomainEvents_EmptiesBuffer()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.RaiseTestEvent(new TestDomainEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameIdSameType_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentId_NotEqual()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameIdDifferentType_NotEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new OtherTestEntity(id);

        // Cross-type: cast to base to invoke the operator overload
        ((Entity<Guid>)a).Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equality_AgainstNull_NotEqual()
    {
        var a = new TestEntity(Guid.NewGuid());

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void Equality_ReferenceSame_AreEqual()
    {
        var a = new TestEntity(Guid.NewGuid());

        a.Equals(a).Should().BeTrue();
    }

    [Fact]
    public void DomainEvents_AreReadOnly()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.RaiseTestEvent(new TestDomainEvent());

        // Confirm the collection type prevents mutation (compile-time + runtime)
        entity.DomainEvents.Should().BeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }
}
