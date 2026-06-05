namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Marker for repositories that encapsulate access to a single aggregate root.
/// Empty by design — concrete repositories declare their OWN named query
/// methods (FindByIdAsync, FindActiveAsync, etc.). Per ADR-010, generic
/// <c>FindAsync(predicate)</c>-style bases are forbidden because they let
/// callers query across aggregate boundaries.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type owned by this repository.</typeparam>
/// <typeparam name="TId">The aggregate root's identifier type.</typeparam>
/// <remarks>
/// <para>
/// <b>Why a marker</b>: ArchTest rule <c>Every_Capability_Repository_Implements_AggregateRepository_Marker</c>
/// (Wave 6) enforces that every repository interface inside a Capability's
/// Domain layer derives from this marker. New contributors writing a
/// repository that forgets the marker fail CI immediately, preventing the
/// reappearance of the legacy generic <c>Repository&lt;T&gt;</c> pattern
/// that ADR-010 retires.
/// </para>
/// <para>
/// <b>Why TAggregate AND TId</b>: most repository methods take or return an
/// aggregate; the typed TId lets <c>FindByIdAsync(TId)</c> signatures stay
/// type-safe without the generic argument-soup of <c>IRepository&lt;T&gt;.GetById(object)</c>.
/// </para>
/// <para>
/// <b>Concrete pattern</b>:
/// <code>
/// public interface IEventRepository : IAggregateRepository&lt;Event, EventId&gt; {
///     Task&lt;Event?&gt; FindByIdAsync(EventId id, CancellationToken ct);
///     Task&lt;IReadOnlyList&lt;Event&gt;&gt; FindPublishedByOrganizerAsync(UserId organizerId, CancellationToken ct);
///     Task AddAsync(Event aggregate, CancellationToken ct);
///     // ... NO generic FindAsync(predicate), NO GetAll()
/// }
/// </code>
/// </para>
/// </remarks>
public interface IAggregateRepository<TAggregate, TId>
    where TAggregate : class
{
    // Marker only — concrete repositories define their own methods.
}
