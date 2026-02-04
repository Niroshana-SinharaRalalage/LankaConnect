using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Fix Duplicate CTA Buttons in Email Templates
    ///
    /// Problem: Some templates have BOTH "View Event &amp; Register" AND "View Event Details" buttons
    /// pointing to the same URL ({{EventDetailsUrl}}), creating redundancy.
    ///
    /// Root Cause: ComprehensiveEmailLinkFix migration added "View Event Details" to templates
    /// that already had "View Event &amp; Register" from earlier migrations.
    ///
    /// Fix:
    /// 1. template-new-event-publication: KEEP "View Event &amp; Register", REMOVE "View Event Details"
    ///    (This email invites people to register - "View Event &amp; Register" is the correct CTA)
    ///
    /// 2. template-event-details-publication: REMOVE "View Event &amp; Register", KEEP "View Event Details"
    ///    (This is manual notification - "View Event Details" is more appropriate)
    ///
    /// 3. template-signup-list-commitment-confirmation: REMOVE "View Event &amp; Register", KEEP "View Event Details"
    ///    (User already committed - "View Event Details" is more appropriate)
    ///
    /// 4. template-signup-list-commitment-update: REMOVE "View Event &amp; Register", KEEP "View Event Details"
    ///    (User already committed - "View Event Details" is more appropriate)
    /// </summary>
    public partial class FixDuplicateCTAButtons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // 1. template-new-event-publication: REMOVE "View Event Details" button
            //    KEEP "View Event & Register" as this email invites people to register
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<!-- View Event Details CTA Button -->[\s\S]*?</table>\s*',
                    '',
                    'g'
                ),
                updated_at = NOW()
                WHERE name = 'template-new-event-publication'
                  AND html_template LIKE '%<!-- View Event Details CTA Button -->%';
            ");

            // ============================================================
            // 2. template-event-details-publication: REMOVE "View Event & Register" button
            //    KEEP "View Event Details" as this is a manual notification
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<table[^>]*role=""presentation""[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*',
                    '',
                    'g'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-details-publication'
                  AND (html_template LIKE '%View Event &amp; Register%'
                       OR html_template LIKE '%View Event & Register%');
            ");

            // ============================================================
            // 3. template-signup-list-commitment-confirmation: REMOVE "View Event & Register"
            //    KEEP "View Event Details" - user already committed
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<table[^>]*role=""presentation""[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*',
                    '',
                    'g'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND (html_template LIKE '%View Event &amp; Register%'
                       OR html_template LIKE '%View Event & Register%');
            ");

            // ============================================================
            // 4. template-signup-list-commitment-update: REMOVE "View Event & Register"
            //    KEEP "View Event Details" - user already committed
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<table[^>]*role=""presentation""[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*',
                    '',
                    'g'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND (html_template LIKE '%View Event &amp; Register%'
                       OR html_template LIKE '%View Event & Register%');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration would require restoring the removed buttons
            // This is a best-effort rollback - in practice, re-running the
            // original migrations would be needed to fully restore

            // Note: We don't restore the duplicate buttons as that was the bug
            // If rollback is truly needed, re-run:
            // - ComprehensiveEmailLinkFix for templates 2-4
            // - Original event-published seeding for template 1
        }
    }
}
