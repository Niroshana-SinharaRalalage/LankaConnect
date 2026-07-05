using System.Collections.Generic;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 7C.2b Chunk 1 — re-applies the decomposed-location Handlebars block
    /// that was erased from the 5 signup/volunteer commitment templates when the
    /// 2026-04-22 recovery migration
    /// <c>20260422163346_Phase7C2_RestoreSignupCommitmentTemplates</c> restored their
    /// bodies from pre-rewrite embedded resources. The original rewrite migration
    /// <c>20260421232025_Phase7C2_RewriteEventLocationInSignupCommitmentTemplates</c>
    /// is still listed in <c>__EFMigrationsHistory</c>, so EF cannot re-run it —
    /// this brand-new forward migration is the correct recovery pattern.
    ///
    /// <para>Targeted templates (3 active REPLACEs, 2 cancellation no-ops):</para>
    /// <list type="bullet">
    /// <item>template-signup-list-commitment-confirmation — UPDATE ( {{EventLocation}} → DecomposedBlock )</item>
    /// <item>template-signup-list-commitment-update — UPDATE</item>
    /// <item>template-volunteer-commitment-confirmation — UPDATE</item>
    /// <item>template-signup-list-commitment-cancellation — NOTICE (no {{EventLocation}} by design)</item>
    /// <item>template-volunteer-commitment-cancellation — NOTICE (same)</item>
    /// </list>
    ///
    /// <para>Safety mechanics (MEMORY <c>feedback_regex_on_email_html.md</c> rule):</para>
    /// <list type="number">
    /// <item>Chunk-scoped backup table <c>communications.email_templates_backup_phase7c2b</c>
    /// — never shadow the existing <c>_phase7c2</c> backup; scoping lesson from the
    /// 2026-04-22 recovery overshoot.</item>
    /// <item>Literal <c>REPLACE(html_template, '{{EventLocation}}', :DecomposedBlock)</c>
    /// against the unique flat token. No regex anywhere.</item>
    /// <item>5 post-UPDATE invariants per active template, each <c>RAISE EXCEPTION</c>
    /// on failure: <c>ROW_COUNT = 1</c>, body no longer contains the legacy token,
    /// body contains <c>{{LocationName}}</c>, body contains <c>{{UserName}}</c>,
    /// body length ≥ 50000.</item>
    /// </list>
    /// </summary>
    public partial class Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates : Migration
    {
        private static readonly string[] ActiveTemplates = new[]
        {
            "template-signup-list-commitment-confirmation",
            "template-signup-list-commitment-update",
            "template-volunteer-commitment-confirmation",
        };

        private static readonly string[] CancellationTemplates = new[]
        {
            "template-signup-list-commitment-cancellation",
            "template-volunteer-commitment-cancellation",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — chunk-scoped backup table. Pre-UPDATE snapshot so Down() is
            // a simple UPDATE … FROM … restore keyed by Id.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS communications.email_templates_backup_phase7c2b (
    id             uuid,
    name           text,
    html_template  text,
    backed_up_at   timestamptz NOT NULL DEFAULT NOW(),
    migration_name text        NOT NULL DEFAULT '20260422234334_Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates'
);

INSERT INTO communications.email_templates_backup_phase7c2b (id, name, html_template)
SELECT ""Id"", name, html_template
FROM communications.email_templates
WHERE name IN (
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',
    'template-volunteer-commitment-confirmation',
    'template-volunteer-commitment-cancellation'
);
");

            // Step 2 — active REPLACE per template + 5 post-UPDATE invariants each.
            var escapedBlock = EmailLocationBlockHtml.DecomposedBlock.Replace("'", "''");
            foreach (var name in ActiveTemplates)
            {
                var escapedName = name.Replace("'", "''");
                migrationBuilder.Sql($@"
DO $migration$
DECLARE
    rows_updated int;
    stored_body  text;
BEGIN
    UPDATE communications.email_templates
       SET html_template = REPLACE(html_template, '{{{{EventLocation}}}}', '{escapedBlock}')
     WHERE name = '{escapedName}'
       AND html_template LIKE '%{{{{EventLocation}}}}%';

    GET DIAGNOSTICS rows_updated = ROW_COUNT;
    IF rows_updated <> 1 THEN
        RAISE EXCEPTION 'Phase7C2b: expected 1 row updated for %, got % — body may already be decomposed or template missing',
            '{escapedName}', rows_updated;
    END IF;

    SELECT html_template INTO stored_body
      FROM communications.email_templates
     WHERE name = '{escapedName}';

    IF stored_body LIKE '%{{{{EventLocation}}}}%' THEN
        RAISE EXCEPTION 'Phase7C2b: % still contains {{{{EventLocation}}}} after REPLACE', '{escapedName}';
    END IF;

    IF stored_body NOT LIKE '%{{{{LocationName}}}}%' THEN
        RAISE EXCEPTION 'Phase7C2b: % missing {{{{LocationName}}}} after REPLACE — DecomposedBlock did not land', '{escapedName}';
    END IF;

    IF stored_body NOT LIKE '%{{{{UserName}}}}%' THEN
        RAISE EXCEPTION 'Phase7C2b: % lost {{{{UserName}}}} greeting after REPLACE — body truncation detected', '{escapedName}';
    END IF;

    IF length(stored_body) < 50000 THEN
        RAISE EXCEPTION 'Phase7C2b: % body suspiciously small after REPLACE (% bytes)',
            '{escapedName}', length(stored_body);
    END IF;
END $migration$;
");
            }

            // Step 3 — cancellation templates: explicit no-op with a NOTICE so the
            // migration log documents why nothing changed (they never contained
            // {{EventLocation}} in the first place).
            foreach (var name in CancellationTemplates)
            {
                var escapedName = name.Replace("'", "''");
                migrationBuilder.Sql($@"
DO $migration$
DECLARE
    has_token boolean;
    body_len  int;
BEGIN
    SELECT
        (html_template LIKE '%{{{{EventLocation}}}}%'),
        length(html_template)
      INTO has_token, body_len
      FROM communications.email_templates
     WHERE name = '{escapedName}';

    IF has_token THEN
        RAISE EXCEPTION 'Phase7C2b: cancellation template % unexpectedly contains {{{{EventLocation}}}} — scope assumption violated', '{escapedName}';
    END IF;

    RAISE NOTICE 'Phase7C2b: cancellation template % left untouched by design (length=%, has_legacy_token=false)',
        '{escapedName}', body_len;
END $migration$;
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore from the chunk-scoped backup table. Keyed by Id for safety —
            // even if a template name changes later, the backup row pairs to the
            // same primary key.
            migrationBuilder.Sql(@"
DO $migration$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
         WHERE table_schema = 'communications'
           AND table_name   = 'email_templates_backup_phase7c2b'
    ) THEN
        RAISE NOTICE 'Phase7C2b Down: backup table not found, nothing to restore';
        RETURN;
    END IF;

    UPDATE communications.email_templates t
       SET html_template = b.html_template
      FROM communications.email_templates_backup_phase7c2b b
     WHERE t.""Id"" = b.id
       AND b.migration_name = '20260422234334_Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates';
END $migration$;
");
        }
    }
}
