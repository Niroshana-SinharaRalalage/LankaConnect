using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.101: Comprehensive idempotent migration to sync production database to match staging.
    ///
    /// This migration handles:
    /// 1. Drop obsolete version columns from communications tables
    /// 2. Create missing tables (event_reminders_sent, event_notification_history)
    /// 3. Ensure all 12 EventCategory reference values exist with deterministic GUIDs
    /// 4. Ensure all 29 email templates exist
    ///
    /// All operations are idempotent (safe to run multiple times).
    /// </summary>
    public partial class Phase6A101_SyncProductionDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // PART 1: Drop obsolete version columns (idempotent)
            // ============================================================
            migrationBuilder.Sql(@"
                ALTER TABLE communications.newsletter_subscribers DROP COLUMN IF EXISTS version;
                ALTER TABLE communications.newsletters DROP COLUMN IF EXISTS version;
            ");

            // ============================================================
            // PART 2: Create missing tables (idempotent)
            // ============================================================

            // Table 1: events.event_reminders_sent
            // Schema from Phase6A71_CreateEventRemindersSentTable
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS events.event_reminders_sent (
                    id UUID NOT NULL,
                    event_id UUID NOT NULL,
                    registration_id UUID NOT NULL,
                    reminder_type VARCHAR(50) NOT NULL,
                    sent_at TIMESTAMPTZ NOT NULL,
                    recipient_email VARCHAR(255) NOT NULL,
                    CONSTRAINT pk_event_reminders_sent PRIMARY KEY (id),
                    CONSTRAINT fk_event_reminders_sent_events_event_id
                        FOREIGN KEY (event_id)
                        REFERENCES events.events(""Id"")
                        ON DELETE CASCADE,
                    CONSTRAINT fk_event_reminders_sent_registrations_registration_id
                        FOREIGN KEY (registration_id)
                        REFERENCES events.registrations(""Id"")
                        ON DELETE CASCADE
                );

                -- Create indexes if they don't exist (use IF NOT EXISTS)
                CREATE INDEX IF NOT EXISTS ix_event_reminders_sent_event_id
                    ON events.event_reminders_sent(event_id);

                CREATE INDEX IF NOT EXISTS ix_event_reminders_sent_registration_id
                    ON events.event_reminders_sent(registration_id);

                CREATE UNIQUE INDEX IF NOT EXISTS ix_event_reminders_sent_event_registration_type
                    ON events.event_reminders_sent(event_id, registration_id, reminder_type);

                CREATE INDEX IF NOT EXISTS ix_event_reminders_sent_sent_at
                    ON events.event_reminders_sent(sent_at);
            ");

            // Table 2: communications.event_notification_history
            // Schema from Phase6A61_AddEventNotificationHistoryTable
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS communications.event_notification_history (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    event_id UUID NOT NULL,
                    sent_by_user_id UUID NOT NULL,
                    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    recipient_count INT NOT NULL,
                    successful_sends INT NOT NULL DEFAULT 0,
                    failed_sends INT NOT NULL DEFAULT 0,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

                    CONSTRAINT fk_event_notification_history_event_id
                        FOREIGN KEY (event_id)
                        REFERENCES events.events(""Id"")
                        ON DELETE CASCADE,

                    CONSTRAINT fk_event_notification_history_sent_by_user_id
                        FOREIGN KEY (sent_by_user_id)
                        REFERENCES identity.users(""Id"")
                        ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ix_event_notification_history_event_id
                    ON communications.event_notification_history(event_id);

                CREATE INDEX IF NOT EXISTS ix_event_notification_history_sent_at_desc
                    ON communications.event_notification_history(sent_at DESC);
            ");

            // ============================================================
            // PART 3: Ensure all 12 EventCategory reference values exist
            // ============================================================
            // Deterministic GUIDs computed via MD5("LankaConnect.ReferenceData.EventCategory.{code}")
            // First remove any duplicates with wrong GUIDs, then insert correct records

            // 1. Religious
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Religious'
                AND id != '31f73d61-6c12-1252-f5ab-10d9d47eba46';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '31f73d61-6c12-1252-f5ab-10d9d47eba46', 'EventCategory', 'Religious', 0, 'Religious', 1, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '31f73d61-6c12-1252-f5ab-10d9d47eba46');
            ");

            // 2. Cultural
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Cultural'
                AND id != '80cd50b4-7630-f5d0-1f9a-a7c480347dcf';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '80cd50b4-7630-f5d0-1f9a-a7c480347dcf', 'EventCategory', 'Cultural', 1, 'Cultural', 2, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '80cd50b4-7630-f5d0-1f9a-a7c480347dcf');
            ");

            // 3. Community
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Community'
                AND id != '0b9effc0-322f-8026-85c6-747e381b41e6';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '0b9effc0-322f-8026-85c6-747e381b41e6', 'EventCategory', 'Community', 2, 'Community', 3, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '0b9effc0-322f-8026-85c6-747e381b41e6');
            ");

            // 4. Educational
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Educational'
                AND id != '4de1eacb-273a-ab85-e811-d60addb4ae30';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '4de1eacb-273a-ab85-e811-d60addb4ae30', 'EventCategory', 'Educational', 3, 'Educational', 4, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '4de1eacb-273a-ab85-e811-d60addb4ae30');
            ");

            // 5. Social
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Social'
                AND id != '4e57a1be-7a76-833e-003f-b2e3182f29f0';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '4e57a1be-7a76-833e-003f-b2e3182f29f0', 'EventCategory', 'Social', 4, 'Social', 5, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '4e57a1be-7a76-833e-003f-b2e3182f29f0');
            ");

            // 6. Business
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Business'
                AND id != '2d87836d-9322-d4b1-b4ec-b5b73eca9ad9';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '2d87836d-9322-d4b1-b4ec-b5b73eca9ad9', 'EventCategory', 'Business', 5, 'Business', 6, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '2d87836d-9322-d4b1-b4ec-b5b73eca9ad9');
            ");

            // 7. Charity
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Charity'
                AND id != '70ab7cff-d677-f4bd-b331-f02908ee3347';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '70ab7cff-d677-f4bd-b331-f02908ee3347', 'EventCategory', 'Charity', 6, 'Charity', 7, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '70ab7cff-d677-f4bd-b331-f02908ee3347');
            ");

            // 8. Entertainment
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Entertainment'
                AND id != 'cdaa97c0-e68f-2819-984e-63bb9dcf35a6';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT 'cdaa97c0-e68f-2819-984e-63bb9dcf35a6', 'EventCategory', 'Entertainment', 7, 'Entertainment', 8, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = 'cdaa97c0-e68f-2819-984e-63bb9dcf35a6');
            ");

            // 9. Workshop
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Workshop'
                AND id != 'c5735376-4831-c12b-a01e-672efee6c8e3';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT 'c5735376-4831-c12b-a01e-672efee6c8e3', 'EventCategory', 'Workshop', 8, 'Workshop', 9, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = 'c5735376-4831-c12b-a01e-672efee6c8e3');
            ");

            // 10. Festival
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Festival'
                AND id != '9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1', 'EventCategory', 'Festival', 9, 'Festival', 10, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1');
            ");

            // 11. Ceremony
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Ceremony'
                AND id != 'e1d5afac-09d6-ef55-a529-f5bf473ef103';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT 'e1d5afac-09d6-ef55-a529-f5bf473ef103', 'EventCategory', 'Ceremony', 10, 'Ceremony', 11, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = 'e1d5afac-09d6-ef55-a529-f5bf473ef103');
            ");

            // 12. Celebration
            migrationBuilder.Sql(@"
                DELETE FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory' AND code = 'Celebration'
                AND id != '6313b249-2620-3e97-c1bd-f1d50814156d';

                INSERT INTO reference_data.reference_values (id, enum_type, code, int_value, name, display_order, is_active, metadata, created_at, updated_at)
                SELECT '6313b249-2620-3e97-c1bd-f1d50814156d', 'EventCategory', 'Celebration', 11, 'Celebration', 12, true, '{""iconUrl"":""""}'::jsonb, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM reference_data.reference_values WHERE id = '6313b249-2620-3e97-c1bd-f1d50814156d');
            ");

            // ============================================================
            // PART 4: Ensure all 29 email templates exist (idempotent)
            // ============================================================

            // ----- Templates 1-22: Already exist in production (safety net inserts) -----
            // These use minimal content since WHERE NOT EXISTS will skip them if they already exist.

            // 1. template-free-event-registration-confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-free-event-registration-confirmation', 'Confirmation email for free event registration', 'Registration Confirmed - {{EventTitle}}', 'Hi {{UserName}}, your registration for {{EventTitle}} is confirmed.', '<p>Hi {{UserName}}, your registration for {{EventTitle}} is confirmed.</p>', 'Event', 'Registration', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-free-event-registration-confirmation');
            ");

            // 2. template-paid-event-registration-confirmation-with-ticket
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-paid-event-registration-confirmation-with-ticket', 'Confirmation email for paid event registration with ticket', 'Registration & Payment Confirmed - {{EventTitle}}', 'Hi {{UserName}}, your payment and registration for {{EventTitle}} is confirmed.', '<p>Hi {{UserName}}, your payment and registration for {{EventTitle}} is confirmed.</p>', 'Event', 'Registration', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-paid-event-registration-confirmation-with-ticket');
            ");

            // 3. template-preliminary-registration-payment-pending
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-preliminary-registration-payment-pending', 'Email for preliminary registration awaiting payment', 'Payment Pending - {{EventTitle}}', 'Hi {{UserName}}, your registration for {{EventTitle}} is pending payment.', '<p>Hi {{UserName}}, your registration for {{EventTitle}} is pending payment.</p>', 'Event', 'Registration', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-preliminary-registration-payment-pending');
            ");

            // 4. template-event-reminder
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-event-reminder', 'Reminder email for upcoming events', 'Reminder: {{EventTitle}} is coming up!', 'Hi {{UserName}}, this is a reminder that {{EventTitle}} is coming up soon.', '<p>Hi {{UserName}}, this is a reminder that {{EventTitle}} is coming up soon.</p>', 'Event', 'Reminder', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-event-reminder');
            ");

            // 5. template-membership-email-verification
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-membership-email-verification', 'Email verification for new member registration', 'Verify Your Email - LankaConnect', 'Hi {{UserName}}, please verify your email address.', '<p>Hi {{UserName}}, please verify your email address.</p>', 'Account', 'Verification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-membership-email-verification');
            ");

            // 6. template-signup-list-commitment-confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-signup-list-commitment-confirmation', 'Confirmation email for signup list commitment', 'Signup Confirmed - {{EventTitle}}', 'Hi {{UserName}}, your signup commitment for {{EventTitle}} is confirmed.', '<p>Hi {{UserName}}, your signup commitment for {{EventTitle}} is confirmed.</p>', 'Event', 'SignUp', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-signup-list-commitment-confirmation');
            ");

            // 7. template-signup-list-commitment-update
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-signup-list-commitment-update', 'Update notification for signup list commitment changes', 'Signup Updated - {{EventTitle}}', 'Hi {{UserName}}, your signup commitment for {{EventTitle}} has been updated.', '<p>Hi {{UserName}}, your signup commitment for {{EventTitle}} has been updated.</p>', 'Event', 'SignUp', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-signup-list-commitment-update');
            ");

            // 8. template-signup-list-commitment-cancellation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-signup-list-commitment-cancellation', 'Cancellation notification for signup list commitment', 'Signup Cancelled - {{EventTitle}}', 'Hi {{UserName}}, your signup commitment for {{EventTitle}} has been cancelled.', '<p>Hi {{UserName}}, your signup commitment for {{EventTitle}} has been cancelled.</p>', 'Event', 'SignUp', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-signup-list-commitment-cancellation');
            ");

            // 9. template-event-registration-cancellation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-event-registration-cancellation', 'Notification when event registration is cancelled', 'Registration Cancelled - {{EventTitle}}', 'Hi {{UserName}}, your registration for {{EventTitle}} has been cancelled.', '<p>Hi {{UserName}}, your registration for {{EventTitle}} has been cancelled.</p>', 'Event', 'Registration', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-event-registration-cancellation');
            ");

            // 10. template-attendees-added-confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-attendees-added-confirmation', 'Confirmation when additional attendees are added to registration', 'Attendees Added - {{EventTitle}}', 'Hi {{UserName}}, additional attendees have been added to your registration for {{EventTitle}}.', '<p>Hi {{UserName}}, additional attendees have been added to your registration for {{EventTitle}}.</p>', 'Event', 'Registration', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-attendees-added-confirmation');
            ");

            // 11. template-refund-requested
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-refund-requested', 'Notification when refund is requested', 'Refund Requested - {{EventTitle}}', 'Hi {{UserName}}, your refund request for {{EventTitle}} has been received.', '<p>Hi {{UserName}}, your refund request for {{EventTitle}} has been received.</p>', 'Payment', 'Refund', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-refund-requested');
            ");

            // 12. template-refund-completed
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-refund-completed', 'Notification when refund is completed', 'Refund Completed - {{EventTitle}}', 'Hi {{UserName}}, your refund for {{EventTitle}} has been completed.', '<p>Hi {{UserName}}, your refund for {{EventTitle}} has been completed.</p>', 'Payment', 'Refund', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-refund-completed');
            ");

            // 13. template-new-event-publication
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-new-event-publication', 'Notification when a new event is published', 'New Event: {{EventTitle}}', 'A new event has been published: {{EventTitle}}.', '<p>A new event has been published: {{EventTitle}}.</p>', 'Event', 'Notification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-new-event-publication');
            ");

            // 14. template-event-details-publication
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-event-details-publication', 'Event details notification to attendees', 'Event Details: {{EventTitle}}', 'Here are the details for {{EventTitle}}.', '<p>Here are the details for {{EventTitle}}.</p>', 'Event', 'Notification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-event-details-publication');
            ");

            // 15. template-event-cancellation-notifications
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-event-cancellation-notifications', 'Notification when an event is cancelled', 'Event Cancelled: {{EventTitle}}', 'Hi {{UserName}}, the event {{EventTitle}} has been cancelled.', '<p>Hi {{UserName}}, the event {{EventTitle}} has been cancelled.</p>', 'Event', 'Notification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-event-cancellation-notifications');
            ");

            // 16. template-event-approval
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-event-approval', 'Notification when an event is approved', 'Event Approved: {{EventTitle}}', 'Your event {{EventTitle}} has been approved.', '<p>Your event {{EventTitle}} has been approved.</p>', 'Event', 'Notification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-event-approval');
            ");

            // 17. template-newsletter-notification
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-newsletter-notification', 'Newsletter email template', '{{Subject}}', '{{Content}}', '<p>{{Content}}</p>', 'Newsletter', 'Notification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-newsletter-notification');
            ");

            // 18. template-newsletter-subscription-confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-newsletter-subscription-confirmation', 'Confirmation for newsletter subscription', 'Newsletter Subscription Confirmed', 'Hi {{UserName}}, you have successfully subscribed to our newsletter.', '<p>Hi {{UserName}}, you have successfully subscribed to our newsletter.</p>', 'Newsletter', 'Confirmation', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-newsletter-subscription-confirmation');
            ");

            // 19. template-password-reset
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-password-reset', 'Password reset email with reset link', 'Reset Your Password - LankaConnect', 'Hi {{UserName}}, click the link to reset your password: {{ResetLink}}', '<p>Hi {{UserName}}, click the link to reset your password: <a href=""{{ResetLink}}"">Reset Password</a></p>', 'Account', 'Security', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-password-reset');
            ");

            // 20. template-password-change-confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-password-change-confirmation', 'Confirmation that password was changed', 'Password Changed - LankaConnect', 'Hi {{UserName}}, your password has been successfully changed.', '<p>Hi {{UserName}}, your password has been successfully changed.</p>', 'Account', 'Security', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-password-change-confirmation');
            ");

            // 21. template-welcome
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-welcome', 'Welcome email for new users', 'Welcome to LankaConnect!', 'Hi {{UserName}}, welcome to LankaConnect!', '<p>Hi {{UserName}}, welcome to LankaConnect!</p>', 'Account', 'Welcome', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-welcome');
            ");

            // 22. template-organizer-role-approval
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
                SELECT gen_random_uuid(), 'template-organizer-role-approval', 'Notification when organizer role is approved', 'Organizer Role Approved - LankaConnect', 'Hi {{UserName}}, your organizer role has been approved.', '<p>Hi {{UserName}}, your organizer role has been approved.</p>', 'Account', 'Notification', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-organizer-role-approval');
            ");

            // ----- Templates 23-29: Likely missing from production (full content from Phase6A93) -----

            // 23. OrganizerCustomEmail
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'OrganizerCustomEmail',
                    'Phase 6A.93: Custom email template for organizers to send to event attendees/signups',
                    '{{CustomSubject}}',
                    'Hi {{UserName}},

{{CustomBody}}

---
Event: {{EventTitle}}
Date: {{EventDateTime}}
Location: {{EventLocation}}
Event Details: {{EventDetailsUrl}}

Best regards,
{{OrganizerName}}
{{OrganizerEmail}}

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .custom-body { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 6px; border-left: 4px solid #6366f1; white-space: pre-wrap; }
        .event-info { background: #eef2ff; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .event-info h3 { margin: 0 0 12px 0; color: #4338ca; font-size: 16px; }
        .event-info p { margin: 6px 0; color: #3730a3; }
        .event-info a { color: #4f46e5; text-decoration: none; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .organizer-sig { margin-top: 20px; padding-top: 20px; border-top: 1px solid #e5e7eb; }
        .organizer-sig p { margin: 4px 0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Message from Event Organizer</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""custom-body"">{{CustomBody}}</div>

            <div class=""event-info"">
                <h3>Event Details</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><a href=""{{EventDetailsUrl}}"">View Event Details →</a></p>
            </div>

            <div class=""organizer-sig"">
                <p>Best regards,</p>
                <p><strong>{{OrganizerName}}</strong></p>
                <p><a href=""mailto:{{OrganizerEmail}}"" style=""color: #6366f1;"">{{OrganizerEmail}}</a></p>
            </div>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Organizer',
                    'Custom',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'OrganizerCustomEmail'
                );
            ");

            // 24. template-support-ticket-confirmation
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-support-ticket-confirmation',
                    'Phase 6A.93: Auto-confirmation email sent when contact form is submitted',
                    'We Received Your Message - Reference #{{ReferenceId}}',
                    'Hi {{Name}},

Thank you for contacting LankaConnect!

We have received your message and will respond as soon as possible. Please keep this reference number for your records:

Reference Number: {{ReferenceId}}
Subject: {{Subject}}

Your Message:
{{Message}}

Our team typically responds within 24-48 business hours. If your matter is urgent, please mention your reference number in any follow-up communication.

Best regards,
The LankaConnect Team

Questions? Contact us at {{SupportEmail}}

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .reference-box { background: #ecfdf5; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; }
        .reference-box .label { color: #065f46; font-size: 14px; margin-bottom: 8px; }
        .reference-box .number { color: #059669; font-size: 24px; font-weight: 700; font-family: monospace; }
        .message-box { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 6px; border-left: 4px solid #10b981; }
        .message-box h3 { margin: 0 0 10px 0; color: #374151; font-size: 14px; }
        .message-box p { margin: 0; color: #4b5563; white-space: pre-wrap; }
        .info-text { color: #6b7280; font-size: 14px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Message Received</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{Name}}</strong>,</p>
            <p>Thank you for contacting LankaConnect! We have received your message and will respond as soon as possible.</p>

            <div class=""reference-box"">
                <div class=""label"">Your Reference Number</div>
                <div class=""number"">{{ReferenceId}}</div>
            </div>

            <p><strong>Subject:</strong> {{Subject}}</p>

            <div class=""message-box"">
                <h3>Your Message:</h3>
                <p>{{Message}}</p>
            </div>

            <p class=""info-text"">Our team typically responds within 24-48 business hours. If your matter is urgent, please mention your reference number in any follow-up communication.</p>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>Questions? Contact us at <a href=""mailto:{{SupportEmail}}"">{{SupportEmail}}</a></p>
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Support',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-support-ticket-confirmation'
                );
            ");

            // 25. template-support-ticket-reply
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-support-ticket-reply',
                    'Phase 6A.93: Email sent when admin replies to a support ticket',
                    'Re: {{Subject}} - Reference #{{ReferenceId}}',
                    'Hi {{Name}},

We have responded to your support request (Reference: {{ReferenceId}}).

Our Response:
{{ReplyContent}}

---
Original Subject: {{Subject}}

If you have any further questions, please reply to this email or contact us at {{SupportEmail}} with your reference number.

Best regards,
The LankaConnect Support Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .header .ref { display: inline-block; background: rgba(255,255,255,0.2); padding: 6px 14px; border-radius: 20px; margin-top: 10px; font-size: 14px; }
        .content { padding: 30px 20px; }
        .reply-box { background: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .reply-box h3 { margin: 0 0 12px 0; color: #1e40af; font-size: 14px; }
        .reply-box p { margin: 0; color: #1e3a8a; white-space: pre-wrap; }
        .original-subject { color: #6b7280; font-size: 13px; margin-top: 20px; padding-top: 15px; border-top: 1px solid #e5e7eb; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #3b82f6; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Support Response</h1>
            <div class=""ref"">Reference: {{ReferenceId}}</div>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{Name}}</strong>,</p>
            <p>We have responded to your support request.</p>

            <div class=""reply-box"">
                <h3>Our Response:</h3>
                <p>{{ReplyContent}}</p>
            </div>

            <p class=""original-subject""><strong>Original Subject:</strong> {{Subject}}</p>

            <p>If you have any further questions, please reply to this email or contact us with your reference number.</p>

            <p>Best regards,<br><strong>The LankaConnect Support Team</strong></p>
        </div>
        <div class=""footer"">
            <p>Questions? Contact us at <a href=""mailto:{{SupportEmail}}"">{{SupportEmail}}</a></p>
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Support',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-support-ticket-reply'
                );
            ");

            // 26. template-account-locked-by-admin
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-account-locked-by-admin',
                    'Phase 6A.93: Email sent when admin locks a user account',
                    'Your LankaConnect Account Has Been Temporarily Locked',
                    'Hi {{UserName}},

Your LankaConnect account has been temporarily locked by an administrator.

Lock Details:
- Locked Until: {{LockUntil}}
- Reason: {{Reason}}

During this time, you will not be able to log in or access your account.

If you believe this was done in error or have questions, please contact us at {{SupportEmail}}.

LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .alert-box { background: #fef3c7; border: 1px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .alert-box h3 { margin: 0 0 12px 0; color: #92400e; font-size: 16px; }
        .alert-box p { margin: 6px 0; color: #78350f; }
        .info-text { color: #6b7280; font-size: 14px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #f59e0b; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Account Temporarily Locked</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>
            <p>Your LankaConnect account has been temporarily locked by an administrator.</p>

            <div class=""alert-box"">
                <h3>Lock Details</h3>
                <p><strong>Locked Until:</strong> {{LockUntil}}</p>
                <p><strong>Reason:</strong> {{Reason}}</p>
            </div>

            <p class=""info-text"">During this time, you will not be able to log in or access your account.</p>

            <p>If you believe this was done in error or have questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #f59e0b;"">{{SupportEmail}}</a>.</p>

            <p>LankaConnect Team</p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Security',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-locked-by-admin'
                );
            ");

            // 27. template-account-unlocked-by-admin
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-account-unlocked-by-admin',
                    'Phase 6A.93: Email sent when admin unlocks a user account',
                    'Your LankaConnect Account Has Been Unlocked',
                    'Hi {{UserName}},

Great news! Your LankaConnect account has been unlocked by an administrator.

You can now log in and access your account as usual.

Login here: {{LoginUrl}}

If you have any questions, please contact us at {{SupportEmail}}.

Best regards,
LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .success-box { background: #ecfdf5; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; }
        .success-box p { margin: 0; color: #065f46; font-size: 16px; }
        .cta-button { display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 16px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Account Unlocked</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""success-box"">
                <p>Your LankaConnect account has been unlocked!</p>
            </div>

            <p>You can now log in and access your account as usual.</p>

            <div style=""text-align: center;"">
                <a href=""{{LoginUrl}}"" class=""cta-button"">Login to Your Account</a>
            </div>

            <p>If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #10b981;"">{{SupportEmail}}</a>.</p>

            <p>Best regards,<br><strong>LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Security',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-unlocked-by-admin'
                );
            ");

            // 28. template-account-activated-by-admin
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-account-activated-by-admin',
                    'Phase 6A.93: Email sent when admin activates a user account',
                    'Your LankaConnect Account Has Been Activated',
                    'Hi {{UserName}},

Great news! Your LankaConnect account has been activated by an administrator.

You can now log in and enjoy all the features of LankaConnect.

Login here: {{LoginUrl}}

If you have any questions, please contact us at {{SupportEmail}}.

Welcome back!
LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .success-box { background: #ecfdf5; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; }
        .success-box p { margin: 0; color: #065f46; font-size: 16px; }
        .cta-button { display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 16px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Account Activated</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""success-box"">
                <p>Your LankaConnect account has been activated!</p>
            </div>

            <p>You can now log in and enjoy all the features of LankaConnect.</p>

            <div style=""text-align: center;"">
                <a href=""{{LoginUrl}}"" class=""cta-button"">Login to Your Account</a>
            </div>

            <p>If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #10b981;"">{{SupportEmail}}</a>.</p>

            <p>Welcome back!<br><strong>LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-activated-by-admin'
                );
            ");

            // 29. template-account-deactivated-by-admin
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-account-deactivated-by-admin',
                    'Phase 6A.93: Email sent when admin deactivates a user account',
                    'Your LankaConnect Account Has Been Deactivated',
                    'Hi {{UserName}},

Your LankaConnect account has been deactivated by an administrator.

You will no longer be able to log in or access your account until it is reactivated.

If you believe this was done in error or would like to request reactivation, please contact us at {{SupportEmail}}.

LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .notice-box { background: #f3f4f6; border: 1px solid #9ca3af; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .notice-box p { margin: 0; color: #374151; }
        .info-text { color: #6b7280; font-size: 14px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #6b7280; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Account Deactivated</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""notice-box"">
                <p>Your LankaConnect account has been deactivated by an administrator.</p>
            </div>

            <p class=""info-text"">You will no longer be able to log in or access your account until it is reactivated.</p>

            <p>If you believe this was done in error or would like to request reactivation, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #6b7280;"">{{SupportEmail}}</a>.</p>

            <p>LankaConnect Team</p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-deactivated-by-admin'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: xmin is a PostgreSQL system column, not added by Up(), so not dropped here

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                schema: "communications",
                table: "newsletters",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                schema: "communications",
                table: "newsletter_subscribers",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8262));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8386));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8115));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8325));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8355));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8765));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8291));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8202));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8473));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8445));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8415));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8709));
        }
    }
}
