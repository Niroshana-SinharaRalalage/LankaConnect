using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave 5.2.a-fix (2026-06-28) — EventPass + PassPurchase feature removal.
    ///
    /// Founder ruling on 2026-06-28: EventPass + PassPurchase feature was early
    /// exploration code superseded by TicketTier (multi-tier ticketing with seat
    /// assignments + OnPlatform payment) in production. Never wired to a UI, never
    /// created passes in production (all 146 staging events have passCount=0), no
    /// public POST endpoint for PassPurchase. The W5.2.a HasMany fix (commit
    /// 9d9c2e78) made EF query the underlying tables for the first time and exposed
    /// that they don't exist in the staging DB — root cause unknown but irrelevant
    /// once the feature is being deleted.
    ///
    /// SCHEMA IMPACT: ZERO. The scaffolder generated DropTable for event_passes +
    /// pass_purchases (because they were in the snapshot from W5.1.a-α.3 commit
    /// 47e14ef9 / hotfix2 59ed4483 + W5.2.a 9d9c2e78). Running those DropTable ops
    /// would error with 42P01 "relation does not exist" because the tables are
    /// already missing from staging (root cause investigation in
    /// docs/architecture/W52A_TABLE_DRIFT_INVESTIGATION.md). Empty Up()/Down() per
    /// [[empty-up-snapshot-rebaseline]] precedent. The model snapshot delta
    /// (.Designer.cs + AppDbContextModelSnapshot.cs) carries the EventPass /
    /// PassPurchase entity removals so subsequent migrations diff correctly.
    ///
    /// Reference_values.created_at HasData seed timestamp churn (12 UpdateData ops
    /// scaffolded from EF model rebuild) stripped — would overwrite production
    /// audit timestamps for a no-op model rebuild.
    ///
    /// Idempotent SQL: single __EFMigrationsHistory row insert. Zero DDL.
    /// </summary>
    public partial class Wave5_2a_fix_RemoveEventPassPassPurchaseFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: model-snapshot rebaseline only.
            // Tables don't exist in staging (per W52A_TABLE_DRIFT_INVESTIGATION dossier);
            // DropTable would error. Snapshot delta carries the entity removals.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: model-snapshot rebaseline only.
        }
    }
}
