using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.98 Fix: Remove max-width:850px constraint from email templates.
    /// Makes templates fully responsive with auto-adjustable width based on device/viewport.
    ///
    /// Changes:
    /// - Inner table: "width: 850px; max-width: 850px;" → "width: 100%;"
    /// - Also removes standalone "max-width: 850px;" if present
    /// </summary>
    public partial class Phase6A98Fix_RemoveMaxWidthConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Replace "width: 850px; max-width: 850px;" with "width: 100%;"
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'width:\s*850px\s*;\s*max-width:\s*850px\s*;?',
                    'width: 100%;',
                    'gi'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%max-width: 850px%'
                   OR html_template LIKE '%max-width:850px%';
            ");

            // Step 2: Remove any remaining standalone "max-width: 850px;"
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'max-width:\s*850px\s*;?\s*',
                    '',
                    'gi'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%max-width: 850px%'
                   OR html_template LIKE '%max-width:850px%';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: Add back the max-width constraint
            // Note: This is a best-effort rollback - may not perfectly restore original formatting
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    'style=""width: 100%;(\s*)background',
                    'style=""width: 850px; max-width: 850px;\1background',
                    'gi'
                ),
                updated_at = NOW();
            ");
        }
    }
}
