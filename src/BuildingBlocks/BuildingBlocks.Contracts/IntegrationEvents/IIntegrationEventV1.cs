namespace LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

/// <summary>
/// Marker interface declaring an integration event uses schema version 1.
/// </summary>
/// <remarks>
/// <para>
/// <b>Versioning convention</b>: every <see cref="IntegrationEventBase"/>
/// subclass should implement exactly one <c>IIntegrationEventV*</c> marker.
/// When the schema breaks compatibility, declare a NEW event class with
/// <c>IIntegrationEventV2</c> (etc) rather than mutating the existing type.
/// </para>
/// <para>
/// Subscribers can implement handlers for multiple versions concurrently
/// during a transition window — the dispatcher routes purely by concrete
/// CLR type, so V1 and V2 are independent dispatch keys.
/// </para>
/// <example>
/// <code>
/// public sealed record UserRegisteredV1 : IntegrationEventBase, IIntegrationEventV1
/// {
///     public required Guid UserId { get; init; }
///     public required string Email { get; init; }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IIntegrationEventV1
{
}
