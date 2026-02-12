using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A106_FixNewsletterTemplateContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.106: Fix newsletter template to use correct content placeholder
            // The HTML template was using {{EventDescription}} (copy-paste error from event template)
            // Should use {{NewsletterContent}} to display the actual newsletter message
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = replace(
                    html_template,
                    '{{EventDescription}}',
                    '{{NewsletterContent}}'
                ),
                    updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                AND html_template LIKE '%{{EventDescription}}%';
            ");
        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.106: Rollback - revert newsletter template to previous (broken) state
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = replace(
                    html_template,
                    '{{NewsletterContent}}',
                    '{{EventDescription}}'
                ),
                    updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                AND html_template LIKE '%{{NewsletterContent}}%';
            ");
        }

    }
}
