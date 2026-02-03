using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.98 Fix: Remove max-width:850px constraint from email templates.
    /// Makes templates fully responsive with auto-adjustable width based on device/viewport.
    /// 
    /// Changes:
    /// - Inner table: "width:850px;max-width:850px" → "width:100%"
    /// - Removes fixed width constraints for better responsiveness
    /// </summary>
    public partial class Phase6A98Fix_RemoveMaxWidthConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove "width:850px;max-width:850px;" and replace with "width:100%;"
            // This handles the inner content table
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'width:\s*850px\s*;\s*max-width:\s*850px\s*;?',
                    'width:100%;',
                    'gi'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%max-width:850px%'
                   OR html_template LIKE '%max-width: 850px%';
            ");

            // Also handle any standalone max-width:850px that might remain
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'max-width:\s*850px\s*;?',
                    '',
                    'gi'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%max-width:850px%'
                   OR html_template LIKE '%max-width: 850px%';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: Add back the max-width constraint
            // Find "width:100%;" on inner tables and restore to "width:850px;max-width:850px;"
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'style=""width:100%;background:#ffffff',
                    'style=""width:850px;max-width:850px;background:#ffffff',
                    'gi'
                ),
                updated_at = NOW();
            ");
        }
    }
}
