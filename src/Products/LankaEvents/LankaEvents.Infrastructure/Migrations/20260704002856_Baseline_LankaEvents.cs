using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LankaConnect.Products.LankaEvents.Infrastructure.Migrations;

/// <summary>
/// Wave 6.5.e (2026-07-03) BASELINE migration for <see cref="LankaConnect.Products.LankaEvents.Infrastructure.Data.LankaEventsDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Up() / Down() are intentionally empty.</b> Every business-entity table
/// mapped by <c>LankaEventsDbContext</c> (Event, Registration, Ticket, Sponsor,
/// Donation, Collection, SignUpList, VenueLayout, etc. — see the DbContext's
/// DbSet properties for the complete list) was created by the legacy
/// <c>AppDbContext</c> migration chain over Phases 6A.1 through 6A.156 (2025-11
/// through 2026-04). This baseline exists only so the new
/// <c>LankaEventsDbContext</c> sees the schema as "already applied" —
/// preventing <c>dotnet ef database update</c> from issuing CREATE TABLE
/// statements against tables that already exist.
/// </para>
/// <para>
/// Mirrors the W3.5 Notifications baseline (<c>Baseline_Notifications</c>) and
/// the W4.2 Media baseline (<c>Baseline_Media</c>) patterns. The companion
/// <c>.Designer.cs</c> + <c>LankaEventsDbContextModelSnapshot.cs</c> capture
/// the full Event-family model at Wave 6.5.e time — subsequent migrations on
/// this context diff against that snapshot.
/// </para>
/// <para>
/// <b>Deployment notes</b>:
/// </para>
/// <list type="bullet">
///   <item>On staging/prod databases where the Event-family tables ALREADY
///   exist, the per-context <c>events.__EFMigrationsHistory</c> row must be
///   inserted ONCE manually (EF only writes that row when it RUNS Up(), which
///   is empty here):
///   <code>
///   INSERT INTO events."__EFMigrationsHistory" (migration_id, product_version)
///   VALUES ('20260704002856_Baseline_LankaEvents', '8.0.19');
///   </code></item>
///   <item>On a fresh local dev database where the tables DON'T exist:
///   running the legacy AppDbContext migration chain first (still creates the
///   tables); then the empty Up() here is a safe no-op.</item>
///   <item>The follow-up migration
///   <c>Wave6_5_e_AddLankaEventsOperationalTables</c> creates the three
///   operational tables (<c>events.outbox</c> / <c>events.outbox_dead_letter</c>
///   / <c>events.idempotency_keys</c>) that back the Wave 6.5 outbox pipeline.</item>
/// </list>
/// </remarks>
public partial class Baseline_LankaEvents : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — legacy AppDbContext migrations already
        // materialised every table in this baseline's snapshot. See class-level
        // XML doc for the deployment notes.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — this baseline never issued DDL, so there is
        // nothing to undo. Rolling back would require dropping the entire
        // events schema, which is out of scope for a per-context baseline.
    }
}
