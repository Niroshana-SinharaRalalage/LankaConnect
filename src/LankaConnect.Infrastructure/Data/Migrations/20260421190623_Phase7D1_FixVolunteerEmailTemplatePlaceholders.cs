using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 7D.1 G14 — fix Handlebars placeholders in volunteer email templates.
    ///
    /// Root cause: the Phase 7D.1 seed migration (20260420175444_Phase7D1_SeedVolunteerEmailTemplates)
    /// used REGEXP_REPLACE(html_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g') to relabel visible wording
    /// when cloning the signup-list confirmation/cancellation templates. The regex was greedy and
    /// matched INSIDE Handlebars {{...}} tokens, so parameter names got rewritten:
    ///   {{SignupListUrl}}   → {{VolunteerListUrl}}
    ///   {{HasSignUpLists}}  → {{HasVolunteerLists}}  (and block forms)
    ///   {{SignupFormsUrl}}  → {{VolunteerFormsUrl}}
    ///   {{HasSignupForms}}  → {{HasVolunteerForms}}  (and block forms)
    /// But SignupCommitmentEmailParams.ToDictionary() still emits the ORIGINAL key names, so the
    /// custom Handlebars engine finds no match and delivers literal "{{VolunteerListUrl}}" in email.
    ///
    /// Fix: restore ToDictionary-compatible token names on both volunteer templates via narrow REPLACE.
    /// Row-count assertion per MEMORY Phase 6A.117 — prevents silent 0-row migration.
    /// </summary>
    public partial class Phase7D1_FixVolunteerEmailTemplatePlaceholders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Both volunteer templates share the same broken-token set. A single UPDATE per template
            // chains the REPLACEs across html_template, text_template, subject_template.
            migrationBuilder.Sql(@"
                DO $migration$
                DECLARE affected INT;
                BEGIN
                    UPDATE communications.email_templates
                    SET
                        html_template = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                            html_template,
                            '{{#HasVolunteerLists}}', '{{#HasSignUpLists}}'),
                            '{{/HasVolunteerLists}}', '{{/HasSignUpLists}}'),
                            '{{HasVolunteerLists}}',  '{{HasSignUpLists}}'),
                            '{{VolunteerListUrl}}',   '{{SignupListUrl}}'),
                            '{{#HasVolunteerForms}}', '{{#HasSignupForms}}'),
                            '{{/HasVolunteerForms}}', '{{/HasSignupForms}}'),
                            '{{HasVolunteerForms}}',  '{{HasSignupForms}}'),
                            '{{VolunteerFormsUrl}}',  '{{SignupFormsUrl}}'),
                        text_template = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                            text_template,
                            '{{#HasVolunteerLists}}', '{{#HasSignUpLists}}'),
                            '{{/HasVolunteerLists}}', '{{/HasSignUpLists}}'),
                            '{{HasVolunteerLists}}',  '{{HasSignUpLists}}'),
                            '{{VolunteerListUrl}}',   '{{SignupListUrl}}'),
                            '{{#HasVolunteerForms}}', '{{#HasSignupForms}}'),
                            '{{/HasVolunteerForms}}', '{{/HasSignupForms}}'),
                            '{{HasVolunteerForms}}',  '{{HasSignupForms}}'),
                            '{{VolunteerFormsUrl}}',  '{{SignupFormsUrl}}'),
                        subject_template = REPLACE(REPLACE(REPLACE(REPLACE(
                            subject_template,
                            '{{VolunteerListUrl}}',  '{{SignupListUrl}}'),
                            '{{HasVolunteerLists}}', '{{HasSignUpLists}}'),
                            '{{VolunteerFormsUrl}}', '{{SignupFormsUrl}}'),
                            '{{HasVolunteerForms}}', '{{HasSignupForms}}')
                    WHERE name = 'template-volunteer-commitment-confirmation'
                      AND (
                           html_template LIKE '%{{VolunteerListUrl}}%'
                        OR html_template LIKE '%HasVolunteerLists%'
                        OR html_template LIKE '%{{VolunteerFormsUrl}}%'
                        OR html_template LIKE '%HasVolunteerForms%'
                        OR text_template LIKE '%{{VolunteerListUrl}}%'
                        OR text_template LIKE '%{{VolunteerFormsUrl}}%'
                      );
                    GET DIAGNOSTICS affected = ROW_COUNT;
                    IF affected = 0 THEN
                        RAISE EXCEPTION 'Phase 7D.1 G14: template-volunteer-commitment-confirmation did not contain the broken Handlebars tokens — migration not applied (expected 1 row).';
                    END IF;

                    UPDATE communications.email_templates
                    SET
                        html_template = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                            html_template,
                            '{{#HasVolunteerLists}}', '{{#HasSignUpLists}}'),
                            '{{/HasVolunteerLists}}', '{{/HasSignUpLists}}'),
                            '{{HasVolunteerLists}}',  '{{HasSignUpLists}}'),
                            '{{VolunteerListUrl}}',   '{{SignupListUrl}}'),
                            '{{#HasVolunteerForms}}', '{{#HasSignupForms}}'),
                            '{{/HasVolunteerForms}}', '{{/HasSignupForms}}'),
                            '{{HasVolunteerForms}}',  '{{HasSignupForms}}'),
                            '{{VolunteerFormsUrl}}',  '{{SignupFormsUrl}}'),
                        text_template = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                            text_template,
                            '{{#HasVolunteerLists}}', '{{#HasSignUpLists}}'),
                            '{{/HasVolunteerLists}}', '{{/HasSignUpLists}}'),
                            '{{HasVolunteerLists}}',  '{{HasSignUpLists}}'),
                            '{{VolunteerListUrl}}',   '{{SignupListUrl}}'),
                            '{{#HasVolunteerForms}}', '{{#HasSignupForms}}'),
                            '{{/HasVolunteerForms}}', '{{/HasSignupForms}}'),
                            '{{HasVolunteerForms}}',  '{{HasSignupForms}}'),
                            '{{VolunteerFormsUrl}}',  '{{SignupFormsUrl}}'),
                        subject_template = REPLACE(REPLACE(REPLACE(REPLACE(
                            subject_template,
                            '{{VolunteerListUrl}}',  '{{SignupListUrl}}'),
                            '{{HasVolunteerLists}}', '{{HasSignUpLists}}'),
                            '{{VolunteerFormsUrl}}', '{{SignupFormsUrl}}'),
                            '{{HasVolunteerForms}}', '{{HasSignupForms}}')
                    WHERE name = 'template-volunteer-commitment-cancellation'
                      AND (
                           html_template LIKE '%{{VolunteerListUrl}}%'
                        OR html_template LIKE '%HasVolunteerLists%'
                        OR html_template LIKE '%{{VolunteerFormsUrl}}%'
                        OR html_template LIKE '%HasVolunteerForms%'
                        OR text_template LIKE '%{{VolunteerListUrl}}%'
                        OR text_template LIKE '%{{VolunteerFormsUrl}}%'
                      );
                    GET DIAGNOSTICS affected = ROW_COUNT;
                    IF affected = 0 THEN
                        RAISE EXCEPTION 'Phase 7D.1 G14: template-volunteer-commitment-cancellation did not contain the broken Handlebars tokens — migration not applied (expected 1 row).';
                    END IF;
                END $migration$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback-symmetry: reverse the REPLACEs, restoring the broken Handlebars tokens.
            // Not a useful state to roll back to (emails broken), but required for migration parity.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                        html_template,
                        '{{#HasSignUpLists}}', '{{#HasVolunteerLists}}'),
                        '{{/HasSignUpLists}}', '{{/HasVolunteerLists}}'),
                        '{{HasSignUpLists}}',  '{{HasVolunteerLists}}'),
                        '{{SignupListUrl}}',   '{{VolunteerListUrl}}'),
                        '{{#HasSignupForms}}', '{{#HasVolunteerForms}}'),
                        '{{/HasSignupForms}}', '{{/HasVolunteerForms}}'),
                        '{{HasSignupForms}}',  '{{HasVolunteerForms}}'),
                        '{{SignupFormsUrl}}',  '{{VolunteerFormsUrl}}'),
                    text_template = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                        text_template,
                        '{{#HasSignUpLists}}', '{{#HasVolunteerLists}}'),
                        '{{/HasSignUpLists}}', '{{/HasVolunteerLists}}'),
                        '{{HasSignUpLists}}',  '{{HasVolunteerLists}}'),
                        '{{SignupListUrl}}',   '{{VolunteerListUrl}}'),
                        '{{#HasSignupForms}}', '{{#HasVolunteerForms}}'),
                        '{{/HasSignupForms}}', '{{/HasVolunteerForms}}'),
                        '{{HasSignupForms}}',  '{{HasVolunteerForms}}'),
                        '{{SignupFormsUrl}}',  '{{VolunteerFormsUrl}}'),
                    subject_template = REPLACE(REPLACE(REPLACE(REPLACE(
                        subject_template,
                        '{{SignupListUrl}}',  '{{VolunteerListUrl}}'),
                        '{{HasSignUpLists}}', '{{HasVolunteerLists}}'),
                        '{{SignupFormsUrl}}', '{{VolunteerFormsUrl}}'),
                        '{{HasSignupForms}}', '{{HasVolunteerForms}}')
                WHERE name IN (
                    'template-volunteer-commitment-confirmation',
                    'template-volunteer-commitment-cancellation'
                );
            ");
        }
    }
}
