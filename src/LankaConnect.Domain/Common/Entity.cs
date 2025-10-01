using LankaConnect.Domain.Common.Contracts;

namespace LankaConnect.Domain.Common;

/// <summary>
/// Generic entity base class for domain entities with typed identifiers
/// </summary>
public abstract class Entity<T> : IEquatable<Entity<T>>
    where T : notnull
{
    public required T Id { get; init; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity()
    {
        // For entities with default ID generation - Id must be set by derived class
        CreatedAt = DateTime.UtcNow;
    }

    protected Entity(T id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<T> entity && Equals(entity);
    }

    public bool Equals(Entity<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<T>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<T>? left, Entity<T>? right)
    {
        return EqualityComparer<Entity<T>>.Default.Equals(left, right);
    }

    public static bool operator !=(Entity<T>? left, Entity<T>? right)
    {
        return !(left == right);
    }
}