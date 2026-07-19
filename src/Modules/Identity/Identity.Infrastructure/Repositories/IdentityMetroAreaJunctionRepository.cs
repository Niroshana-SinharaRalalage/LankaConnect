using LankaConnect.Modules.Identity.Contracts.Repositories;
using LankaConnect.Modules.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Identity.Infrastructure.Repositories;

/// <summary>
/// EF-Core-backed implementation of
/// <see cref="IIdentityMetroAreaJunctionRepository"/>. Writes to the
/// <c>identity.user_preferred_metro_areas</c> junction table via
/// <see cref="IdentityDbContext"/> using parameterised raw-SQL statements —
/// the junction is EF-<c>Ignore</c>d under <c>IdentityDbContext</c>
/// (see <c>IdentityDbContext.OnModelCreating</c>) because the MetroArea
/// principal aggregate is owned by the LankaEvents product per
/// Blueprint §7.8, so the shadow navigation on
/// <see cref="Identity.Domain.Entities.User"/> cannot flush changes via
/// the change tracker on this context.
/// </summary>
/// <remarks>
/// <para>
/// Wave 8.5.i (2026-07-18). Consolidates the two raw-SQL blocks that
/// Sprint-Day 7 (2026-07-14) hotfix-added to
/// <c>RegisterUserHandler</c> + <c>UpdateUserPreferredMetroAreasCommandHandler</c>
/// into a single Infrastructure-owned adapter. The Application layer no
/// longer imports <c>IdentityDbContext</c> for junction persistence and
/// no longer contains inline INSERT / DELETE strings — those details are
/// encapsulated here.
/// </para>
/// <para>
/// <b>Raw-SQL retention rationale.</b> The task brief cites Blueprint §7.8
/// which mandates cross-module writes go through Contracts surfaces — this
/// interface satisfies that constraint at the module boundary. The
/// implementation still uses raw SQL because the alternative
/// (re-introducing the shadow navigation with proper MetroArea
/// hydration) requires changing <c>IdentityDbContext.OnModelCreating</c>
/// to stop <c>Ignore</c>-ing MetroArea, which cascades into a cross-module
/// model bleed that Consult #14 PASS B explicitly forbids. Deferred to
/// Phase B when the MetroArea shape is either promoted to SharedKernel
/// (making the shadow nav trivially valid) or replaced by an integration
/// event.
/// </para>
/// <para>
/// <b>Transaction ownership.</b> None of these methods call
/// <c>SaveChangesAsync</c>. The junction rows are written to Postgres
/// immediately by the underlying <c>ExecuteSqlRawAsync</c> call — they are
/// NOT staged in the EF change tracker (the junction is <c>Ignore</c>d).
/// Callers who need atomicity with a companion User aggregate write should
/// wrap the sequence in a <see cref="Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction"/>
/// on <see cref="IdentityDbContext"/> — deferred as Phase B follow-up.
/// </para>
/// </remarks>
public sealed class IdentityMetroAreaJunctionRepository : IIdentityMetroAreaJunctionRepository
{
    private readonly IdentityDbContext _dbContext;

    public IdentityMetroAreaJunctionRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task ReplacePreferredMetroAreasAsync(
        Guid userId,
        IReadOnlyList<Guid> metroAreaIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metroAreaIds);

        // Delete every existing junction row for this user. Idempotent when the
        // user has no current preferences.
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM identity.user_preferred_metro_areas WHERE user_id = {0}",
            new object[] { userId },
            cancellationToken);

        // Re-insert the new preferences (0..N rows). We keep the loop simple —
        // a single INSERT with a VALUES multi-row expression would be marginally
        // faster but requires manual parameter interpolation that is not worth
        // the readability trade-off for the typical 1-20 metro-area cap.
        foreach (var metroAreaId in metroAreaIds)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO identity.user_preferred_metro_areas (user_id, metro_area_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                new object[] { userId, metroAreaId },
                cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task AddPreferredMetroAreaAsync(
        Guid userId,
        Guid metroAreaId,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO identity.user_preferred_metro_areas (user_id, metro_area_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
            new object[] { userId, metroAreaId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemovePreferredMetroAreaAsync(
        Guid userId,
        Guid metroAreaId,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM identity.user_preferred_metro_areas WHERE user_id = {0} AND metro_area_id = {1}",
            new object[] { userId, metroAreaId },
            cancellationToken);
    }
}
