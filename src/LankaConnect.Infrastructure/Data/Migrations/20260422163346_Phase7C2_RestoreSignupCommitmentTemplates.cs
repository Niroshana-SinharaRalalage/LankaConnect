using LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 7C.2 recovery — restores the authoritative pre-damage bodies for the five
    /// signup/volunteer commitment email templates. Migration
    /// 20260421213355_Phase7C2_RemoveDuplicateLocationFromSignupCommitmentTemplates used
    /// an over-greedy REGEXP_REPLACE anchored on "Event Date" that ate the entire top
    /// half of the HTML (banner, greeting, commitment-details card) in staging DB.
    ///
    /// Damage scope (staging only — prod never received the broken migration):
    /// - template-signup-list-commitment-confirmation: DAMAGED
    /// - template-signup-list-commitment-update:       DAMAGED
    /// - template-volunteer-commitment-confirmation:   DAMAGED
    /// - template-signup-list-commitment-cancellation: UNTOUCHED (regex required the
    ///   "Event Date" + {{EventLocation}} rows which cancellation bodies never contained)
    /// - template-volunteer-commitment-cancellation:   UNTOUCHED (same reason)
    ///
    /// All five are UPDATEd for idempotency and contract symmetry (cancellations are
    /// self-set to their known-good body so re-running the migration is deterministic).
    ///
    /// Recovery mechanics:
    /// 1. Create backup table <c>communications.email_templates_backup_phase7c2</c>
    ///    and snapshot the current damaged body before touching it.
    /// 2. UPDATE html_template from the embedded pre-damage HTML (loaded via
    ///    <see cref="Phase7C2RecoveryTemplates"/> — no File.ReadAllText, MEMORY 6A.129b).
    /// 3. Post-UPDATE: assert 1 row matched per template and that the stored body
    ///    contains <c>{{UserName}}</c> (i.e. the greeting survived the write) —
    ///    aborts the migration with RAISE EXCEPTION if either check fails.
    /// </summary>
    public partial class Phase7C2_RestoreSignupCommitmentTemplates : Migration
    {
        private static readonly (string Name, bool WasDamaged)[] Templates = new[]
        {
            ("template-signup-list-commitment-confirmation", true),
            ("template-signup-list-commitment-update",       true),
            ("template-signup-list-commitment-cancellation", false),
            ("template-volunteer-commitment-confirmation",   true),
            ("template-volunteer-commitment-cancellation",   false),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS communications.email_templates_backup_phase7c2 (
    id                   uuid,
    name                 text,
    html_template        text,
    backed_up_at         timestamptz NOT NULL DEFAULT NOW(),
    migration_name       text        NOT NULL DEFAULT '20260422163346_Phase7C2_RestoreSignupCommitmentTemplates'
);

INSERT INTO communications.email_templates_backup_phase7c2 (id, name, html_template)
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

            foreach (var (name, _) in Templates)
            {
                var html = Phase7C2RecoveryTemplates.LoadHtml(name);
                var escapedHtml = html.Replace("'", "''");
                var escapedName = name.Replace("'", "''");

                migrationBuilder.Sql($@"
DO $$
DECLARE
    rows_updated int;
    stored_body  text;
BEGIN
    UPDATE communications.email_templates
       SET html_template = '{escapedHtml}'
     WHERE name = '{escapedName}';

    GET DIAGNOSTICS rows_updated = ROW_COUNT;
    IF rows_updated <> 1 THEN
        RAISE EXCEPTION 'Phase7C2_Restore: expected 1 row updated for %, got %',
            '{escapedName}', rows_updated;
    END IF;

    SELECT html_template INTO stored_body
      FROM communications.email_templates
     WHERE name = '{escapedName}';

    IF stored_body NOT LIKE '%{{{{UserName}}}}%' THEN
        RAISE EXCEPTION
            'Phase7C2_Restore: % missing greeting token {{{{UserName}}}} after UPDATE',
            '{escapedName}';
    END IF;

    IF length(stored_body) < 50000 THEN
        RAISE EXCEPTION
            'Phase7C2_Restore: % body suspiciously small after UPDATE (% bytes)',
            '{escapedName}', length(stored_body);
    END IF;
END $$;
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
         WHERE table_schema = 'communications'
           AND table_name   = 'email_templates_backup_phase7c2'
    ) THEN
        RAISE NOTICE 'Phase7C2_Restore Down: backup table not found, nothing to restore';
        RETURN;
    END IF;

    UPDATE communications.email_templates t
       SET html_template = b.html_template
      FROM communications.email_templates_backup_phase7c2 b
     WHERE t.""Id"" = b.id
       AND b.migration_name = '20260422163346_Phase7C2_RestoreSignupCommitmentTemplates';
END $$;
");
        }
    }
}
