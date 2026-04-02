using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase7A_WhatsAppIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_whatsapp_preferences",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    whatsapp_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    whatsapp_phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    phone_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    verification_code_expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    verification_locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notify_event_registration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_event_reminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_event_cancellation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_event_update = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_signup_commitment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_refund = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_newsletter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_new_event = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_payment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    quiet_hours_start = table.Column<TimeOnly>(type: "time", nullable: true),
                    quiet_hours_end = table.Column<TimeOnly>(type: "time", nullable: true),
                    respect_cultural_timing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_whatsapp_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWhatsAppPreferences_Users_UserId",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_messages",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    template_parameters = table.Column<string>(type: "jsonb", nullable: true),
                    message_content = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    cultural_context = table.Column<string>(type: "jsonb", nullable: true),
                    cultural_appropriateness_score = table.Column<double>(type: "double precision", nullable: false),
                    requires_cultural_validation = table.Column<bool>(type: "boolean", nullable: false),
                    diaspora_region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    acs_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    newsletter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_Events_EventId",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_Users_UserId",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_templates",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    header_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body_text = table.Column<string>(type: "text", nullable: false),
                    footer_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    button_types = table.Column<string>(type: "jsonb", nullable: true),
                    parameter_names = table.Column<string>(type: "jsonb", nullable: false),
                    parameter_count = table.Column<int>(type: "integer", nullable: false),
                    meta_template_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sample_values = table.Column<string>(type: "jsonb", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_webhook_events",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    acs_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_webhook_events", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(863));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(919));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(830));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(886));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(898));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(1026));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(874));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(850));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(1003));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(946));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(930));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 2, 4, 8, 56, 213, DateTimeKind.Utc).AddTicks(1016));

            migrationBuilder.CreateIndex(
                name: "IX_UserWhatsAppPreferences_CreatedAt",
                schema: "communications",
                table: "user_whatsapp_preferences",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_UserWhatsAppPreferences_Phone_EnabledVerified",
                schema: "communications",
                table: "user_whatsapp_preferences",
                column: "whatsapp_phone_number",
                filter: "whatsapp_enabled = true AND phone_verified = true");

            migrationBuilder.CreateIndex(
                name: "IX_UserWhatsAppPreferences_UserId_Unique",
                schema: "communications",
                table: "user_whatsapp_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_AcsMessageId",
                schema: "communications",
                table: "whatsapp_messages",
                column: "acs_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CreatedAt",
                schema: "communications",
                table: "whatsapp_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_EventId",
                schema: "communications",
                table: "whatsapp_messages",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_Status",
                schema: "communications",
                table: "whatsapp_messages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_Status_ScheduledFor",
                schema: "communications",
                table: "whatsapp_messages",
                columns: new[] { "status", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_TemplateName",
                schema: "communications",
                table: "whatsapp_messages",
                column: "template_name");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_ToPhoneNumber",
                schema: "communications",
                table: "whatsapp_messages",
                column: "to_phone_number");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_UserId",
                schema: "communications",
                table: "whatsapp_messages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_Category",
                schema: "communications",
                table: "whatsapp_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_CreatedAt",
                schema: "communications",
                table: "whatsapp_templates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_Status",
                schema: "communications",
                table: "whatsapp_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_TemplateName_Unique",
                schema: "communications",
                table: "whatsapp_templates",
                column: "template_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_AcsMessageId",
                schema: "communications",
                table: "whatsapp_webhook_events",
                column: "acs_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_CreatedAt",
                schema: "communications",
                table: "whatsapp_webhook_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_Processed_CreatedAt",
                schema: "communications",
                table: "whatsapp_webhook_events",
                columns: new[] { "processed", "created_at" });

            // Phase 7A: Seed 13 Tier 1 WhatsApp templates + 1 verification template
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            migrationBuilder.Sql($@"
INSERT INTO communications.whatsapp_templates (id, template_name, display_name, category, language, status, header_text, body_text, footer_text, parameter_names, parameter_count, created_at)
VALUES
  (gen_random_uuid(), 'event_registration_confirmed', 'Event Registration Confirmed', 'Utility', 'en', 'Pending', 'Registration Confirmed', 'Hi {{{{1}}}}, your registration for ""{{{{2}}}}"" is confirmed!\n\nDate: {{{{3}}}}\nTime: {{{{4}}}}\nLocation: {{{{5}}}}\nQuantity: {{{{6}}}}\n\nView event details: {{{{7}}}}', 'LankaConnect - Sri Lankan Community Events', '[""user_name"",""event_title"",""event_date"",""event_time"",""event_location"",""registration_quantity"",""event_url""]', 7, NOW()),
  (gen_random_uuid(), 'event_ticket_confirmation', 'Event Ticket Confirmation', 'Utility', 'en', 'Pending', 'Your Ticket is Ready', 'Hi {{{{1}}}}, your ticket for ""{{{{2}}}}"" has been confirmed!\n\nTicket Code: {{{{3}}}}\nDate: {{{{4}}}}\nTime: {{{{5}}}}\nLocation: {{{{6}}}}\n\nPresent this ticket code at the venue for entry.\n\nView your ticket: {{{{7}}}}', 'LankaConnect', '[""user_name"",""event_title"",""ticket_code"",""event_date"",""event_time"",""event_location"",""event_url""]', 7, NOW()),
  (gen_random_uuid(), 'event_reminder', 'Event Reminder', 'Utility', 'en', 'Pending', 'Event Reminder', 'Hi {{{{1}}}}, reminder: ""{{{{2}}}}"" is {{{{3}}}}!\n\n{{{{4}}}}\n\nDate: {{{{5}}}}\nTime: {{{{6}}}}\nLocation: {{{{7}}}}\n\nView event details: {{{{8}}}}', 'LankaConnect', '[""user_name"",""event_title"",""reminder_timeframe"",""reminder_message"",""event_date"",""event_time"",""event_location"",""event_url""]', 8, NOW()),
  (gen_random_uuid(), 'event_cancelled', 'Event Cancelled', 'Utility', 'en', 'Pending', 'Event Cancelled', 'Hi {{{{1}}}}, unfortunately ""{{{{2}}}}"" has been cancelled.\n\n{{{{3}}}}\n\n{{{{4}}}}\n\nWe apologize for the inconvenience.', 'LankaConnect', '[""user_name"",""event_title"",""cancellation_reason"",""refund_message"",""event_url""]', 5, NOW()),
  (gen_random_uuid(), 'new_event_announcement', 'New Event Announcement', 'Marketing', 'en', 'Pending', 'New Event', 'Hi {{{{1}}}}, check out this new event: ""{{{{2}}}}""!\n\nDate: {{{{3}}}}\nLocation: {{{{4}}}}\n\nRegister now: {{{{5}}}}', 'LankaConnect', '[""user_name"",""event_title"",""event_date"",""event_location"",""event_url""]', 5, NOW()),
  (gen_random_uuid(), 'event_details_update', 'Event Details Updated', 'Utility', 'en', 'Pending', 'Event Updated', 'Hi {{{{1}}}}, ""{{{{2}}}}"" has been updated.\n\nDate: {{{{3}}}}\nTime: {{{{4}}}}\nLocation: {{{{5}}}}\n\nPlease review the latest details: {{{{6}}}}', 'LankaConnect', '[""user_name"",""event_title"",""event_date"",""event_time"",""event_location"",""event_url""]', 6, NOW()),
  (gen_random_uuid(), 'newsletter_broadcast', 'Newsletter Broadcast', 'Marketing', 'en', 'Pending', NULL, '{{{{1}}}}\n\n{{{{2}}}}\n\nRead the full newsletter: {{{{3}}}}', 'LankaConnect', '[""newsletter_title"",""newsletter_preview"",""newsletter_url""]', 3, NOW()),
  (gen_random_uuid(), 'registration_cancelled', 'Registration Cancelled', 'Utility', 'en', 'Pending', 'Registration Cancelled', 'Hi {{{{1}}}}, your registration for ""{{{{2}}}}"" has been cancelled.\n\nReason: {{{{3}}}}\nDate cancelled: {{{{4}}}}\n\nIf you have questions, please contact the event organizer.', 'LankaConnect', '[""user_name"",""event_title"",""cancellation_reason"",""cancellation_date"",""event_url""]', 5, NOW()),
  (gen_random_uuid(), 'signup_commitment_confirmed', 'Sign-up Confirmed', 'Utility', 'en', 'Pending', 'Sign-up Confirmed', 'Hi {{{{1}}}}, your commitment to ""{{{{2}}}}"" for the event ""{{{{3}}}}"" has been confirmed!\n\nItem: {{{{4}}}}\nQuantity: {{{{5}}}}\n\nView event details: {{{{6}}}}', 'LankaConnect', '[""user_name"",""list_name"",""event_title"",""commitment_item"",""commitment_quantity"",""event_url""]', 6, NOW()),
  (gen_random_uuid(), 'signup_commitment_updated', 'Sign-up Updated', 'Utility', 'en', 'Pending', 'Sign-up Updated', 'Hi {{{{1}}}}, your commitment to ""{{{{2}}}}"" for ""{{{{3}}}}"" has been updated.\n\nItem: {{{{4}}}}\nNew quantity: {{{{5}}}}\n\nView event details: {{{{6}}}}', 'LankaConnect', '[""user_name"",""list_name"",""event_title"",""commitment_item"",""commitment_quantity"",""event_url""]', 6, NOW()),
  (gen_random_uuid(), 'signup_commitment_cancelled', 'Sign-up Cancelled', 'Utility', 'en', 'Pending', 'Sign-up Cancelled', 'Hi {{{{1}}}}, your commitment to ""{{{{2}}}}"" for ""{{{{3}}}}"" has been cancelled.\n\nIf this was a mistake, you can sign up again from the event page.\n\nView event: {{{{4}}}}', 'LankaConnect', '[""user_name"",""list_name"",""event_title"",""event_url""]', 4, NOW()),
  (gen_random_uuid(), 'refund_initiated', 'Refund Initiated', 'Utility', 'en', 'Pending', 'Refund Initiated', 'Hi {{{{1}}}}, a refund of ${{{{2}}}} for ""{{{{3}}}}"" has been initiated.\n\nStatus: {{{{4}}}}\nReference: {{{{5}}}}\n\nYou will be notified when the refund is completed.', 'LankaConnect', '[""user_name"",""refund_amount"",""event_title"",""refund_status"",""reference_id""]', 5, NOW()),
  (gen_random_uuid(), 'refund_completed', 'Refund Completed', 'Utility', 'en', 'Pending', 'Refund Completed', 'Hi {{{{1}}}}, your refund of ${{{{2}}}} for ""{{{{3}}}}"" has been completed!\n\nThe refund will appear in your account within 5-10 business days.\n\nReference: {{{{4}}}}', 'LankaConnect', '[""user_name"",""refund_amount"",""event_title"",""reference_id""]', 4, NOW()),
  (gen_random_uuid(), 'phone_verification', 'Phone Verification', 'Utility', 'en', 'Pending', NULL, 'Your LankaConnect verification code is {{{{1}}}}. This code expires in 10 minutes.', 'LankaConnect', '[""verification_code""]', 1, NOW());
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_whatsapp_preferences",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "whatsapp_messages",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "whatsapp_templates",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "whatsapp_webhook_events",
                schema: "communications");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2719));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2799));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2659));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2755));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2772));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2737));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2698));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2854));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2838));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2817));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 29, 22, 40, 22, 416, DateTimeKind.Utc).AddTicks(2871));
        }
    }
}
