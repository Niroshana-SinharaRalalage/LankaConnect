using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

/// <summary>
/// Marker interface preserved for DI-resolution compatibility. All methods
/// have been retired.
/// </summary>
/// <remarks>
/// <para>
/// <b>Retired 2026-07-17 (Wave 8.5.h, Tech Lead D-01)</b> — the
/// <c>CommitAsync(DbContext[], CancellationToken)</c> overload was removed
/// because its shared-connection <c>Database.UseTransactionAsync</c> pattern
/// silently emitted "The specified transaction is not associated with the
/// current connection." at runtime when AppDbContext + a module DbContext
/// drew separate physical connections from the Npgsql pool. Fixing that
/// properly required scoped shared-connection wiring (1-2 days engineering);
/// retirement took ~2 hours by refactoring the 16 live callers to per-context
/// direct <c>SaveChangesAsync</c> plus the per-module
/// <c>DomainEventSaveChangesInterceptor</c> (Wave 8.5.f) for domain-event
/// dispatch.
/// </para>
/// <para>
/// <b>Do NOT re-add methods here.</b> Cross-context propagation should route
/// through integration events (contracts + outbox) — see
/// <c>IIntegrationEventOutbox&lt;TDbContext&gt;</c> and Consult #25 Q6 blanket
/// approval of direct-SaveChanges. If a Phase B product surfaces a truly
/// atomic cross-context write that a saga cannot decompose, escalate to
/// architect for saga-log infrastructure — do NOT re-introduce a multi-context
/// commit shim.
/// </para>
/// <para>
/// The type itself is preserved (not deleted) so the DI registration in
/// <c>LankaConnect.API.LegacyInfrastructureDependencyInjection</c> and any
/// pre-existing <c>&lt;see cref&gt;</c> references in module repositories
/// continue to resolve. See
/// <c>docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md</c> for the
/// per-module DbContext ownership rules.
/// </para>
/// </remarks>
public interface IMultiContextUnitOfWork : IUnitOfWork
{
    // Wave 8.5.h (2026-07-17): CommitAsync(DbContext[]) method retired per
    // Tech Lead D-01. See remarks above. ArchTest rule
    // Rule15_UnitOfWork_DoesNotReintroduce_MultiContextCommitAsync guards
    // against regression.
}
