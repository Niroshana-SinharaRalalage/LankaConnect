using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave 5.2.a (2026-06-28) — fixes the AddPass HasMany configuration gap that
    /// surfaced as KNOWN-DEFECT-DEFERRED in W5.1.a-alpha.3 commit 47e14ef9.
    ///
    /// EventConfiguration.cs now explicitly declares:
    ///   builder.HasMany(e => e.Passes).WithOne().HasForeignKey("EventId")
    ///                                  .OnDelete(DeleteBehavior.Cascade);
    ///   builder.Navigation(e => e.Passes).UsePropertyAccessMode(PropertyAccessMode.Field);
    ///
    /// Previously absent: EF Core auto-discovered EventPass via Event.Passes nav with
    /// default conventions but did NOT register the parent-child relationship correctly,
    /// causing UnitOfWork.CommitAsync to report "0 changes committed" after AddPass on
    /// a tracked Event. The explicit config wires the change tracker properly.
    ///
    /// SCHEMA IMPACT: ZERO. The existing event_passes.event_id FK constraint
    /// (FK_event_passes_events_event_id, ON DELETE CASCADE) was created by migration
    /// 20251123163612_AddSignUpListAndSignUpCommitmentTables.cs with the EXACT shape
    /// the explicit HasMany config produces. EF Core's scaffolder confirmed this by
    /// emitting ZERO DDL (no DropForeignKey, no AddForeignKey, no DropConstraint, no
    /// AlterColumn). The only scaffolded ops were reference_values.created_at
    /// UpdateData churn from .HasData seed timestamp reflection — stripped from the
    /// migration body to prevent production-data modification.
    ///
    /// Empty Up()/Down() per [[empty-up-snapshot-rebaseline]] precedent. The model
    /// snapshot delta (.Designer.cs + AppDbContextModelSnapshot.cs) captures the new
    /// HasMany registration so subsequent migrations diff correctly. NO destructive
    /// DDL means NO SCHEMA-DESTRUCTIVE-APPROVED header required.
    ///
    /// Idempotent SQL artifact: single __EFMigrationsHistory row insert. Zero DDL.
    /// </summary>
    public partial class Wave5_2a_AddPassHasManyConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: pure model-snapshot rebaseline for the HasMany registration.
            // The underlying FK constraint already exists (event_passes.event_id from migration
            // 20251123163612). Behavioral fix is in the C# config layer (EventConfiguration.cs);
            // schema unchanged.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: pure model-snapshot rebaseline.
        }
    }
}
