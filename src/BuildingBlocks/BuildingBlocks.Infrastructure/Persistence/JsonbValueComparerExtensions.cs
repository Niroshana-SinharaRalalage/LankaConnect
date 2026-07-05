using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LankaConnect.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Helpers for configuring EF Core <see cref="ValueComparer{T}"/> on JSONB
/// columns backed by mutable collections — addresses MEMORY.md Phase 6A.129's
/// "JSONB silently omitted from UPDATE SQL" pitfall.
/// </summary>
/// <remarks>
/// <para>
/// EF Core's default snapshot for a collection property uses a SINGLE reference
/// to the live list. In-place mutations (the DDD pattern <c>backingField.Clear();
/// backingField.AddRange(newItems);</c>) modify the live list AND the snapshot
/// simultaneously — the change tracker sees no delta and omits the column from
/// the UPDATE statement.
/// </para>
/// <para>
/// The fix is a deep-copy snapshot via a custom <see cref="ValueComparer{T}"/>:
/// <c>SequenceEqual</c> for equality, content-aggregated hash, and
/// <c>list.ToList().AsReadOnly()</c> for the snapshot.
/// </para>
/// </remarks>
public static class JsonbValueComparerExtensions
{
    /// <summary>
    /// Applies a deep-copy <see cref="ValueComparer{T}"/> to an
    /// <c>IReadOnlyList&lt;T&gt;</c> JSONB property so in-place mutations on
    /// the backing field are detected as changes.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TElement">The collection element type.</typeparam>
    public static PropertyBuilder<IReadOnlyList<TElement>> ApplyJsonbReadOnlyListComparer<TEntity, TElement>(
        this PropertyBuilder<IReadOnlyList<TElement>> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        var comparer = new ValueComparer<IReadOnlyList<TElement>>(
            equalsExpression: (left, right) =>
                left == null && right == null
                || left != null && right != null && left.SequenceEqual(right),
            hashCodeExpression: list =>
                list == null
                    ? 0
                    : list.Aggregate(0, (acc, item) => HashCode.Combine(acc, item == null ? 0 : item.GetHashCode())),
            snapshotExpression: list =>
                list == null
                    ? (IReadOnlyList<TElement>)new List<TElement>().AsReadOnly()
                    : (IReadOnlyList<TElement>)list.ToList().AsReadOnly());

        builder.Metadata.SetValueComparer(comparer);
        return builder;
    }

    /// <summary>
    /// Applies a deep-copy <see cref="ValueComparer{T}"/> to a
    /// <c>List&lt;T&gt;</c> JSONB property — same intent as
    /// <see cref="ApplyJsonbReadOnlyListComparer{TEntity,TElement}"/> but for
    /// the concrete <c>List&lt;T&gt;</c> surface (some domain entities expose
    /// concrete types for serialization convenience).
    /// </summary>
    public static PropertyBuilder<List<TElement>> ApplyJsonbListComparer<TEntity, TElement>(
        this PropertyBuilder<List<TElement>> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        var comparer = new ValueComparer<List<TElement>>(
            equalsExpression: (left, right) =>
                left == null && right == null
                || left != null && right != null && left.SequenceEqual(right),
            hashCodeExpression: list =>
                list == null
                    ? 0
                    : list.Aggregate(0, (acc, item) => HashCode.Combine(acc, item == null ? 0 : item.GetHashCode())),
            snapshotExpression: list =>
                list == null
                    ? new List<TElement>()
                    : list.ToList());

        builder.Metadata.SetValueComparer(comparer);
        return builder;
    }
}
