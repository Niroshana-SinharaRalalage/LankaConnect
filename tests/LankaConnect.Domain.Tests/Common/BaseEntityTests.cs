using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Common;

public class BaseEntityTests
{
    [Fact]
    public void Constructor_Should_GenerateNewId_WhenCalled()
    {
        var entity = new TestEntity();
        
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Constructor_Should_SetCreatedAtToNow_WhenCalled()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;
        
        Assert.True(entity.CreatedAt >= before && entity.CreatedAt <= after);
    }

    [Fact]
    public void UpdatedAt_Should_BeNull_WhenEntityIsNew()
    {
        var entity = new TestEntity();
        
        Assert.Null(entity.UpdatedAt);
    }

    [Fact]
    public void MarkAsUpdated_Should_SetUpdatedAtToNow()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;
        
        entity.MarkAsUpdated();
        var after = DateTime.UtcNow;
        
        Assert.NotNull(entity.UpdatedAt);
        Assert.True(entity.UpdatedAt >= before && entity.UpdatedAt <= after);
    }

    [Fact]
    public void Equals_Should_ReturnTrue_WhenEntitiesHaveSameId()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);
        
        Assert.Equal(entity1, entity2);
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenEntitiesHaveDifferentIds()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        
        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void GetHashCode_Should_ReturnSameValue_WhenEntitiesHaveSameId()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);
        
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    // Additional comprehensive tests for 100% coverage

    [Fact]
    public void Constructor_WithId_Should_UseProvidedId()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);
        
        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void DomainEvents_Should_BeEmptyInitially()
    {
        var entity = new TestEntity();
        
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_Should_AddEventToCollection()
    {
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();
        
        entity.RaiseTestDomainEvent(domainEvent);
        
        Assert.Single(entity.DomainEvents);
        Assert.Contains(domainEvent, entity.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_Should_AddMultipleEventsInOrder()
    {
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        var event3 = new TestDomainEvent();
        
        entity.RaiseTestDomainEvent(event1);
        entity.RaiseTestDomainEvent(event2);
        entity.RaiseTestDomainEvent(event3);
        
        Assert.Equal(3, entity.DomainEvents.Count);
        Assert.Equal(event1, entity.DomainEvents[0]);
        Assert.Equal(event2, entity.DomainEvents[1]);
        Assert.Equal(event3, entity.DomainEvents[2]);
    }

    [Fact]
    public void ClearDomainEvents_Should_RemoveAllEvents()
    {
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        
        entity.RaiseTestDomainEvent(event1);
        entity.RaiseTestDomainEvent(event2);
        
        Assert.Equal(2, entity.DomainEvents.Count);
        
        entity.ClearDomainEvents();
        
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void DomainEvents_Should_BeReadOnly()
    {
        var entity = new TestEntity();
        var domainEvents = entity.DomainEvents;
        
        // Should not be able to cast to mutable list
        Assert.IsNotType<List<IDomainEvent>>(domainEvents);
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>(domainEvents);
    }

    [Fact]
    public void MarkAsUpdated_Should_UpdateTimestampMultipleTimes()
    {
        var entity = new TestEntity();
        
        entity.MarkAsUpdated();
        var firstUpdate = entity.UpdatedAt;
        
        // Small delay to ensure timestamp difference
        Thread.Sleep(1);
        
        entity.MarkAsUpdated();
        var secondUpdate = entity.UpdatedAt;
        
        Assert.NotNull(firstUpdate);
        Assert.NotNull(secondUpdate);
        Assert.True(secondUpdate > firstUpdate);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Equals_Should_HandleNullComparisons_Correctly(bool useNullDirectly)
    {
        var entity = new TestEntity();
        BaseEntity? nullEntity = useNullDirectly ? null : null;
        
        Assert.False(entity.Equals(nullEntity));
        Assert.False(entity == nullEntity);
        Assert.True(entity != nullEntity);
        
        if (!useNullDirectly)
        {
            Assert.True(nullEntity == null);
            Assert.False(nullEntity != null);
        }
    }

    [Fact]
    public void Equals_Should_HandleSameReferenceComparison()
    {
        var entity = new TestEntity();
        
        Assert.True(entity.Equals(entity));
        Assert.True(ReferenceEquals(entity, entity));
        // Test operator overloads properly
        var sameEntity = entity;
        Assert.True(entity == sameEntity);
        Assert.False(entity != sameEntity);
    }

    [Fact]
    public void Equals_Should_HandleDifferentEntityTypes_WithSameId()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new DifferentTestEntity(id);
        
        // Should still be equal because BaseEntity equality is based on Id only
        Assert.Equal<BaseEntity>(entity1, entity2);
        Assert.True(entity1.Equals(entity2));
        Assert.True(entity2.Equals(entity1));
    }

    [Fact]
    public void Equals_Should_HandleNonBaseEntityObjects()
    {
        var entity = new TestEntity();
        var nonEntity = "not an entity";
        
        Assert.False(entity.Equals(nonEntity));
    }

    [Fact]
    public void GetHashCode_Should_BeConsistentAcrossMultipleCalls()
    {
        var entity = new TestEntity();
        
        var hash1 = entity.GetHashCode();
        var hash2 = entity.GetHashCode();
        var hash3 = entity.GetHashCode();
        
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void Entities_Should_WorkInHashSet_Correctly()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        
        var set = new HashSet<BaseEntity>();
        var entity1a = new TestEntity(id1);
        var entity1b = new TestEntity(id1); // Same ID
        var entity2 = new TestEntity(id2);
        
        set.Add(entity1a);
        set.Add(entity1b); // Should not add duplicate
        set.Add(entity2);
        
        Assert.Equal(2, set.Count);
        Assert.Contains(entity1a, set);
        Assert.Contains(entity1b, set); // Should find it (same ID)
        Assert.Contains(entity2, set);
    }

    [Fact]
    public void Entities_Should_WorkInDictionary_Correctly()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        
        var dict = new Dictionary<BaseEntity, string>();
        var entity1a = new TestEntity(id1);
        var entity1b = new TestEntity(id1); // Same ID
        var entity2 = new TestEntity(id2);
        
        dict[entity1a] = "value1";
        dict[entity1b] = "value2"; // Should overwrite
        dict[entity2] = "value3";
        
        Assert.Equal(2, dict.Count);
        Assert.Equal("value2", dict[entity1a]); // Updated by entity1b
        Assert.Equal("value3", dict[entity2]);
    }

    [Fact]
    public void DomainEvents_Should_HandleMultipleEventsOfSameType()
    {
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        
        entity.RaiseTestDomainEvent(event1);
        entity.RaiseTestDomainEvent(event2);
        
        Assert.Equal(2, entity.DomainEvents.Count);
        Assert.NotSame(event1, event2); // Different instances
    }

    [Fact]
    public void DomainEvents_Should_PreserveDifferentEventTypes()
    {
        var entity = new TestEntity();
        var testEvent = new TestDomainEvent();
        var anotherEvent = new AnotherTestDomainEvent();
        
        entity.RaiseTestDomainEvent(testEvent);
        entity.RaiseAnotherTestDomainEvent(anotherEvent);
        
        Assert.Equal(2, entity.DomainEvents.Count);
        Assert.IsType<TestDomainEvent>(entity.DomainEvents[0]);
        Assert.IsType<AnotherTestDomainEvent>(entity.DomainEvents[1]);
    }

    [Fact]
    public void Entity_Should_HandleLargeNumberOfDomainEvents()
    {
        var entity = new TestEntity();
        
        // Add many events
        for (int i = 0; i < 1000; i++)
        {
            entity.RaiseTestDomainEvent(new TestDomainEvent());
        }
        
        Assert.Equal(1000, entity.DomainEvents.Count);
        
        // Clear should handle large collections efficiently
        entity.ClearDomainEvents();
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void BaseEntity_Should_HandleTimeZoneConsistency()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        
        // Both should use UTC
        Assert.Equal(DateTimeKind.Utc, entity1.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, entity2.CreatedAt.Kind);
        
        entity1.MarkAsUpdated();
        Assert.NotNull(entity1.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, entity1.UpdatedAt.Value.Kind);
    }

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Entity_Should_HandleSpecificGuids(string guidString)
    {
        var id = Guid.Parse(guidString);
        var entity = new TestEntity(id);
        
        Assert.Equal(id, entity.Id);
        
        // Should work with equality comparisons
        var entity2 = new TestEntity(id);
        Assert.Equal(entity, entity2);
    }

    [Fact]
    public async Task Entity_Should_BeThreadSafeForDomainEvents()
    {
        var entity = new TestEntity();
        var tasks = new List<Task>();
        
        // Add events from multiple threads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    entity.RaiseTestDomainEvent(new TestDomainEvent());
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Should have all events (though order may vary)
        Assert.Equal(100, entity.DomainEvents.Count);
    }

    // Test helper classes and events

    private class TestEntity : BaseEntity
    {
        public TestEntity() : base() { }
        public TestEntity(Guid id) : base(id) { }
        
        // Expose protected method for testing
        public void RaiseTestDomainEvent(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
        
        public void RaiseAnotherTestDomainEvent(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }

    private class DifferentTestEntity : BaseEntity
    {
        public DifferentTestEntity(Guid id) : base(id) { }
    }

    private class TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }

    private class AnotherTestDomainEvent : IDomainEvent
    {
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }
}