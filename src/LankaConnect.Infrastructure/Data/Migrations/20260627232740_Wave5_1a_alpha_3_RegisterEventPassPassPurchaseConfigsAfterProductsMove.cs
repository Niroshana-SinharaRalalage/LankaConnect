using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave 5.1.a-α.3 (2026-06-27) — pure model-snapshot rebaseline.
    ///
    /// The W5.1.a-α.3 atomic move relocated EventPass + PassPurchase from
    /// LankaConnect.Domain.Events.Entities to LankaConnect.Products.LankaEvents.Domain.Entities.
    /// Same commit also registered EventPassConfiguration + PassPurchaseConfiguration in
    /// AppDbContext.OnModelCreating (pre-existing dead-file fix — they were defined in
    /// Phase 6AX but never applied). Together these brought the EF model snapshot in line
    /// with the actual DB schema for these two tables for the first time since Phase 6AX.
    ///
    /// The scaffolder consequently emitted CreateTable for event_passes + pass_purchases
    /// (absent from snapshot), plus a DropCheckConstraint/AddCheckConstraint pair on
    /// registrations.ck_registrations_valid_format whose only difference is \n vs \r\n
    /// line endings in the constraint SQL string — PostgreSQL normalises both to the same
    /// constraint, so it is also a no-op. Plus the usual reference_values.created_at
    /// UpdateData churn from .HasData seed timestamp reflection.
    ///
    /// The tables already exist in staging (created by migration
    /// 20251123163612_AddSignUpListAndSignUpCommitmentTables.cs with IDENTICAL columns,
    /// types, FKs, and indexes). Running CreateTable would error "already exists".
    /// Empty Up()/Down() per the [[empty-up-snapshot-rebaseline]] precedent. The model
    /// snapshot delta (.Designer.cs + AppDbContextModelSnapshot.cs) captures the new
    /// entity registrations so subsequent migrations diff correctly.
    ///
    /// Idempotent SQL: single __EFMigrationsHistory row insert, no DDL.
    /// </summary>
    public partial class Wave5_1a_alpha_3_RegisterEventPassPassPurchaseConfigsAfterProductsMove : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: model-snapshot rebaseline only.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: model-snapshot rebaseline only.
        }
    }
}
