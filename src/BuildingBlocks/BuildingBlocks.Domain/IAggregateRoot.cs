namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Marker for aggregate-root entities — the persistence + transaction boundary
/// in DDD. Repositories operate on aggregate roots, never on inner entities.
/// </summary>
/// <remarks>
/// <para>
/// Combined with <see cref="Entity{TId}"/> to give an aggregate identity plus
/// the marker. Pattern:
/// </para>
/// <code>
/// public sealed class Event : Entity&lt;Guid&gt;, IAggregateRoot
/// {
///     // ...
/// }
/// </code>
/// <para>
/// ArchTest in W2.2+ enforces that repositories return only types implementing
/// this interface.
/// </para>
/// </remarks>
public interface IAggregateRoot
{
}
