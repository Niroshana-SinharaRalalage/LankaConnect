namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Generic entity base — entities have a stable identity (<see cref="Id"/>) that
/// distinguishes them from value objects, and may raise domain events as their
/// state mutates.
/// </summary>
/// <typeparam name="TId">
/// The identifier type. Common choices are <see cref="Guid"/> (for distributed
/// generation) or strongly-typed ID wrappers; never <see cref="string"/> for
/// identity (strings are mutable concepts; use typed IDs instead).
/// </typeparam>
/// <remarks>
/// <para>
/// Equality is identity-based: two entities are equal iff they share the same
/// concrete type and same <see cref="Id"/>. This differs from
/// <see cref="ValueObject"/> which uses structural equality.
/// </para>
/// <para>
/// Domain events accumulate on the entity until the persistence boundary
/// (typically the UnitOfWork in BuildingBlocks.Application W2.4) flushes
/// them via <see cref="ClearDomainEvents"/> after handlers run.
/// </para>
/// </remarks>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>The stable identity. Set once at construction and never mutated.</summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>Domain events raised by this entity since the last flush.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Required for EF Core proxy-based materialization and serializers; concrete
    /// entities should provide a public constructor that sets <see cref="Id"/>.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>Initializes the entity with its identity.</summary>
    /// <exception cref="ArgumentNullException">If <paramref name="id"/> is null.</exception>
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>
    /// Records a domain event for downstream dispatch when the unit of work commits.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="domainEvent"/> is null.</exception>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Empties the local event buffer — called by the dispatcher after events
    /// have been published downstream. Tests sometimes call this to reset state
    /// between scenarios.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // ---------- Identity equality ----------

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Different concrete types with the same Id are NOT equal — e.g. an
        // EventId(Guid X) and a UserId(Guid X) must remain distinct.
        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Entity<TId> e && Equals(e);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Identity equality operator.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Identity inequality operator.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
