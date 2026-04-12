using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 7C — Part 2: Rebrand all email templates (excluding the 8 rebuilt in Phase7C_RebuildNewerTemplates).
    ///
    /// 7 passes applied to communications.email_templates:
    ///   Pass 1 — Logo table width  165 → 95  (HTML attribute + inline style)
    ///   Pass 2 — Logo title text   LankaConnect → LankaEvents  (anchored to font-size: 20px)
    ///   Pass 3 — Tagline text      Sri Lankan Community Hub → by LankaConnect  (+right-align, padding-top 5→3)
    ///   Pass 4 — Logo title weight font-weight: 500 → 700  (anchored to font-size: 20px context)
    ///   Pass 5 — Copyright         © 2026 LankaConnect. → © {{Year}} LankaEvents.
    ///   Pass 6 — Sign-off          The LankaConnect Team → The LankaEvents Team  (html + text columns)
    ///   Pass 7 — Title tag         &lt;title&gt;LankaConnect → &lt;title&gt;LankaEvents
    ///
    /// REGEXP_REPLACE is used (not REPLACE) wherever context-anchoring is needed to avoid false positives.
    /// </summary>
    public partial class Phase7C_ReбрандAllTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── reference_data timestamp updates (auto-generated) ─────────────
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1427));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1483));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1367));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1456));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1470));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1550));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1442));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1411));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1524));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1511));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1498));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1537));

            // ─────────────────────────────────────────────────────────────────────
            // Rebrand passes — all exclude the 8 templates fully rebuilt in
            // Phase7C_RebuildNewerTemplates (which already carry the new branding).
            // ─────────────────────────────────────────────────────────────────────

            // ── Pass 1: Logo table width  165 → 95 ───────────────────────────
            // Pre-flight confirmed width="165" is exclusively the logo text <table>.
            // Single nested REGEXP_REPLACE covers:
            //   (a) HTML attribute  width="165"       → width="95"
            //   (b) CSS inline      width: 165px      → width: 95px  (also catches min-width: 165px)
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    REGEXP_REPLACE(html_template, 'width=""165""', 'width=""95""', 'g'),
    'width: 165px', 'width: 95px', 'g'
)
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");

            // ── Pass 2: Logo title text  LankaConnect → LankaEvents ──────────
            // Anchored to font-size: 20px (the logo <p> style) to avoid touching
            // body text, URL slugs, or footer copyright lines.
            // Backreference \1 preserves the full style string up to the closing >.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    '(font-size: 20px;[^>]*)>LankaConnect',
    '\1>LankaEvents',
    'g'
)
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");

            // ── Pass 3: Tagline text + right-align ────────────────────────────
            // Changes:  Sri Lankan Community Hub → by LankaConnect
            //           padding-top: 5px         → padding-top: 3px
            //           (inserts)                   text-align: right
            // Pattern anchors to the tagline <p> CSS signature to avoid false positives.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    'padding-top: 5px;\s+display: block;"">Sri Lankan Community Hub',
    'padding-top: 3px; text-align: right; display: block;"">by LankaConnect',
    'g'
)
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");

            // ── Pass 4: Logo title font-weight  500 → 700 ────────────────────
            // Anchored to font-size: 20px; font-weight:  so that only the logo
            // title <p> is targeted (other 500-weight elements are not preceded
            // by font-size: 20px in the same style attribute).
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    '(font-size: 20px; font-weight: )500(;)',
    '\1700\2',
    'g'
)
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");

            // ── Pass 5: Copyright year + brand name ───────────────────────────
            // Handles both the HTML entity (&#169;) and the UTF-8 © character
            // to cover all template variations.  The \. escapes the literal dot.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    REGEXP_REPLACE(
        html_template,
        '&#169; 2026 LankaConnect\.',
        '&#169; {{Year}} LankaEvents.',
        'g'
    ),
    '© 2026 LankaConnect\.',
    '© {{Year}} LankaEvents.',
    'g'
)
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");

            // ── Pass 6: Sign-off  The LankaConnect Team → The LankaEvents Team ─
            // Applies to BOTH html_template and text_template columns.
            // Uses REPLACE (literal) — no regex anchoring needed for this phrase.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET
    html_template = REPLACE(html_template, 'The LankaConnect Team', 'The LankaEvents Team'),
    text_template = REPLACE(text_template, 'The LankaConnect Team', 'The LankaEvents Team')
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");

            // ── Pass 7: <title> tag brand name ────────────────────────────────
            // Replaces the HTML document <title> only.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = REPLACE(html_template, '<title>LankaConnect</title>', '<title>LankaEvents</title>')
WHERE name NOT IN (
    'template-addon-purchase-receipt',
    'template-collection-receipt',
    'template-sponsor-confirmation',
    'template-donation-refund',
    'template-collection-refund',
    'template-sponsor-refund',
    'template-donation-receipt',
    'template-photo-album-published'
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── reference_data rollback (auto-generated) ──────────────────────
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1360));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1417));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1328));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1386));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1397));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1489));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1371));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1456));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1444));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1429));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1478));

            // Rebranding changes are not automatically reversible.
            // Restore html_template / text_template from a DB snapshot or backup if rollback is needed.
            migrationBuilder.Sql(
                @"SELECT 1; -- Phase7C_ReбрандAllTemplates Down(): restore from DB snapshot if needed.");
        }
    }
}
