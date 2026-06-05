namespace LankaConnect.SharedKernel.Identity;

/// <summary>
/// Typed accessor for the currently-authenticated user. Returns
/// <see cref="UserId"/> rather than a raw string so callers can't accidentally
/// pass actor identifiers where typed user IDs are expected.
/// </summary>
/// <remarks>
/// <para>
/// <b>Difference vs ICurrentActor</b>: <c>ICurrentActor.ActorId</c> returns a
/// <see cref="string"/> that may be a user GUID OR a system principal name
/// (<c>"system"</c>, <c>"migration:Phase6A.148"</c>, <c>"webhook:stripe"</c>).
/// <see cref="IUserContext.CurrentUserId"/> returns ONLY a typed user ID, or
/// <c>null</c> for anonymous / system-actor requests. Use IUserContext when
/// the code path requires a real human user (e.g. authorization checks);
/// use ICurrentActor for audit logging where any actor identity is OK.
/// </para>
/// <para>
/// <b>Implementations</b>: BuildingBlocks.Web wires an
/// <c>HttpContext.User</c>-backed impl that extracts the user's
/// <c>NameIdentifier</c> claim. Background-job hosts wire a no-op impl
/// returning <c>null</c>.
/// </para>
/// </remarks>
public interface IUserContext
{
    /// <summary>The current user's typed ID, or <c>null</c> for anonymous / system requests.</summary>
    UserId? CurrentUserId { get; }
}
