namespace LankaConnect.Domain.Common.Contracts;

/// <summary>
/// Contract for domain entities
/// </summary>
public interface IEntity<TId>
{
    TId Id { get; }
}

/// <summary>
/// Contract for domain entities with Guid identifiers
/// </summary>
public interface IEntity : IEntity<Guid>
{
}