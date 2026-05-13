namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Provides the currently-authenticated actor's identifier so behaviors
/// (audit, correlation) can attribute their records.
/// </summary>
/// <remarks>
/// Concrete implementation lives in <c>BuildingBlocks.Web</c> (W2.6) and
/// reads from <c>HttpContext.User</c>. Background jobs / system-initiated
/// operations bind a separate <see cref="ICurrentActor"/> implementation
/// that returns a fixed system identifier.
/// </remarks>
public interface ICurrentActor
{
    /// <summary>
    /// Stable string identifier (typically the user GUID) or <c>null</c> for
    /// anonymous / unauthenticated callers.
    /// </summary>
    string? ActorId { get; }
}
