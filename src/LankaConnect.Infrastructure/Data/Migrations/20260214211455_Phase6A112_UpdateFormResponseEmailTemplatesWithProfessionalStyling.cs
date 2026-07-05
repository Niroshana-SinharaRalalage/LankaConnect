using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.112: Update Form Response Email Templates with Professional Styling
    ///
    /// Replaces existing basic form response templates with professionally styled versions
    /// matching Phase 6A.96 signup list template standards:
    /// - Gradient header/footer (orange → red → green)
    /// - Responsive design (900px, 480px breakpoints)
    /// - MSO/Outlook compatibility
    /// - Conditional sections for event image, organizer contact, signup lists, signup forms
    ///
    /// Templates Updated:
    /// 1. template-form-response-confirmation - User submits a form response
    /// 2. template-form-response-update - User updates their response
    /// 3. template-form-response-cancellation - User deletes their response
    ///
    /// Changes from Phase 6A.108:
    /// - Removed FormDescription section (per user feedback)
    /// - Added "View Signup Lists" button (conditional)
    /// - Added "View Signup Forms" button (cancellation template only)
    /// - Updated edit button text to "Edit Your Response" with "You can edit your response at any time"
    /// - Removed "If you have questions, feel free to reply to this email." text
    /// - Fixed button colors: Orange (#ea580c) for both "View Signup List" and "View Signup Form"
    /// </summary>
    public partial class Phase6A112_UpdateFormResponseEmailTemplatesWithProfessionalStyling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Get the base path to Template_Correction folder
            var projectRoot = FindProjectRoot();
            var templatePath = Path.Combine(projectRoot, "Template_Correction", "staging");

            // Read the 3 modified HTML templates
            var confirmationHtml = File.ReadAllText(Path.Combine(templatePath, "template-form-response-confirmation-modified.html"));
            var updateHtml = File.ReadAllText(Path.Combine(templatePath, "template-form-response-update-modified.html"));
            var cancellationHtml = File.ReadAllText(Path.Combine(templatePath, "template-form-response-cancellation-modified.html"));

            // Update Template 1: Form Response Confirmation
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET
                    subject_template = '{{{{EventTitle}}}} - Form Response Received',
                    html_template = '{EscapeSql(confirmationHtml)}',
                    updated_at = NOW()
                WHERE name = 'template-form-response-confirmation';
            ");

            // Update Template 2: Form Response Update
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET
                    subject_template = '{{{{EventTitle}}}} - Form Response Updated',
                    html_template = '{EscapeSql(updateHtml)}',
                    updated_at = NOW()
                WHERE name = 'template-form-response-update';
            ");

            // Update Template 3: Form Response Cancellation
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET
                    subject_template = '{{{{EventTitle}}}} - Form Response Cancelled',
                    html_template = '{EscapeSql(cancellationHtml)}',
                    updated_at = NOW()
                WHERE name = 'template-form-response-cancellation';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration cannot automatically restore old templates
            // The old templates would need to be manually restored from backup or Phase 6A.108 migration
            migrationBuilder.Sql(@"
                -- Phase 6A.112 Down: Email template changes cannot be automatically reverted.
                -- To rollback, re-run Phase 6A.108 migration or restore from database backup.
                SELECT 'Phase 6A.112 rollback: Email template changes cannot be automatically reverted. Re-run Phase 6A.108 to restore basic templates.' as warning;
            ");
        }

        /// <summary>
        /// Finds the project root directory by searching for the .sln file.
        /// </summary>
        private string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);

            // Search up the directory tree for LankaConnect.sln
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "LankaConnect.sln")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new InvalidOperationException(
                    "Could not find project root (LankaConnect.sln). " +
                    $"Current directory: {currentDir}");
            }

            return dir.FullName;
        }

        /// <summary>
        /// Escapes single quotes for SQL string literals.
        /// </summary>
        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
