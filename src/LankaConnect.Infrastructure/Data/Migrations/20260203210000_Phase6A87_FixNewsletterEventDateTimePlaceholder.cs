using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.87: Fix Newsletter template EventDateTime placeholder mismatch.
    ///
    /// Root Cause Analysis:
    /// - Phase 6A.83 changed NewsletterEmailJob to pass `EventDateTime` parameter
    /// - But the Newsletter template (Phase6A.75) still uses `{{EventDate}}` placeholder
    /// - This mismatch causes literal `{{EventDate}}` to appear in emails
    ///
    /// Fix:
    /// - Update Newsletter template to use `{{EventDateTime}}` (consistent with other templates)
    /// - Other templates standardized in Phase 6A.96 already use `{{EventDateTime}}`
    /// </summary>
    public partial class Phase6A87_FixNewsletterEventDateTimePlaceholder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix HTML template - replace {{EventDate}} with {{EventDateTime}}
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, '{{EventDate}}', '{{EventDateTime}}'),
                    text_template = REPLACE(text_template, '{{EventDate}}', '{{EventDateTime}}'),
                    updated_at = NOW()
                WHERE name = 'newsletter';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back to {{EventDate}}
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, '{{EventDateTime}}', '{{EventDate}}'),
                    text_template = REPLACE(text_template, '{{EventDateTime}}', '{{EventDate}}'),
                    updated_at = NOW()
                WHERE name = 'newsletter';
            ");
        }
    }
}
