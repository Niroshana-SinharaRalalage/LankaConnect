namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Opt-in marker for entities that should be soft-deleted (logical delete)
/// rather than physically removed. <c>BaseDbContext</c> in W2.5 intercepts
/// <c>EntityState.Deleted</c> on these entities, flips them to Modified, and
/// sets <see cref="IsDeleted"/>; queries on the DbSet exclude deleted rows
/// by default via a global query filter.
/// </summary>
/// <remarks>
/// <para>
/// To query INCLUDING soft-deleted rows (e.g. admin recovery flows), use
/// <c>.IgnoreQueryFilters()</c> on the query.
/// </para>
/// <para>
/// Hard-delete is still possible via <c>DbContext.Database.ExecuteSqlRaw</c>
/// or by removing <see cref="ISoftDeletable"/> from the entity contract —
/// the soft-delete interception is opt-in, not blanket policy.
/// </para>
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>True if this row is logically deleted (excluded from default queries).</summary>
    bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when soft-delete was applied; null until then.</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>Actor that performed the soft-delete, or null for anonymous.</summary>
    string? DeletedBy { get; set; }
}
