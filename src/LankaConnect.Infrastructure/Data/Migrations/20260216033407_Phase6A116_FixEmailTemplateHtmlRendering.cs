using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.116: Fix HTML Rendering in Email Templates
    ///
    /// Problem: Email templates use double braces {{ResponseSummary}} which HTML-escapes the content.
    /// Users see literal <br/> tags instead of line breaks in form response summaries.
    ///
    /// Solution: Change {{ResponseSummary}} to {{{ResponseSummary}}} (triple braces) for raw HTML rendering.
    ///
    /// Templates Updated:
    /// 1. template-form-response-confirmation
    /// 2. template-form-response-update
    /// 3. template-form-response-cancellation
    /// 4. template-signup-list-commitment-confirmation (also has layout issues - Issue #11)
    /// 5. template-signup-list-commitment-update (also has layout issues - Issue #12)
    ///
    /// This uses PostgreSQL REPLACE() function to update templates directly in the database.
    /// </summary>
    public partial class Phase6A116_FixEmailTemplateHtmlRendering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix HTML rendering: Change {{ResponseSummary}} to {{{ResponseSummary}}} for raw HTML
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = REPLACE(html_template, '{{ResponseSummary}}', '{{{ResponseSummary}}}'),
                    updated_at = NOW()
                WHERE name IN (
                    'template-form-response-confirmation',
                    'template-form-response-update',
                    'template-form-response-cancellation',
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Change {{{ResponseSummary}}} back to {{ResponseSummary}}
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = REPLACE(html_template, '{{{ResponseSummary}}}', '{{ResponseSummary}}'),
                    updated_at = NOW()
                WHERE name IN (
                    'template-form-response-confirmation',
                    'template-form-response-update',
                    'template-form-response-cancellation',
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                );
            ");
        }
    }
}
