namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module read-only API for the Identity module. Consumers outside Identity
/// (e.g. <c>EventCancellationEmailJob</c> bulk-fetching recipient emails,
/// <c>CreateEventCommandHandler</c> loading the organizer for audit fields,
/// <c>GetBadgesQueryHandler</c> resolving creator display names, the 4
/// Communications.Application EmailGroup handlers per architect Additional
/// Finding #4) inject this interface instead of reaching into
/// <c>LankaConnect.Domain.Users</c> or the legacy <c>IUserRepository</c>,
/// preserving the ArchTest module boundary
/// <c>LegacyApplication_DoesNotDependOnIdentityDomain</c> (Wave 4.6.d.3).
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Implementation lives in Identity.Application as
/// <c>IdentityQueries</c> (added in Wave 4.6.b); DI wiring is in
/// <c>Identity.Api.IdentityModule</c>.
/// <para>
/// Per architect Additional Finding #5 (2026-06-24), this surface uses
/// <c>Guid</c> for user identifiers instead of the
/// <c>SharedKernel.Identity.UserId</c> strongly-typed primitive. The latter
/// would require a 128-site refactor of legacy callsites; the
/// <c>Guid</c>-then-<c>UserId</c> promotion is deferred to a future cleanup
/// wave.
/// </para>
/// <para>
/// Auth-internal lookups (<c>GetByRefreshTokenAsync</c>,
/// <c>GetByEmailVerificationTokenAsync</c>, <c>GetByPasswordResetTokenAsync</c>,
/// <c>GetByExternalProviderIdAsync</c>) are deliberately omitted -- they stay
/// on the Identity-internal <c>IUserRepository</c> consumed only by Identity
/// command handlers.
/// </para>
/// </remarks>
public interface IIdentityQueries
{
    // -------- Single-user reads --------

    /// <summary>
    /// Returns a single user by primary key WITHOUT full detail.
    /// Returns <c>null</c> when no row exists.
    /// </summary>
    Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns a single user by primary key WITH full detail (preferences,
    /// profile, last-login). Returns <c>null</c> when no row exists.
    /// </summary>
    Task<UserDetailDto?> GetUserDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns a single user by email address (case-insensitive comparison
    /// applied at the repository layer). Returns <c>null</c> when no row exists.
    /// </summary>
    Task<UserSummaryDto?> GetByEmailAsync(string email, CancellationToken ct = default);

    // -------- Batch reads (N+1 mitigation) --------

    /// <summary>
    /// Batch query: returns users matching the supplied ids. Missing ids are
    /// silently skipped (the returned list is a subset of <paramref name="ids"/>).
    /// Order is unspecified at the Contracts layer.
    /// </summary>
    Task<IReadOnlyList<UserSummaryDto>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Batch display-name lookup: returns a map of user-id to FullName
    /// concatenation. Used by creator-attribution rendering (e.g. badge cards,
    /// activity feeds). Missing ids omitted.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Batch email lookup: returns a map of user-id to email. Used by bulk
    /// email recipient resolution (e.g. EventCancellationEmailJob). Missing
    /// ids omitted.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsByUserIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default);

    // -------- Search + admin paged list --------

    /// <summary>
    /// Free-text search by name; returns up to 50 results ordered by name
    /// match relevance. Used by attendee-attribution typeahead.
    /// </summary>
    Task<IReadOnlyList<UserSummaryDto>> SearchByNameAsync(
        string term,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 6A.133 search-by-name-email-or-phone surface for co-organizer
    /// linking. Excludes the supplied user id (typically current user).
    /// Returns up to <paramref name="maxResults"/> matches.
    /// </summary>
    /// <remarks>
    /// Wave 4.6.d.2.a (2026-06-24). Added per architect ruling for the
    /// BatchLinkOrganizerContacts consumer + similar attendee-search paths.
    /// </remarks>
    Task<IReadOnlyList<UserSummaryDto>> SearchUsersAsync(
        string searchTerm,
        Guid? excludeUserId,
        int maxResults,
        CancellationToken ct = default);

    /// <summary>
    /// Batch lookup by email addresses. Returns a list of users whose emails
    /// match (case-insensitive). Missing emails silently skipped.
    /// </summary>
    /// <remarks>
    /// Wave 4.6.d.2.a (2026-06-24). Added per architect ruling for the
    /// RegisterAnonymousAttendee + EventCancellationEmailJob newsletter-tail
    /// flow that match attendees by email.
    /// </remarks>
    Task<IReadOnlyList<UserSummaryDto>> GetUserSummariesByEmailsAsync(
        IReadOnlyList<string> emails,
        CancellationToken ct = default);

    /// <summary>
    /// Email-orchestration projection: returns a user's contact-tier fields
    /// (email + display name + active + email-verified + lockout flags +
    /// profile-photo). Used by the ~18 legacy consumers that need a few
    /// fields beyond <see cref="UserSummaryDto"/> but not the full
    /// <see cref="UserDetailDto"/> admin shape.
    /// </summary>
    /// <remarks>
    /// Wave 4.6.d.2.a (2026-06-24). Architect-mandated to replace the bulk
    /// of d.2.b consumer swaps. Returns <c>null</c> when no row exists.
    /// </remarks>
    Task<UserContactDto?> GetContactInfoAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Admin user-list paging endpoint. Filter by role + active flag; search
    /// applies to email + display name. Returns the page slice + total count
    /// for the table pager.
    /// </summary>
    Task<(IReadOnlyList<UserSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        UserRoleDto? roleFilter,
        bool? activeFilter,
        CancellationToken ct = default);

    /// <summary>
    /// Admin pending role-upgrade queue. Returns all users with an active
    /// upgrade request awaiting Admin review.
    /// </summary>
    Task<IReadOnlyList<UserPendingRoleUpgradeDto>> GetUsersWithPendingRoleUpgradesAsync(
        CancellationToken ct = default);

    // -------- Counters --------

    /// <summary>
    /// Total user count (includes locked + deactivated).
    /// </summary>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Active user count (excludes locked + deactivated).
    /// </summary>
    Task<int> CountActiveUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Locked user count (active accounts currently inside lockout window).
    /// </summary>
    Task<int> CountLockedAccountsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a map of role -> count for admin dashboard tile rendering.
    /// </summary>
    Task<IReadOnlyDictionary<UserRoleDto, int>> GetUserCountsByRoleAsync(
        CancellationToken ct = default);

    // -------- Existence probes --------

    /// <summary>
    /// Probe whether a user with the supplied email already exists. Used by
    /// registration + email-change flows.
    /// </summary>
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct = default);
}
