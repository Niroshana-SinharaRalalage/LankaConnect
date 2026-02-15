using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.112/113: Update 19 email templates
    ///
    /// Changes:
    /// 1. Add "View Signup Forms" button to 14 event email templates
    /// 2. Remove "feel free" text from 8 templates
    ///
    /// Templates Updated:
    /// - template-account-activated-by-admin
    /// - template-account-unlocked-by-admin
    /// - template-attendees-added-confirmation
    /// - template-event-cancellation-notifications
    /// - template-event-details-publication
    /// - template-event-registration-cancellation
    /// - template-event-reminder
    /// - template-free-event-registration-confirmation
    /// - template-membership-email-verification
    /// - template-new-event-publication
    /// - template-newsletter-notification
    /// - template-paid-event-registration-confirmation-with-ticket
    /// - template-password-change-confirmation
    /// - template-password-reset
    /// - template-refund-completed
    /// - template-refund-requested
    /// - template-signup-list-commitment-cancellation
    /// - template-signup-list-commitment-confirmation
    /// - template-signup-list-commitment-update
    /// </summary>
    public partial class Phase6A113_UpdateEmailTemplatesWithSignupFormsButton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Get path to modified templates
            var projectRoot = FindProjectRoot();
            var templatePath = Path.Combine(projectRoot, "Template_Correction", "staging-phase6a113");

            // Update: template-account-activated-by-admin
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-account-activated-by-admin-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-account-activated-by-admin';
                ");
            }

            // Update: template-account-unlocked-by-admin
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-account-unlocked-by-admin-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-account-unlocked-by-admin';
                ");
            }

            // Update: template-attendees-added-confirmation
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-attendees-added-confirmation-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-attendees-added-confirmation';
                ");
            }

            // Update: template-event-cancellation-notifications
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-event-cancellation-notifications-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-event-cancellation-notifications';
                ");
            }

            // Update: template-event-details-publication
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-event-details-publication-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-event-details-publication';
                ");
            }

            // Update: template-event-registration-cancellation
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-event-registration-cancellation-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-event-registration-cancellation';
                ");
            }

            // Update: template-event-reminder
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-event-reminder-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-event-reminder';
                ");
            }

            // Update: template-free-event-registration-confirmation
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-free-event-registration-confirmation-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-free-event-registration-confirmation';
                ");
            }

            // Update: template-membership-email-verification
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-membership-email-verification-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-membership-email-verification';
                ");
            }

            // Update: template-new-event-publication
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-new-event-publication-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-new-event-publication';
                ");
            }

            // Update: template-newsletter-notification
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-newsletter-notification-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-newsletter-notification';
                ");
            }

            // Update: template-paid-event-registration-confirmation-with-ticket
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-paid-event-registration-confirmation-with-ticket-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
                ");
            }

            // Update: template-password-change-confirmation
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-password-change-confirmation-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-password-change-confirmation';
                ");
            }

            // Update: template-password-reset
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-password-reset-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-password-reset';
                ");
            }

            // Update: template-refund-completed
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-refund-completed-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-refund-completed';
                ");
            }

            // Update: template-refund-requested
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-refund-requested-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-refund-requested';
                ");
            }

            // Update: template-signup-list-commitment-cancellation
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-signup-list-commitment-cancellation-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-signup-list-commitment-cancellation';
                ");
            }

            // Update: template-signup-list-commitment-confirmation
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-signup-list-commitment-confirmation-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-signup-list-commitment-confirmation';
                ");
            }

            // Update: template-signup-list-commitment-update
            {
                var htmlContent = File.ReadAllText(Path.Combine(templatePath, "template-signup-list-commitment-update-modified.html"));
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{EscapeSql(htmlContent)}',
                        updated_at = NOW()
                    WHERE name = 'template-signup-list-commitment-update';
                ");
            }

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration not implemented - would need to restore from Phase 6A.96 templates
            migrationBuilder.Sql(@"
                -- Phase 6A.113 Down: Email template changes cannot be automatically reverted.
                -- To rollback, restore from database backup or re-run previous migration.
                SELECT 'Phase 6A.113 rollback: Email template changes cannot be automatically reverted.' as warning;
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
