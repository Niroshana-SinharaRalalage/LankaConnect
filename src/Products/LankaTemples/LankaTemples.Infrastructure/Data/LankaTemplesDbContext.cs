using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Products.LankaTemples.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the LankaTemples product. Phase B
/// scaffolding per Consult #27 Q5 (2026-07-15) — empty <c>OnModelCreating</c>
/// as gate condition (scaffolding UNBLOCKED; aggregate mappings land per-aggregate
/// once Domain types are authored).
/// </summary>
/// <remarks>
/// <para>
/// <b>Default schema:</b> <c>templates</c>. Follows the per-module physical-schema
/// pattern (LankaEvents = <c>events</c>, Communications = <c>communications</c>,
/// Identity = <c>identity</c>, etc.).
/// </para>
/// <para>
/// <b>Cross-module principals</b> to ignore once aggregates land (mirrors
/// LankaEventsDbContext + IdentityDbContext + CommunicationsDbContext patterns):
/// User (Identity), Event (LankaEvents) if temple events reference LankaEvents,
/// MetroArea (LankaEvents) for location joins. Route cross-module reads via
/// Contracts surfaces per Blueprint §7.8.
/// </para>
/// <para>
/// <b>Wave 8.5.f interceptor:</b> once Domain aggregates are added, wire
/// <c>DomainEventSaveChangesInterceptor</c> in the LankaTemplesModule DI
/// registration exactly as LankaEventsModule / IdentityModule / CommunicationsModule
/// did.
/// </para>
/// </remarks>
public sealed class LankaTemplesDbContext : DbContext
{
    /// <summary>Postgres schema all LankaTemples-owned tables map into by default.</summary>
    public const string SchemaName = "temples";

    public LankaTemplesDbContext(DbContextOptions<LankaTemplesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        // Phase B scaffolding — no aggregates mapped yet. First aggregate lands with
        // an ApplyConfiguration call + an initial migration (see LankaEvents.Infrastructure
        // migration pattern for reference).

        base.OnModelCreating(modelBuilder);
    }
}
