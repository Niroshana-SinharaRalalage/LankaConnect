using LankaConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Wave 6.5.a: extends the legacy <see cref="IUnitOfWork"/> with a multi-context
/// overload so a handler can atomically commit changes across
/// <see cref="AppDbContext"/> AND one or more per-module DbContexts (e.g.
/// <c>MediaDbContext</c>, <c>NotificationsDbContext</c>, <c>FormsDbContext</c>).
/// </summary>
/// <remarks>
/// <para>
/// Retires the "self-saving repository" pattern documented in ADR-006 and
/// implemented as a stop-gap in NotificationRepository, PhotoAlbumRepository,
/// FormRepository, and FormResponseRepository. Those repos call
/// <c>_context.SaveChangesAsync()</c> internally because the legacy
/// <see cref="IUnitOfWork.CommitAsync"/> only saves <see cref="AppDbContext"/>.
/// F30a (Wave 9.h.10.6 commit <c>f0b684a0</c>) proved the pattern silently
/// corrupts data when handlers forget the self-save — the entire PhotoAlbums
/// feature was data-losing in production since the Wave 4.2 MediaDbContext
/// extraction.
/// </para>
/// <para>
/// The overload opens ONE transaction on <see cref="AppDbContext"/>, enrolls
/// each module context via <c>Database.UseTransaction</c>, calls
/// <c>SaveChangesAsync</c> on each in order, dispatches domain events across
/// ALL enrolled contexts, then commits. If any step throws, all changes roll
/// back atomically. <c>System.Transactions.TransactionScope</c> is explicitly
/// rejected per ADR-005 (Npgsql doesn't support DTC cleanly).
/// </para>
/// <para>
/// Handlers that need multi-context atomicity inject
/// <see cref="IMultiContextUnitOfWork"/> instead of the single-context
/// <see cref="IUnitOfWork"/>. Existing handlers depending on the legacy
/// interface continue to work unchanged.
/// </para>
/// </remarks>
public interface IMultiContextUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Commits a set of module DbContexts atomically alongside
    /// <see cref="AppDbContext"/>. Domain events raised on entities tracked by
    /// any enrolled context dispatch through MediatR AFTER the transaction
    /// commits successfully.
    /// </summary>
    /// <param name="moduleContexts">Zero or more per-module DbContexts to
    /// enroll in the transaction. May be empty (equivalent to
    /// <see cref="IUnitOfWork.CommitAsync"/> semantics but with a single call
    /// site for both patterns).</param>
    /// <param name="cancellationToken">Standard cancellation token; a throw
    /// mid-commit rolls back all enrolled contexts.</param>
    /// <returns>Total change count summed across all committed contexts.</returns>
    Task<int> CommitAsync(DbContext[] moduleContexts, CancellationToken cancellationToken = default);
}
