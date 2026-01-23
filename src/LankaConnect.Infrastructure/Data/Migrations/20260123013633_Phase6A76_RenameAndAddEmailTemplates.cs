using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.76: Email Template Standardization
    ///
    /// This migration:
    /// 1. Renames 14 existing templates from short names to template-* convention
    /// 2. Adds 5 missing templates that were causing silent email failures
    ///
    /// Template Rename Mapping:
    /// - registration-confirmation → template-free-event-registration-confirmation
    /// - event-published → template-new-event-publication
    /// - member-email-verification → template-membership-email-verification
    /// - event-cancelled-notification → template-event-cancellation-notifications
    /// - registration-cancellation → template-event-registration-cancellation
    /// - newsletter-confirmation → template-newsletter-subscription-confirmation
    /// - newsletter → template-newsletter-notification
    /// - event-details → template-event-details-publication
    /// - signup-commitment-confirmation → template-signup-list-commitment-confirmation
    /// - signup-commitment-updated → template-signup-list-commitment-update
    /// - signup-commitment-cancelled → template-signup-list-commitment-cancellation
    /// - event-approved → template-event-approval
    /// - event-reminder → template-event-reminder
    /// - ticket-confirmation → template-paid-event-registration-confirmation-with-ticket
    ///
    /// New Templates Added:
    /// - template-password-reset
    /// - template-password-change-confirmation
    /// - template-welcome
    /// - template-anonymous-rsvp-confirmation
    /// - template-organizer-role-approval
    /// </summary>
    public partial class Phase6A76_RenameAndAddEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // PART 1: RENAME ALL 14 EXISTING TEMPLATES
            // =====================================================

            // Rename registration-confirmation to template-free-event-registration-confirmation
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-free-event-registration-confirmation', updated_at = NOW()
                WHERE name = 'registration-confirmation';");

            // Rename event-published to template-new-event-publication
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-new-event-publication', updated_at = NOW()
                WHERE name = 'event-published';");

            // Rename member-email-verification to template-membership-email-verification
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-membership-email-verification', updated_at = NOW()
                WHERE name = 'member-email-verification';");

            // Rename event-cancelled-notification to template-event-cancellation-notifications
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-event-cancellation-notifications', updated_at = NOW()
                WHERE name = 'event-cancelled-notification';");

            // Rename registration-cancellation to template-event-registration-cancellation
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-event-registration-cancellation', updated_at = NOW()
                WHERE name = 'registration-cancellation';");

            // Rename newsletter-confirmation to template-newsletter-subscription-confirmation
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-newsletter-subscription-confirmation', updated_at = NOW()
                WHERE name = 'newsletter-confirmation';");

            // Rename newsletter to template-newsletter-notification
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-newsletter-notification', updated_at = NOW()
                WHERE name = 'newsletter';");

            // Rename event-details to template-event-details-publication
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-event-details-publication', updated_at = NOW()
                WHERE name = 'event-details';");

            // Rename signup-commitment-confirmation to template-signup-list-commitment-confirmation
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-signup-list-commitment-confirmation', updated_at = NOW()
                WHERE name = 'signup-commitment-confirmation';");

            // Rename signup-commitment-updated to template-signup-list-commitment-update
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-signup-list-commitment-update', updated_at = NOW()
                WHERE name = 'signup-commitment-updated';");

            // Rename signup-commitment-cancelled to template-signup-list-commitment-cancellation
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-signup-list-commitment-cancellation', updated_at = NOW()
                WHERE name = 'signup-commitment-cancelled';");

            // Rename event-approved to template-event-approval
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-event-approval', updated_at = NOW()
                WHERE name = 'event-approved';");

            // Rename event-reminder to template-event-reminder
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-event-reminder', updated_at = NOW()
                WHERE name = 'event-reminder';");

            // Rename ticket-confirmation to template-paid-event-registration-confirmation-with-ticket
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-paid-event-registration-confirmation-with-ticket', updated_at = NOW()
                WHERE name = 'ticket-confirmation';");

            // Also rename organizer-role-approved if it exists from old migration
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'template-organizer-role-approval', updated_at = NOW()
                WHERE name = 'organizer-role-approved';");

            // =====================================================
            // PART 2: ADD 5 MISSING TEMPLATES
            // =====================================================

            // Template 1: Password Reset
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates ("Id", "name", "description", "category", "type", "subject_template", "text_template", "html_template", "is_active", "created_at")
                SELECT
                    gen_random_uuid(),
                    'template-password-reset',
                    'Password reset email with secure token link',
                    'Transactional',
                    'PasswordReset',
                    'Reset Your Password - LankaConnect',
                    '<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Reset Your Password</title></head><body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;""><div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;""><h1 style=""color: white; margin: 0; font-size: 28px;"">Password Reset</h1></div><div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;""><p>Hello {{UserName}},</p><p>We received a request to reset your password. Click the button below to create a new password:</p><div style=""text-align: center; margin: 30px 0;""><a href=""{{ResetLink}}"" style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Reset Password</a></div><p style=""color: #666; font-size: 14px;"">This link will expire in 1 hour for security reasons.</p><p style=""color: #666; font-size: 14px;"">If you did not request this reset, please ignore this email or contact support if you have concerns.</p><hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;""><p style=""color: #999; font-size: 12px; text-align: center;"">© {{Year}} LankaConnect. All rights reserved.</p></div></body></html>',
                    'Hello {{UserName}}, We received a request to reset your password. Visit this link to create a new password: {{ResetLink}} This link will expire in 1 hour. If you did not request this reset, please ignore this email.',
                    true,

                    NOW()
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-password-reset');");

            // Template 2: Password Change Confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates ("Id", "name", "description", "category", "type", "subject_template", "text_template", "html_template", "is_active", "created_at")
                SELECT
                    gen_random_uuid(),
                    'template-password-change-confirmation',
                    'Confirmation email after successful password change',
                    'Transactional',
                    'PasswordChange',
                    'Your Password Has Been Changed - LankaConnect',
                    '<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Password Changed</title></head><body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;""><div style=""background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;""><h1 style=""color: white; margin: 0; font-size: 28px;"">Password Changed</h1></div><div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;""><p>Hello {{UserName}},</p><p>Your password has been successfully changed on {{ChangedAt}}.</p><p style=""color: #666;"">If you made this change, you can safely ignore this email.</p><p style=""color: #d9534f; font-weight: bold;"">If you did not make this change, please contact our support team immediately and secure your account.</p><hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;""><p style=""color: #999; font-size: 12px; text-align: center;"">© {{Year}} LankaConnect. All rights reserved.</p></div></body></html>',
                    'Hello {{UserName}}, Your password has been successfully changed on {{ChangedAt}}. If you did not make this change, please contact support immediately.',
                    true,

                    NOW()
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-password-change-confirmation');");

            // Template 3: Welcome Email
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates ("Id", "name", "description", "category", "type", "subject_template", "text_template", "html_template", "is_active", "created_at")
                SELECT
                    gen_random_uuid(),
                    'template-welcome',
                    'Welcome email for new user registration',
                    'Transactional',
                    'Welcome',
                    'Welcome to LankaConnect!',
                    '<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Welcome</title></head><body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;""><div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;""><h1 style=""color: white; margin: 0; font-size: 28px;"">Welcome to LankaConnect!</h1></div><div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;""><p>Hello {{UserName}},</p><p>Thank you for joining LankaConnect! We''re excited to have you as part of our community.</p><p>With LankaConnect, you can:</p><ul><li>Discover local events and activities</li><li>Connect with your community</li><li>Create and manage your own events</li><li>Stay updated with newsletters</li></ul><div style=""text-align: center; margin: 30px 0;""><a href=""{{DashboardUrl}}"" style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Get Started</a></div><p>If you have any questions, our support team is here to help.</p><hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;""><p style=""color: #999; font-size: 12px; text-align: center;"">© {{Year}} LankaConnect. All rights reserved.</p></div></body></html>',
                    'Hello {{UserName}}, Welcome to LankaConnect! Thank you for joining our community. Visit {{DashboardUrl}} to get started.',
                    true,

                    NOW()
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-welcome');");

            // Template 4: Anonymous RSVP Confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates ("Id", "name", "description", "category", "type", "subject_template", "text_template", "html_template", "is_active", "created_at")
                SELECT
                    gen_random_uuid(),
                    'template-anonymous-rsvp-confirmation',
                    'Confirmation email for anonymous event RSVP',
                    'Transactional',
                    'EventRegistration',
                    'Your RSVP Confirmation - {{EventTitle}}',
                    '<!DOCTYPE html><html><head><meta charset=""utf-8""><title>RSVP Confirmed</title></head><body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;""><div style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;""><h1 style=""color: white; margin: 0; font-size: 28px;"">RSVP Confirmed!</h1></div><div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;""><p>Hello,</p><p>Your RSVP for <strong>{{EventTitle}}</strong> has been confirmed!</p><div style=""background: white; padding: 20px; border-radius: 5px; margin: 20px 0;""><h3 style=""margin-top: 0;"">Event Details</h3><p><strong>Date:</strong> {{EventDate}}</p><p><strong>Time:</strong> {{EventTime}}</p><p><strong>Location:</strong> {{EventLocation}}</p>{{#if GuestCount}}<p><strong>Guests:</strong> {{GuestCount}}</p>{{/if}}</div><div style=""text-align: center; margin: 30px 0;""><a href=""{{EventUrl}}"" style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">View Event Details</a></div><p style=""color: #666; font-size: 14px;"">To modify or cancel your RSVP, use this link: <a href=""{{ManageRsvpUrl}}"">Manage RSVP</a></p><hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;""><p style=""color: #999; font-size: 12px; text-align: center;"">© {{Year}} LankaConnect. All rights reserved.</p></div></body></html>',
                    'Your RSVP for {{EventTitle}} has been confirmed! Event Date: {{EventDate}} Time: {{EventTime}} Location: {{EventLocation}} View event: {{EventUrl}} Manage RSVP: {{ManageRsvpUrl}}',
                    true,

                    NOW()
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-anonymous-rsvp-confirmation');");

            // Template 5: Organizer Role Approval
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates ("Id", "name", "description", "category", "type", "subject_template", "text_template", "html_template", "is_active", "created_at")
                SELECT
                    gen_random_uuid(),
                    'template-organizer-role-approval',
                    'Notification email when user is approved as an event organizer',
                    'Transactional',
                    'RoleApproval',
                    'Congratulations! You''re Now an Event Organizer - LankaConnect',
                    '<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Organizer Role Approved</title></head><body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;""><div style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;""><h1 style=""color: white; margin: 0; font-size: 28px;"">🎉 Congratulations!</h1></div><div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;""><p>Hello {{UserName}},</p><p>Great news! Your request to become an <strong>Event Organizer</strong> has been approved!</p><p>You now have access to:</p><ul><li>Create and publish events</li><li>Manage event registrations</li><li>Send notifications to attendees</li><li>Access organizer dashboard and analytics</li></ul><div style=""text-align: center; margin: 30px 0;""><a href=""{{DashboardUrl}}"" style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Go to Organizer Dashboard</a></div><p>We''re excited to see the amazing events you''ll create!</p><hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;""><p style=""color: #999; font-size: 12px; text-align: center;"">© {{Year}} LankaConnect. All rights reserved.</p></div></body></html>',
                    'Hello {{UserName}}, Congratulations! Your request to become an Event Organizer has been approved. You can now create events, manage registrations, and access the organizer dashboard at {{DashboardUrl}}.',
                    true,

                    NOW()
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-organizer-role-approval');");

            // Update reference_values timestamps (auto-generated by EF Core)
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1905));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1984));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1845));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1944));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1963));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(2121));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1925));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(1881));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(2037));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(2019));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(2002));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 1, 36, 31, 468, DateTimeKind.Utc).AddTicks(2054));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // REVERT PART 2: DELETE 5 NEW TEMPLATES
            // =====================================================
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-password-reset',
                    'template-password-change-confirmation',
                    'template-welcome',
                    'template-anonymous-rsvp-confirmation',
                    'template-organizer-role-approval'
                );");

            // =====================================================
            // REVERT PART 1: RENAME TEMPLATES BACK TO ORIGINAL NAMES
            // =====================================================

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'registration-confirmation', updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'event-published', updated_at = NOW()
                WHERE name = 'template-new-event-publication';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'member-email-verification', updated_at = NOW()
                WHERE name = 'template-membership-email-verification';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'event-cancelled-notification', updated_at = NOW()
                WHERE name = 'template-event-cancellation-notifications';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'registration-cancellation', updated_at = NOW()
                WHERE name = 'template-event-registration-cancellation';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'newsletter-confirmation', updated_at = NOW()
                WHERE name = 'template-newsletter-subscription-confirmation';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'newsletter', updated_at = NOW()
                WHERE name = 'template-newsletter-notification';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'event-details', updated_at = NOW()
                WHERE name = 'template-event-details-publication';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'signup-commitment-confirmation', updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'signup-commitment-updated', updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'signup-commitment-cancelled', updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-cancellation';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'event-approved', updated_at = NOW()
                WHERE name = 'template-event-approval';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'event-reminder', updated_at = NOW()
                WHERE name = 'template-event-reminder';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'ticket-confirmation', updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'organizer-role-approved', updated_at = NOW()
                WHERE name = 'template-organizer-role-approval';");

            // Revert reference_values timestamps
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(506));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(618));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(340));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(568));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(593));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(743));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(539));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(404));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(693));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(669));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(645));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(718));
        }
    }
}
