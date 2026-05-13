namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

public sealed class BaseDbContextAuditTests
{
    [Fact]
    public async Task SaveChanges_OnAdd_StampsCreatedAtAndCreatedBy()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new AuditableEntity { Name = "first" };

        db.Auditable.Add(entity);
        await db.SaveChangesAsync();

        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.CreatedBy.Should().Be("user-1");
        entity.UpdatedAt.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task SaveChanges_OnAdd_AnonymousActor_StampsNullCreatedBy()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: null);
        var entity = new AuditableEntity { Name = "anon" };

        db.Auditable.Add(entity);
        await db.SaveChangesAsync();

        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.CreatedBy.Should().BeNull();
    }

    [Fact]
    public async Task SaveChanges_OnUpdate_StampsUpdatedAtAndPreservesCreatedAt()
    {
        var (db, actor) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new AuditableEntity { Name = "v1" };
        db.Auditable.Add(entity);
        await db.SaveChangesAsync();
        var originalCreatedAt = entity.CreatedAt;
        var originalCreatedBy = entity.CreatedBy;

        // Wait a moment then update with a different actor
        await Task.Delay(20);
        actor.ActorId = "user-2";
        entity.Name = "v2";
        await db.SaveChangesAsync();

        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeAfter(originalCreatedAt);
        entity.UpdatedBy.Should().Be("user-2");
        // CreatedAt + CreatedBy MUST NOT change on update
        entity.CreatedAt.Should().Be(originalCreatedAt);
        entity.CreatedBy.Should().Be(originalCreatedBy);
    }

    [Fact]
    public async Task SaveChanges_PlainEntity_NoAuditFieldsTouched()
    {
        // A non-IAuditable entity must pass through SaveChanges without any audit interference.
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new PlainEntity { Name = "plain" };

        db.Plain.Add(entity);

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void SaveChanges_Sync_AlsoStampsAuditFields()
    {
        // Confirm the override on the sync SaveChanges path mirrors the async path.
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new AuditableEntity { Name = "sync" };

        db.Auditable.Add(entity);
        db.SaveChanges();

        entity.CreatedBy.Should().Be("user-1");
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
