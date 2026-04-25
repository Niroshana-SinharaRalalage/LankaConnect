using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 7C.2 — Bug B fix (five commitment templates): the EVENT DETAILS CARD
    /// renders <c>&lt;p&gt;{{EventLocation}}&lt;/p&gt;</c>, where <c>{{EventLocation}}</c>
    /// is bound to <c>EventLocation.ToString()</c> which appends a GPS-coordinate suffix
    /// (<c>"{Street}, {City}, {State}, {ZipCode}, {Country} ({Coordinates})"</c>). Admin UI
    /// + diaspora sync still depend on that <c>ToString()</c> shape, so we cannot change
    /// the value-object — we replace the template's <c>&lt;p&gt;</c> wrapper with the
    /// decomposed two-sibling-if block that the Phase 7C.2 projection populates:
    ///   - bold <c>{{LocationName}}</c> (when venue has a name)
    ///   - full 5-part <c>{{LocationAddress}}</c> (falls back to "Online Event")
    ///   - optional secondary location block
    ///
    /// This migration depends on the preceding migration
    /// <c>Phase7C2_RemoveDuplicateLocationFromSignupCommitmentTemplates</c> having
    /// removed the duplicate COMMITMENT DETAILS Location row, so each template now
    /// has exactly ONE <c>{{EventLocation}}</c> placeholder (in the EVENT DETAILS CARD).
    ///
    /// Per Phase 6A.117 rule: REGEXP_REPLACE (not REPLACE) because the <c>&lt;p style="..."&gt;</c>
    /// wrapper contains multi-line whitespace that varies across seed migrations. The
    /// two-sibling-if shape mirrors <c>Phase7C2_FreeEventTemplate_FixElseClause</c>
    /// (custom engine in <c>AzureEmailService.RenderTemplateContent</c> does NOT support
    /// <c>{{else}}</c>).
    /// </summary>
    public partial class Phase7C2_RewriteEventLocationInSignupCommitmentTemplates : Migration
    {
        // Matches the Location row's <p style="...">{{EventLocation}}</p> wrapper.
        // \s*>\s* covers the newline between the closing quote of style and the >,
        // and the newline around {{EventLocation}}.
        // [^"]+ is safe because the style attribute uses &quot; for embedded quotes.
        private const string EventLocationWrapperRegex =
            @"<p\s+style=""[^""]+""\s*>\s*\{\{EventLocation\}\}\s*</p>";

        // Phase 7C.2 decomposed block. No {{else}} — the custom template engine
        // strips falsy {{#if}}...{{/if}} blocks but does not branch on {{else}}.
        // LocationAddress is guaranteed non-empty by the Application-layer projection.
        private const string NewBlock =
            "{{#if HasLocationName}}" +
                "<span style=\"display:block;font-weight:700;color:#111827;font-size:14px;\">{{LocationName}}</span>" +
            "{{/if}}" +
            "<span style=\"display:block;font-weight:500;color:#374151;font-size:13px;margin-top:2px;\">{{LocationAddress}}</span>" +
            "{{#if HasSecondaryLocation}}" +
                "<span style=\"display:block;margin-top:14px;font-size:10px;font-weight:600;text-transform:uppercase;letter-spacing:1.2px;color:#9ca3af;\">{{SecondaryLocationLabel}}</span>" +
                "{{#if HasSecondaryLocationName}}" +
                    "<span style=\"display:block;font-weight:700;color:#111827;font-size:14px;margin-top:2px;\">{{SecondaryLocationName}}</span>" +
                "{{/if}}" +
                "<span style=\"display:block;font-weight:500;color:#374151;font-size:13px;margin-top:2px;\">{{SecondaryLocationAddress}}</span>" +
            "{{/if}}";

        private static readonly string[] TargetTemplates = new[]
        {
            "template-signup-list-commitment-confirmation",
            "template-signup-list-commitment-update",
            "template-signup-list-commitment-cancellation",
            "template-volunteer-commitment-confirmation",
            "template-volunteer-commitment-cancellation",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var newBlockEscaped = NewBlock.Replace("'", "''");

            foreach (var name in TargetTemplates)
            {
                migrationBuilder.Sql($@"
DO $migration$
DECLARE
    affected INT;
BEGIN
    UPDATE communications.email_templates
    SET html_template = REGEXP_REPLACE(
        html_template,
        '{EventLocationWrapperRegex}',
        '{newBlockEscaped}',
        'g'
    ),
    updated_at = NOW()
    WHERE name = '{name}';

    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected = 0 THEN
        RAISE EXCEPTION 'Phase 7C.2 rewrite-event-location migration: template ""{name}"" not found. Aborting.';
    END IF;

    -- Additional guard: verify the placeholder was actually rewritten.
    IF EXISTS (
        SELECT 1 FROM communications.email_templates
        WHERE name = '{name}'
          AND html_template LIKE '%{{{{EventLocation}}}}%'
    ) THEN
        RAISE EXCEPTION 'Phase 7C.2 rewrite-event-location migration: template ""{name}"" still contains {{{{EventLocation}}}} after rewrite — regex did not match. Aborting.';
    END IF;
END
$migration$;
");
            }

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(914));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(858));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(946));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(961));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(1091));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(930));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(898));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(1060));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(1043));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(1023));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 23, 20, 21, 839, DateTimeKind.Utc).AddTicks(1076));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 7C.2 — rolling back the template rewrite is intentionally a no-op.
            // The original <p style="...">{{EventLocation}}</p> wrapper carried multi-line
            // inline CSS whose exact whitespace varied across seed migrations, so a
            // faithful restore cannot be expressed as a single SQL string. If rollback
            // is ever needed, re-run the relevant seed migration for that template name.
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1205));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1122));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1268));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1296));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1548));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1237));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1174));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1492));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1461));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1423));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1521));
        }
    }
}
