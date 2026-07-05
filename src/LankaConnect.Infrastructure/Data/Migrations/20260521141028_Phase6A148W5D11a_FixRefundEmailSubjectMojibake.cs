using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.148.W5.D11.a: Fix UTF-8 mojibake in 4 refund-workflow email subject_template
    /// rows. Original D7/D13 migrations used em-dash characters that got encoded as CP-1252
    /// and stored as U+FFFD (replacement char) — visible as <c>?</c> in subject lines:
    ///
    ///   "Your Refund Decision ? {{EventTitle}}"               ? should be U+2014 em-dash
    ///   "Refund Request Received ? Pending Organizer Review ? {{EventTitle}}"
    ///   "Refund Request Declined ? {{EventTitle}}"
    ///   "Refund Request Withdrawn ? {{EventTitle}}"
    ///
    /// This migration restores proper em-dash using Unicode escape (—) in the C# source
    /// so the encoding survives editor/CI roundtrips. Verified at the byte level — em-dash
    /// in UTF-8 is the 3-byte sequence 0xE2 0x80 0x94.
    ///
    /// W5.D11.b (separate follow-up) will rewrite the html_template bodies with full
    /// LankaConnect brand parity to match template-event-registration-cancellation.
    ///
    /// Idempotent — UPDATEs overwrite whatever's currently stored.
    /// </summary>
    public partial class Phase6A148W5D11a_FixRefundEmailSubjectMojibake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5757));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5860));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5802));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5824));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5977));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5780));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5734));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5913));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5882));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5956));

            // === W5.D11.a: fix UTF-8 mojibake in 4 refund-workflow subject_template rows ===
            // Em-dash (—) is U+2014. Stored at byte level as 0xE2 0x80 0x94 in UTF-8.
            // Using — Unicode escape keeps the encoding intact across editors/CI.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET subject_template = 'Your Refund Decision — {{EventTitle}}',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE name = 'template-refund-decision';

UPDATE communications.email_templates
SET subject_template = 'Refund Request Received — Pending Organizer Review — {{EventTitle}}',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE name = 'template-refund-pending-review';

UPDATE communications.email_templates
SET subject_template = 'Refund Request Declined — {{EventTitle}}',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE name = 'template-refund-rejected';

UPDATE communications.email_templates
SET subject_template = 'Refund Request Withdrawn — {{EventTitle}}',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE name = 'template-refund-withdrawn';
");
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
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3057));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3175));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(2956));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3116));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3146));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3325));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3085));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3026));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3267));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3241));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3203));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3296));
        }
    }
}
