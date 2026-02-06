using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A74Part9BC_FixInvalidNewsletterStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.74 Part 9B/9C: Fix invalid newsletter status values
            // NOTE: status column is VARCHAR (enum stored as string), not integer!
            // Fix newsletters with status='1' or any other invalid string value
            // Set status based on PublishedAt, ExpiresAt, and SentAt dates
            migrationBuilder.Sql(@"
                UPDATE communications.newsletters
                SET status = CASE
                    WHEN sent_at IS NOT NULL THEN 'Sent'
                    WHEN published_at IS NULL THEN 'Draft'
                    WHEN expires_at IS NOT NULL AND expires_at < CURRENT_TIMESTAMP THEN 'Inactive'
                    WHEN published_at IS NOT NULL THEN 'Active'
                    ELSE 'Draft'  -- Default to Draft
                END
                WHERE status NOT IN ('Draft', 'Active', 'Inactive', 'Sent');
            ");

            // Phase 6A.74 Part 9C: Add check constraint to prevent future invalid status values
            // Valid status values: 'Draft', 'Active', 'Inactive', 'Sent' (strings, not integers)
            migrationBuilder.Sql(@"
                ALTER TABLE communications.newsletters
                ADD CONSTRAINT ck_newsletters_status_valid
                CHECK (status IN ('Draft', 'Active', 'Inactive', 'Sent'));
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(7885));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(8015));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(7768));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(7953));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(7984));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(8134));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(7920));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(7836));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(8092));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(8059));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(8038));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 1, 38, 35, 441, DateTimeKind.Utc).AddTicks(8113));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.74 Part 9C: Remove check constraint
            migrationBuilder.Sql(@"
                ALTER TABLE communications.newsletters
                DROP CONSTRAINT IF EXISTS ck_newsletters_status_valid;
            ");

            // Note: Cannot rollback status fix as we don't have historical data
            // Newsletters that were fixed will remain in their corrected state

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(3700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4290));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(3629));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4136));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4254));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4461));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(3721));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(3677));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4381));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4351));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4321));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 14, 34, 34, 60, DateTimeKind.Utc).AddTicks(4430));
        }
    }
}
