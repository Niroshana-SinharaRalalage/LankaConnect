using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Common.Entities;

/// <summary>
/// Base class for all domain entities
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
public abstract class EntityBase<TId> : Entity<TId>
    where TId : struct
{
    protected EntityBase(TId id) : base(id)
    {
    }

    protected EntityBase() : base()
    {
    }
}

/// <summary>
/// Base class for entities with standard Guid identifiers
/// </summary>
public abstract class EntityBase : EntityBase<Guid>
{
    protected EntityBase(Guid id) : base(id)
    {
    }

    protected EntityBase() : base(Guid.NewGuid())
    {
    }
}