using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.96 Fix: Remove remaining old footer text from templates.
    /// The original migration didn't match the exact format of the footer text.
    /// </summary>
    public partial class Phase6A96Fix_RemoveOldFooterText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the old footer text with various patterns
            // Pattern 1: With <p> tag and specific styling (font-size: 13px)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<p[^>]*>\s*This email was sent by LankaConnect\.?\s*If you have any questions,\s*please contact your event organizer\.?\s*</p>',
                    '',
                    'gi'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%This email was sent by LankaConnect%';
            ");

            // Pattern 2: Any remaining instances without exact <p> tags
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'This email was sent by LankaConnect\.?\s*If you have any questions,\s*please contact your event organizer\.?',
                    '',
                    'gi'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%This email was sent by LankaConnect%';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore deleted text
        }
    }
}
