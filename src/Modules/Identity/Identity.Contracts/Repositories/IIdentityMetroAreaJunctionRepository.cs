namespace LankaConnect.Modules.Identity.Contracts.Repositories;

/// <summary>
/// Cross-module write surface for the <c>identity.user_preferred_metro_areas</c>
/// junction table. Owned by the Identity module; consumed by handlers that need
/// to add / remove / replace a user's preferred metro-area set without importing
/// <c>LankaConnect.Modules.Identity.Infrastructure.Data.IdentityDbContext</c>
/// into the Application layer.
/// </summary>
/// <remarks>
/// <para>
/// Wave 8.5.i (2026-07-18). Replaces the two direct
/// <c>_identityDbContext.Database.ExecuteSqlRawAsync</c> blocks in
/// <c>RegisterUserHandler</c> + <c>UpdateUserPreferredMetroAreasCommandHandler</c>
/// per Blueprint §7.8 (cross-module reads / writes go through Contracts
/// surfaces, not raw SQL leaking out of Application handlers).
/// </para>
/// <para>
/// Sprint-Day 7 (2026-07-14) landed the raw-SQL pattern as a hotfix while the
/// shadow-junction navigation on <see cref="Identity.Domain.Entities.User"/>
/// still <c>Ignore</c>d <c>MetroArea</c> under <c>IdentityDbContext</c> — the
/// shortcut worked but leaked persistence details (schema + table + column
/// names + SQL) into two Application-layer handlers. This interface hides that
/// behind a repository whose implementation lives in
/// <c>Identity.Infrastructure</c>.
/// </para>
/// <para>
/// Per Consult #15 PASS C: interface signatures use <see cref="Guid"/>, not the
/// Identity domain aggregate type. The implementation may continue to use raw
/// SQL against <c>IdentityDbContext</c> until a future migration re-introduces
/// the shadow navigation properly (Phase B follow-up).
/// </para>
/// </remarks>
public interface IIdentityMetroAreaJunctionRepository
{
    /// <summary>
    /// Replaces the full set of preferred metro-area IDs for the specified user.
    /// Deletes every existing junction row for <paramref name="userId"/> then
    /// inserts one row per element of <paramref name="metroAreaIds"/>. If
    /// <paramref name="metroAreaIds"/> is empty the user's preferences are
    /// cleared.
    /// </summary>
    /// <remarks>
    /// Callers own the surrounding transaction — this method issues delete +
    /// insert statements against the DbContext but does NOT call SaveChanges
    /// on any tracked-entity graph. The junction is unmanaged EF-side under
    /// <c>IdentityDbContext</c> (per <c>modelBuilder.Ignore&lt;MetroArea&gt;()</c>
    /// in <c>IdentityDbContext.OnModelCreating</c>), so no SaveChanges call is
    /// necessary to flush the junction writes.
    /// </remarks>
    Task ReplacePreferredMetroAreasAsync(
        Guid userId,
        IReadOnlyList<Guid> metroAreaIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a single junction row for <paramref name="userId"/> +
    /// <paramref name="metroAreaId"/>. Idempotent — no-ops on primary-key
    /// conflict (<c>ON CONFLICT DO NOTHING</c>) so callers can safely replay
    /// the operation without needing to pre-check the current junction state.
    /// </summary>
    Task AddPreferredMetroAreaAsync(
        Guid userId,
        Guid metroAreaId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a single junction row for <paramref name="userId"/> +
    /// <paramref name="metroAreaId"/>. Idempotent — no-ops when the row does
    /// not exist.
    /// </summary>
    Task RemovePreferredMetroAreaAsync(
        Guid userId,
        Guid metroAreaId,
        CancellationToken cancellationToken);
}
