namespace LankaConnect.Modules.Communications.Contracts;

/// <summary>
/// Cross-module read-only API for the Communications module's EmailGroup
/// aggregate. Consumers outside Communications (e.g. <c>Event</c> aggregate
/// fetching its linked groups via <c>_emailGroupIds</c>) inject this interface
/// instead of <c>Communications.Domain.Repositories.IEmailGroupRepository</c>,
/// preserving the ArchTest module boundary
/// <c>LegacyApplication_DoesNotDependOnCommunicationsDomain</c> (Wave 5.4.d.3).
/// </summary>
/// <remarks>
/// Wave 5.4.a (2026-06-13). Implementation lives in Communications.Application
/// as <c>EmailGroupQueries</c> (added in Wave 5.4.b); DI wiring is in
/// <c>Communications.Api.CommunicationsModule</c>.
/// <para>
/// Per architect ruling 2026-06-13 there is NO <c>GetByEventAsync</c> on this
/// surface. The Event aggregate owns its <c>_emailGroupIds</c> directly and
/// cross-aggregate fetches go through <see cref="GetByIdsAsync"/> (preserves
/// the Phase 6A.32 N+1 batch-fetch pattern + respects module boundary).
/// </para>
/// </remarks>
public interface IEmailGroupQueries
{
    /// <summary>
    /// Batch query: returns email groups matching the supplied ids. Missing ids
    /// are silently skipped (the returned list is a subset of <paramref name="ids"/>).
    /// Order is unspecified at the Contracts layer.
    /// </summary>
    Task<IReadOnlyList<EmailGroupSummaryDto>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single email group by primary key. Returns <c>null</c> when
    /// no row exists.
    /// </summary>
    Task<EmailGroupSummaryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns a single email group by primary key WITH its individual email
    /// addresses hydrated (the aggregate stores them as a CSV string; this
    /// projection splits + cleans them). Returns <c>null</c> when no row exists.
    /// </summary>
    Task<EmailGroupDetailDto?> GetByIdWithEmailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Batch detail query: returns email groups matching the supplied ids with
    /// their individual email addresses hydrated. Missing ids are silently
    /// skipped. Wave 5.4.d.1 (2026-06-22) — added so the cross-aggregate
    /// EventNotificationRecipientService can swap from IEmailGroupRepository
    /// to the Contracts surface without losing the email-list payload.
    /// </summary>
    Task<IReadOnlyList<EmailGroupDetailDto>> GetByIdsWithEmailsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all email groups owned by the given user.
    /// </summary>
    Task<IReadOnlyList<EmailGroupSummaryDto>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken ct = default);
}
