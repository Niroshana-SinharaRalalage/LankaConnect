using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A121_AddDualNullableFieldsToSignUpItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.121: Add new nullable columns for dual nullable fields pattern
            // These columns support BOTH quantity-based ("10 plates") and slot-based ("3 slots") items

            // STEP 1: Add new nullable columns to sign_up_items
            migrationBuilder.AddColumn<int>(
                name: "target_quantity",
                schema: "events",
                table: "sign_up_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "available_slots",
                schema: "events",
                table: "sign_up_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "suggested_per_slot",
                schema: "events",
                table: "sign_up_items",
                type: "integer",
                nullable: true);

            // STEP 2: Data migration - Copy existing quantity values to target_quantity
            // All existing items are quantity-based, so we populate target_quantity
            migrationBuilder.Sql(@"
                UPDATE events.sign_up_items
                SET target_quantity = quantity
                WHERE quantity IS NOT NULL;
            ");

            // STEP 3: Add CHECK constraint to ensure exactly ONE of target_quantity or available_slots is populated
            // This prevents invalid states like both fields being null or both being populated
            migrationBuilder.Sql(@"
                ALTER TABLE events.sign_up_items
                ADD CONSTRAINT chk_sign_up_item_type CHECK (
                    (target_quantity IS NOT NULL AND available_slots IS NULL) OR
                    (available_slots IS NOT NULL AND target_quantity IS NULL)
                );
            ");

            // STEP 4: Drop old columns (safe now that data is migrated)
            migrationBuilder.DropColumn(
                name: "quantity",
                schema: "events",
                table: "sign_up_items");

            migrationBuilder.DropColumn(
                name: "remaining_quantity",
                schema: "events",
                table: "sign_up_items");

            // NOTE: SignUpCommitments migration is NOT included in Phase 6A.121a
            // Changes to sign_up_commitments table deferred to Phase 6A.122
            // The domain entity has the fields, but database schema update comes later
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.121 Rollback: Restore original quantity-only schema

            // STEP 1: Add back old columns
            migrationBuilder.AddColumn<int>(
                name: "quantity",
                schema: "events",
                table: "sign_up_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "remaining_quantity",
                schema: "events",
                table: "sign_up_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // STEP 2: Data migration - Copy target_quantity back to quantity
            // Slot-based items will lose their slot count (converted to 0)
            migrationBuilder.Sql(@"
                UPDATE events.sign_up_items
                SET quantity = COALESCE(target_quantity, 0),
                    remaining_quantity = COALESCE(target_quantity, 0)
                WHERE target_quantity IS NOT NULL;
            ");

            // STEP 3: Drop CHECK constraint
            migrationBuilder.Sql(@"
                ALTER TABLE events.sign_up_items
                DROP CONSTRAINT IF EXISTS chk_sign_up_item_type;
            ");

            // STEP 4: Drop new columns
            migrationBuilder.DropColumn(
                name: "target_quantity",
                schema: "events",
                table: "sign_up_items");

            migrationBuilder.DropColumn(
                name: "available_slots",
                schema: "events",
                table: "sign_up_items");

            migrationBuilder.DropColumn(
                name: "suggested_per_slot",
                schema: "events",
                table: "sign_up_items");
        }
    }
}
