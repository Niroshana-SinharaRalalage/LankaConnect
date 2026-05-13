using System.Linq.Expressions;
using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Cross-cutting <see cref="DbContext"/> base for module DbContexts. Provides:
/// <list type="bullet">
///   <item>Automatic audit-field population for <see cref="IAuditable"/> entities</item>
///   <item>Soft-delete interception + global query filter for <see cref="ISoftDeletable"/> entities</item>
///   <item>JSONB ValueComparer convention helper for mutable collection-backed properties</item>
/// </list>
/// Modules derive from this and add their own <see cref="DbSet{TEntity}"/>s.
/// </summary>
/// <remarks>
/// <para>
/// <b>Audit semantics</b>: <see cref="IAuditable.CreatedAt"/> + <see cref="IAuditable.CreatedBy"/>
/// are set on <c>EntityState.Added</c>; <see cref="IAuditable.UpdatedAt"/> +
/// <see cref="IAuditable.UpdatedBy"/> are set on <c>EntityState.Modified</c>.
/// Actor id comes from injected <see cref="ICurrentActor"/> (null for
/// anonymous / system operations).
/// </para>
/// <para>
/// <b>Soft-delete semantics</b>: when an <see cref="ISoftDeletable"/> entity is
/// in <c>EntityState.Deleted</c>, SaveChanges flips it to Modified, sets
/// <see cref="ISoftDeletable.IsDeleted"/> = true, and populates DeletedAt/DeletedBy.
/// A global query filter (<c>e => !e.IsDeleted</c>) is registered in
/// <see cref="OnModelCreating"/>; use <c>IgnoreQueryFilters()</c> on a query
/// to include deleted rows (admin recovery flows).
/// </para>
/// <para>
/// <b>JSONB ValueComparer</b>: per MEMORY.md Phase 6A.129, EF Core's default
/// change-detection snapshot shares the list instance with the current value
/// for mutable collection-backed properties stored as JSONB — in-place mutations
/// (<c>Clear()</c> + <c>AddRange()</c>) leave the snapshot pointing at the SAME
/// list, so the column appears unchanged and is omitted from UPDATE SQL.
/// The fix is a custom <see cref="ValueComparer{T}"/> with a deep-copy snapshot.
/// <see cref="ApplyJsonbValueComparer{T}"/> below is the canonical helper.
/// </para>
/// </remarks>
public abstract class BaseDbContext : DbContext
{
    private readonly ICurrentActor _currentActor;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructs a base context with audit + soft-delete + JSONB conventions.
    /// </summary>
    protected BaseDbContext(
        DbContextOptions options,
        ICurrentActor currentActor,
        ILogger logger)
        : base(options)
    {
        _currentActor = currentActor ?? throw new ArgumentNullException(nameof(currentActor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    /// <summary>
    /// Stamps audit fields on auditable entries and converts hard-deletes on
    /// soft-deletable entries into Modified + IsDeleted=true.
    /// </summary>
    /// <remarks>
    /// Order matters: soft-delete FIRST (flips state to Modified), then audit
    /// (sees the Modified state and stamps UpdatedAt/UpdatedBy). If we audit
    /// first, soft-deleted entities would skip the Modified branch because
    /// their state is still Deleted, leaving UpdatedAt/UpdatedBy null.
    /// </remarks>
    private void ApplyAuditAndSoftDelete()
    {
        var now = DateTime.UtcNow;
        var actorId = _currentActor.ActorId;

        // Pass 1: soft-delete conversion (Deleted → Modified + IsDeleted=true)
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                _logger.LogDebug(
                    "BaseDbContext: soft-deleting {EntityType} (actor {ActorId})",
                    entry.Entity.GetType().Name,
                    actorId ?? "(anonymous)");

                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = actorId;
                entry.State = EntityState.Modified;
            }
        }

        // Pass 2: audit stamping (now sees the Modified state from pass 1 too)
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = actorId;
                        // Leave UpdatedAt/UpdatedBy null on insert — no update has happened yet.
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        auditable.UpdatedBy = actorId;
                        // Don't overwrite CreatedAt/By — they remain immutable after insert.
                        entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Applies the cross-cutting model conventions — soft-delete global query
    /// filter for every <see cref="ISoftDeletable"/> entity. Module overrides
    /// MUST call <c>base.OnModelCreating(modelBuilder)</c> first.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                ApplySoftDeleteFilter(modelBuilder, entityType.ClrType);
            }
        }
    }

    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder, Type clrType)
    {
        // e => !((ISoftDeletable)e).IsDeleted
        var parameter = Expression.Parameter(clrType, "e");
        var castToSoftDeletable = Expression.Convert(parameter, typeof(ISoftDeletable));
        var isDeletedProperty = Expression.Property(castToSoftDeletable, nameof(ISoftDeletable.IsDeleted));
        var notDeleted = Expression.Not(isDeletedProperty);
        var lambda = Expression.Lambda(notDeleted, parameter);

        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }
}
