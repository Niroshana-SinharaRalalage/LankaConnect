using System;

namespace LankaConnect.Domain.Common;

/// <summary>
/// TDD GREEN Phase: AggregateRoot Implementation
/// Base class for aggregate roots in LankaConnect domain model
/// Inherits from BaseEntity to support repository patterns
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    // Id, CreatedAt, and UpdatedAt are inherited from BaseEntity

    /// <summary>
    /// Version for optimistic concurrency control
    /// </summary>
    public long Version { get; protected set; }

    protected AggregateRoot() : base()
    {
        Version = 0;
    }

    protected AggregateRoot(Guid id) : base(id)
    {
        Version = 0;
    }

    /// <summary>
    /// Validates the aggregate state
    /// </summary>
    public abstract ValidationResult Validate();

    /// <summary>
    /// Marks the aggregate as updated (overrides BaseEntity)
    /// </summary>
    protected new void MarkAsUpdated()
    {
        base.MarkAsUpdated();
        Version++;
    }

    /// <summary>
    /// String representation (overrides BaseEntity)
    /// </summary>
    public override string ToString() => $"{GetType().Name} [Id: {Id}, Version: {Version}]";
}