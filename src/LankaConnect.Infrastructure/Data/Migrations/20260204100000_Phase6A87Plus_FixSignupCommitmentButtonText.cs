using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.87+ Fix: Update button text in signup commitment templates from
    /// "View Event &amp; Register" to "View Event Details" for better UX clarity.
    ///
    /// The signup commitment confirmation email should show "View Event Details"
    /// since the user has already registered/committed - they're not registering.
    /// </summary>
    public partial class Phase6A87Plus_FixSignupCommitmentButtonText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix button text in signup commitment confirmation template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    'View Event &amp; Register',
                    'View Event Details'
                ),
                updated_at = NOW()
                WHERE name IN (
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update',
                    'template-signup-list-commitment-cancellation'
                )
                AND html_template LIKE '%View Event &amp; Register%';
            ");

            // Also fix text templates
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET text_template = REPLACE(
                    text_template,
                    'View Event & Register',
                    'View Event Details'
                ),
                updated_at = NOW()
                WHERE name IN (
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update',
                    'template-signup-list-commitment-cancellation'
                )
                AND text_template LIKE '%View Event & Register%';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert button text change
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    'View Event Details',
                    'View Event &amp; Register'
                ),
                updated_at = NOW()
                WHERE name IN (
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update',
                    'template-signup-list-commitment-cancellation'
                )
                AND html_template LIKE '%View Event Details%';
            ");
        }
    }
}
