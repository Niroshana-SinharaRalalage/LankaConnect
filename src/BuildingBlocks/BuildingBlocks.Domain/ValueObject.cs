namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Base class for DDD value objects. Equality is structural, computed from
/// the components yielded by <see cref="GetEqualityComponents"/>.
/// </summary>
/// <remarks>
/// <para>
/// Value objects have no identity; two with the same components are
/// interchangeable. Examples: <c>Money</c>, <c>Address</c>, <c>DateRange</c>.
/// Contrast with <see cref="Entity{TId}"/> which uses identity-based equality.
/// </para>
/// <para>
/// Implementations override <see cref="GetEqualityComponents"/> to yield each
/// field that contributes to equality. Order matters — yield components in a
/// stable order so hash codes are deterministic across runs.
/// </para>
/// <para>
/// Records can sometimes replace this class, but value objects often need
/// invariant enforcement in constructors and behavior methods that a record's
/// auto-generated structural equality doesn't accommodate cleanly. This base
/// is preferred for value objects with behavior.
/// </para>
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Yields the components used for equality + hashing, in stable order.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }
        return hash.ToHashCode();
    }

    /// <summary>Structural equality operator.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Structural inequality operator.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
