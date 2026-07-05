using System;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 7C.2b Chunk 2b — rewrites the 6 registration/lifecycle email templates
    /// that currently embed a legacy flat location token (<c>{{EventLocation}}</c>
    /// for 5 of them and the non-canonical <c>{{Location}}</c> variant in
    /// <c>template-event-reminder</c>) to the shared
    /// <see cref="EmailLocationBlockHtml.DecomposedBlock"/>. Pairs with Chunk 2a
    /// (commit 93f83122) which extended the corresponding params classes with
    /// <c>WithLocationDetails(...)</c>.
    ///
    /// <para>Active REPLACEs (6 templates) + 1 no-op:</para>
    /// <list type="bullet">
    /// <item><c>template-paid-event-registration-confirmation-with-ticket</c> — REPLACE {{EventLocation}}, anchor {{UserName}}</item>
    /// <item><c>template-event-registration-cancellation</c> — REPLACE {{EventLocation}}, anchor {{UserName}}</item>
    /// <item><c>template-event-cancellation-notifications</c> — REPLACE {{EventLocation}}, anchor {{EventTitle}}
    ///     (bulk notification, greeting is hardcoded "Dear LankaConnect Community," with no personal token)</item>
    /// <item><c>template-event-approval</c> — REPLACE {{EventLocation}}, anchor {{OrganizerName}}</item>
    /// <item><c>template-event-reminder</c> — REPLACE {{Location}} (variant), anchor {{AttendeeName}}</item>
    /// <item><c>template-attendees-added-confirmation</c> — REPLACE {{EventLocation}}, anchor {{UserName}}</item>
    /// <item><c>template-preliminary-registration-payment-pending</c> — RAISE NOTICE no-op
    ///     (template never renders event location; out of scope for body rewrite. Staging
    ///     probe 2026-04-23 confirmed body contains no {{EventLocation}} / {{Location}}.
    ///     The Chunk 2a <c>WithLocationDetails(...)</c> addition on its params class is
    ///     harmless — the template simply ignores those keys.)</item>
    /// </list>
    ///
    /// <para>Safety mechanics (identical to Chunk 1's
    /// <c>20260422234334_Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates</c>):</para>
    /// <list type="number">
    /// <item>Chunk-scoped backup table <c>communications.email_templates_backup_phase7c3a</c>.
    /// Never shadow <c>_phase7c2</c> or <c>_phase7c2b</c>. Snapshots all 7 bodies including
    /// the no-op template for symmetry.</item>
    /// <item>Literal <c>REPLACE(html_template, '{{EventLocation}}' | '{{Location}}', :DecomposedBlock)</c>
    /// — no regex, MEMORY rule <c>feedback_regex_on_email_html.md</c>.</item>
    /// <item>5 post-UPDATE <c>RAISE EXCEPTION</c> invariants per active template:
    /// <c>ROW_COUNT = 1</c>, legacy token absent, <c>{{LocationName}}</c> present,
    /// template-specific anchor survives, <c>length(body) ≥ 50000</c>.</item>
    /// </list>
    /// </summary>
    public partial class Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates : Migration
    {
        private sealed record TemplateRewrite(string Name, string LegacyToken, string AnchorToken);

        /// <summary>
        /// Templates that carry a legacy flat location token and receive the REPLACE.
        /// <c>AnchorToken</c> is whatever Handlebars placeholder is guaranteed to remain
        /// present after the REPLACE — used purely as a body-integrity invariant; does not
        /// have to be a personal-greeting token (see event-cancellation-notifications,
        /// whose body uses hardcoded "Dear LankaConnect Community,").
        /// </summary>
        private static readonly TemplateRewrite[] ActiveRewrites = new[]
        {
            new TemplateRewrite("template-paid-event-registration-confirmation-with-ticket", "{{EventLocation}}", "{{UserName}}"),
            new TemplateRewrite("template-event-registration-cancellation",                  "{{EventLocation}}", "{{UserName}}"),
            new TemplateRewrite("template-event-cancellation-notifications",                 "{{EventLocation}}", "{{EventTitle}}"),
            new TemplateRewrite("template-event-approval",                                    "{{EventLocation}}", "{{OrganizerName}}"),
            new TemplateRewrite("template-event-reminder",                                    "{{Location}}",      "{{AttendeeName}}"),
            new TemplateRewrite("template-attendees-added-confirmation",                      "{{EventLocation}}", "{{UserName}}"),
        };

        /// <summary>
        /// Templates snapshotted into the backup table but NOT rewritten because
        /// their body does not render event location. RAISE NOTICE only.
        /// </summary>
        private static readonly string[] NoOpTemplates = new[]
        {
            "template-preliminary-registration-payment-pending",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — chunk-scoped backup table, snapshot all 7 bodies pre-UPDATE.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS communications.email_templates_backup_phase7c3a (
    id             uuid,
    name           text,
    html_template  text,
    backed_up_at   timestamptz NOT NULL DEFAULT NOW(),
    migration_name text        NOT NULL DEFAULT '20260423065018_Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates'
);

INSERT INTO communications.email_templates_backup_phase7c3a (id, name, html_template)
SELECT ""Id"", name, html_template
FROM communications.email_templates
WHERE name IN (
    'template-paid-event-registration-confirmation-with-ticket',
    'template-event-registration-cancellation',
    'template-event-cancellation-notifications',
    'template-event-approval',
    'template-event-reminder',
    'template-attendees-added-confirmation',
    'template-preliminary-registration-payment-pending'
);
");

            // Step 2 — per-template REPLACE + 5 invariants.
            var escapedBlock = EmailLocationBlockHtml.DecomposedBlock.Replace("'", "''");
            foreach (var r in ActiveRewrites)
            {
                var escapedName = r.Name.Replace("'", "''");
                // NOTE: we use string.Format-style {0}/{1}/{2}/{3} substitution. Arguments
                // are inserted verbatim (no re-escaping of Handlebars braces). Literal
                // braces in the format template are written as {{ / }}.
                var sql = string.Format(@"
DO $migration$
DECLARE
    rows_updated int;
    stored_body  text;
BEGIN
    UPDATE communications.email_templates
       SET html_template = REPLACE(html_template, '{1}', '{3}')
     WHERE name = '{0}'
       AND html_template LIKE '%{1}%';

    GET DIAGNOSTICS rows_updated = ROW_COUNT;
    IF rows_updated <> 1 THEN
        RAISE EXCEPTION 'Phase7C3a: expected 1 row updated for %, got % — body may already be decomposed or template missing',
            '{0}', rows_updated;
    END IF;

    SELECT html_template INTO stored_body
      FROM communications.email_templates
     WHERE name = '{0}';

    IF stored_body LIKE '%{1}%' THEN
        RAISE EXCEPTION 'Phase7C3a: % still contains legacy token {1} after REPLACE', '{0}';
    END IF;

    IF stored_body NOT LIKE '%{{{{LocationName}}}}%' THEN
        RAISE EXCEPTION 'Phase7C3a: % missing {{{{LocationName}}}} after REPLACE — DecomposedBlock did not land', '{0}';
    END IF;

    IF stored_body NOT LIKE '%{2}%' THEN
        RAISE EXCEPTION 'Phase7C3a: % lost anchor {2} after REPLACE — body truncation detected', '{0}';
    END IF;

    IF length(stored_body) < 50000 THEN
        RAISE EXCEPTION 'Phase7C3a: % body suspiciously small after REPLACE (% bytes)',
            '{0}', length(stored_body);
    END IF;
END $migration$;
",
                    escapedName,
                    r.LegacyToken,
                    r.AnchorToken,
                    escapedBlock);

                migrationBuilder.Sql(sql);
            }

            // Step 2b — no-op templates: explicit NOTICE so the migration log documents
            // why nothing was rewritten. Same pattern as Chunk 1 used for cancellation
            // templates that never contained {{EventLocation}}.
            foreach (var name in NoOpTemplates)
            {
                var escapedName = name.Replace("'", "''");
                migrationBuilder.Sql($@"
DO $migration$
DECLARE
    has_standard boolean;
    has_variant  boolean;
    body_len     int;
BEGIN
    SELECT
        (html_template LIKE '%{{{{EventLocation}}}}%'),
        (html_template LIKE '%{{{{Location}}}}%' AND html_template NOT LIKE '%{{{{EventLocation}}}}%'),
        length(html_template)
      INTO has_standard, has_variant, body_len
      FROM communications.email_templates
     WHERE name = '{escapedName}';

    IF has_standard OR has_variant THEN
        RAISE EXCEPTION 'Phase7C3a: no-op template % unexpectedly contains a legacy location token — scope assumption violated', '{escapedName}';
    END IF;

    RAISE NOTICE 'Phase7C3a: template % left untouched by design (length=%, has_legacy_location_token=false)',
        '{escapedName}', body_len;
END $migration$;
");
            }

            // Step 3 — EF-scaffolded seed-data timestamp drift (reference_data.reference_values).
            // These rows get their created_at bumped on every `migrations add` because
            // OnModelCreating uses DateTime.UtcNow for seed timestamps. Harmless — included
            // to keep the ModelSnapshot consistent.
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1849));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1978));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1756));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1909));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(2162));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1879));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(1816));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(2083));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(2052));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(2022));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 23, 6, 50, 11, 1, DateTimeKind.Utc).AddTicks(2132));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore from chunk-scoped backup, keyed by Id (quoted — PascalCase column).
            migrationBuilder.Sql(@"
DO $migration$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
         WHERE table_schema = 'communications'
           AND table_name   = 'email_templates_backup_phase7c3a'
    ) THEN
        RAISE NOTICE 'Phase7C3a Down: backup table not found, nothing to restore';
        RETURN;
    END IF;

    UPDATE communications.email_templates t
       SET html_template = b.html_template
      FROM communications.email_templates_backup_phase7c3a b
     WHERE t.""Id"" = b.id
       AND b.migration_name = '20260423065018_Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates';
END $migration$;
");

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
    }
}
