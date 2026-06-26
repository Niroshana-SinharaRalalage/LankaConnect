namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module projection of a user's location + metro-area preferences. Returned
/// by <see cref="IIdentityQueries.GetPreferencesAsync"/>. Wave 4.10.s1 (2026-06-26)
/// added for the Cross-cutting cleanup drain — consumed by
/// Events.Queries.GetEventsQueryHandler and Events.Queries.GetFeaturedEventsQueryHandler
/// to replace their direct injection of <c>IUserRepository</c>.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="PreferredMetroAreaIds">
/// Metro areas the user has explicitly preferred (empty when none chosen).
/// </param>
/// <param name="LocationCity">User's location city; null when no location set.</param>
/// <param name="LocationState">User's location state; null when no location set.</param>
public sealed record UserPreferencesProjectionDto(
    Guid UserId,
    IReadOnlyList<Guid> PreferredMetroAreaIds,
    string? LocationCity,
    string? LocationState);
