using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Notifications.Infrastructure.Migrations;

/// <summary>
/// Phase A W3.5 BASELINE migration for <c>NotificationsDbContext</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Up() / Down() are intentionally empty.</b> The physical
/// <c>notifications.notifications</c> table + indexes were created by the
/// legacy <c>AppDbContext</c> migration <c>20251111172127_AddNotificationsTable</c>
/// (2025-11-11, Phase 6A.6). This baseline exists only so the new
/// <c>NotificationsDbContext</c> sees the schema as "already applied" — preventing
/// <c>dotnet ef database update</c> from issuing a CREATE TABLE against a table
/// that already exists.
/// </para>
/// <para>
/// <b>Deployment notes</b>:
/// </para>
/// <list type="bullet">
///   <item>On staging/prod databases where the table ALREADY exists, the
///   per-context <c>notifications.__EFMigrationsHistory</c> row must be
///   inserted ONCE manually (EF only writes that row when it RUNS Up(), which
///   is empty here):
///   <code>
///   INSERT INTO notifications."__EFMigrationsHistory" (migration_id, product_version)
///   VALUES ('20260603221324_Baseline_Notifications', '8.0.19');
///   </code></item>
///   <item>On a fresh local dev database where the table DOESN'T exist:
///   running the legacy AppDbContext migration chain first (still creates the
///   table); then the empty Up() here is a safe no-op.</item>
/// </list>
/// <para>
/// The companion <c>.Designer.cs</c> records the model snapshot at this point
/// (Notification entity, lowercase <c>notifications.notifications</c> table
/// mapping). Subsequent migrations diff against this snapshot.
/// </para>
/// </remarks>
public partial class Baseline_Notifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — see <remarks> on the class.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — see <remarks> on the class.
    }
}
