using Microsoft.EntityFrameworkCore;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

public sealed class BaseDbContextSoftDeleteTests
{
    [Fact]
    public async Task Delete_OnSoftDeletable_FlipsToModifiedAndStampsIsDeleted()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new SoftDeletableEntity { Name = "to-delete" };
        db.SoftDeletable.Add(entity);
        await db.SaveChangesAsync();
        var id = entity.Id;

        db.SoftDeletable.Remove(entity);
        await db.SaveChangesAsync();

        // Re-query INCLUDING soft-deleted rows
        var reloaded = await db.SoftDeletable
            .IgnoreQueryFilters()
            .SingleAsync(e => e.Id == id);

        reloaded.IsDeleted.Should().BeTrue();
        reloaded.DeletedAt.Should().NotBeNull();
        reloaded.DeletedBy.Should().Be("user-1");
    }

    [Fact]
    public async Task Query_DefaultFilter_ExcludesSoftDeletedRows()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var live = new SoftDeletableEntity { Name = "alive" };
        var dead = new SoftDeletableEntity { Name = "dead" };
        db.SoftDeletable.AddRange(live, dead);
        await db.SaveChangesAsync();
        db.SoftDeletable.Remove(dead);
        await db.SaveChangesAsync();

        var results = await db.SoftDeletable.ToListAsync();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("alive");
    }

    [Fact]
    public async Task Query_IgnoreQueryFilters_IncludesSoftDeletedRows()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var live = new SoftDeletableEntity { Name = "alive" };
        var dead = new SoftDeletableEntity { Name = "dead" };
        db.SoftDeletable.AddRange(live, dead);
        await db.SaveChangesAsync();
        db.SoftDeletable.Remove(dead);
        await db.SaveChangesAsync();

        var allIncludingDeleted = await db.SoftDeletable.IgnoreQueryFilters().ToListAsync();

        allIncludingDeleted.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_OnAuditableAndSoftDeletable_StampsBothAuditAndSoftDeleteFields()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new AuditableAndSoftDeletableEntity { Name = "both" };
        db.Both.Add(entity);
        await db.SaveChangesAsync();

        db.Both.Remove(entity);
        await db.SaveChangesAsync();

        var reloaded = await db.Both
            .IgnoreQueryFilters()
            .SingleAsync(e => e.Id == entity.Id);

        // Soft delete fields
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.DeletedBy.Should().Be("user-1");
        reloaded.DeletedAt.Should().NotBeNull();
        // Soft-delete is internally a Modified state, so UpdatedAt/UpdatedBy ALSO get stamped
        reloaded.UpdatedAt.Should().NotBeNull();
        reloaded.UpdatedBy.Should().Be("user-1");
        // CreatedAt + CreatedBy still preserved from insert
        reloaded.CreatedBy.Should().Be("user-1");
    }

    [Fact]
    public async Task Delete_OnPlainEntity_HardDeletesAsExpected()
    {
        // No ISoftDeletable marker — EF Core should physically remove the row.
        var (db, _) = TestDbContextBuilder.Build(actorId: "user-1");
        var entity = new PlainEntity { Name = "plain" };
        db.Plain.Add(entity);
        await db.SaveChangesAsync();
        var id = entity.Id;

        db.Plain.Remove(entity);
        await db.SaveChangesAsync();

        var found = await db.Plain.FindAsync(id);
        found.Should().BeNull();
    }
}
