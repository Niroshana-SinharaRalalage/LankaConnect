namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Legacy domain event marker. W3B (2026-06-05) bridged to
/// <see cref="LankaConnect.BuildingBlocks.Domain.IDomainEvent"/> so entities
/// migrated to <c>BB.Entity&lt;TId&gt;</c> can <c>RaiseDomainEvent(...)</c>
/// with legacy event records without forcing all events to migrate atomically.
/// </summary>
/// <remarks>
/// Both interfaces declare an identical <c>DateTime OccurredAt { get; }</c>
/// member, so any legacy event satisfies the BB contract automatically.
/// This interface is DEPRECATED for new code — declare events directly against
/// <see cref="LankaConnect.BuildingBlocks.Domain.IDomainEvent"/>. Wave 4
/// capability extraction migrates remaining legacy events to the BB contract
/// and deletes this bridge.
/// </remarks>
public interface IDomainEvent : LankaConnect.BuildingBlocks.Domain.IDomainEvent
{
}
