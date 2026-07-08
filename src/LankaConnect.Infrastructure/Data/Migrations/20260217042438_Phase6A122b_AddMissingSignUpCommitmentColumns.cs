using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A122b_AddMissingSignUpCommitmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.122b: HOTFIX - Add missing columns to sign_up_commitments table.
            // ROOT CAUSE: Phase6A121_AddDualNullableFieldsToSignUpItems migration added columns to
            // sign_up_items (target_quantity, available_slots, suggested_per_slot) but MISSED adding
            // physical_quantity and slots_claimed to sign_up_commitments.
            // The EF Core model was updated (SignUpCommitment entity + configuration) but the migration
            // script was incomplete. The snapshot was updated so EF Core thinks columns exist,
            // but the actual database columns are missing.
            migrationBuilder.Sql(@"
ALTER TABLE events.sign_up_commitments
    ADD COLUMN IF NOT EXISTS physical_quantity integer,
    ADD COLUMN IF NOT EXISTS slots_claimed integer;
");

            // Phase 6A.122b: Migrate existing data from old quantity column (if it still exists)
            // Uses DO block to avoid errors if quantity column was already removed
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'events'
          AND table_name = 'sign_up_commitments'
          AND column_name = 'quantity'
    ) THEN
        UPDATE events.sign_up_commitments
        SET physical_quantity = quantity
        WHERE physical_quantity IS NULL;
    END IF;
END $$;
");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5564));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5709));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5515));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5599));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5617));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5797));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5581));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5545));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5765));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5748));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5727));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 4, 24, 34, 609, DateTimeKind.Utc).AddTicks(5782));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 431, DateTimeKind.Utc).AddTicks(9949));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(107));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 431, DateTimeKind.Utc).AddTicks(9890));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(58));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(78));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(214));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(39));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 431, DateTimeKind.Utc).AddTicks(9928));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(176));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(153));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(126));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 3, 34, 29, 432, DateTimeKind.Utc).AddTicks(196));
        }
    }
}
