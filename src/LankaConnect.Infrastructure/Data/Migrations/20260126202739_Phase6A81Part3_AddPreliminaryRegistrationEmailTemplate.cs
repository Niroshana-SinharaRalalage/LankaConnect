using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A81Part3_AddPreliminaryRegistrationEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.81 Part 3: Add Preliminary Registration Payment Pending email template
            // This email is sent immediately when a user creates a Preliminary registration but hasn't completed payment
            // User decision: Immediate sending (not delayed)
            // Architect approval: Matches Phase 6A.83 fail-silent pattern

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
                    'template-preliminary-registration-payment-pending',
                    'Phase 6A.81 Part 3: Email sent when Preliminary registration created (payment pending within 24 hours)',
                    'Complete Your Registration for {{EventTitle}} - Payment Pending',
                    'Hi {{UserName}},

Thank you for starting your registration for {{EventTitle}}!

REGISTRATION STATUS: Payment Pending
Your registration is almost complete. To confirm your spot, please complete your payment within the next {{HoursRemaining}} hours.

EVENT DETAILS
-------------
Event: {{EventTitle}}
Date: {{EventStartDate}}
Time: {{EventStartTime}}
Location: {{EventLocation}}

REGISTRATION SUMMARY
-------------------
Number of Attendees: {{AttendeeCount}}
Total Amount: {{Currency}} {{TotalAmount}}

IMPORTANT: Complete Payment Now
-------------------------------
Your checkout session expires at: {{ExpiresAt}}

Click here to complete your payment:
{{PaymentLink}}

If you do not complete payment within 24 hours, your registration will be automatically cancelled and your spot will be released to other attendees.

Questions? Contact us at {{SupportEmail}}

(c) {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #f97316 0%, #ea580c 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .header .status { display: inline-block; background: rgba(255,255,255,0.2); color: white; padding: 8px 16px; border-radius: 20px; margin-top: 10px; font-size: 14px; font-weight: 500; }
        .content { padding: 30px 20px; }
        .alert-box { background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px; }
        .alert-box strong { color: #92400e; }
        .event-details { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 6px; border: 1px solid #e5e7eb; }
        .event-details h3 { margin: 0 0 15px 0; color: #374151; font-size: 16px; }
        .event-details p { margin: 8px 0; color: #4b5563; }
        .event-details strong { color: #111827; }
        .registration-summary { background: white; padding: 15px; margin: 20px 0; border: 2px solid #10b981; border-radius: 6px; }
        .registration-summary h3 { margin: 0 0 12px 0; color: #059669; font-size: 16px; }
        .registration-summary p { margin: 6px 0; color: #4b5563; }
        .cta-button { display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; text-decoration: none; padding: 16px 32px; border-radius: 6px; font-weight: 600; font-size: 16px; margin: 20px 0; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.2); transition: all 0.3s; }
        .cta-button:hover { transform: translateY(-2px); box-shadow: 0 6px 8px rgba(16, 185, 129, 0.3); }
        .expiry-notice { background: #fee2e2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; border-radius: 4px; }
        .expiry-notice strong { color: #991b1b; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
        @media only screen and (max-width: 600px) {
            .container { margin: 0; border-radius: 0; }
            .content { padding: 20px 15px; }
            .cta-button { display: block; text-align: center; }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎟️ Complete Your Registration</h1>
            <div class=""status"">⏳ Payment Pending</div>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>
            <p>Thank you for starting your registration for <strong>{{EventTitle}}</strong>!</p>

            <div class=""alert-box"">
                <strong>⚠️ Action Required:</strong> Your registration is almost complete. To confirm your spot, please complete your payment within the next <strong>{{HoursRemaining}} hours</strong>.
            </div>

            <div class=""event-details"">
                <h3>📅 Event Details</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date:</strong> {{EventStartDate}}</p>
                <p><strong>Time:</strong> {{EventStartTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
            </div>

            <div class=""registration-summary"">
                <h3>✓ Registration Summary</h3>
                <p><strong>Number of Attendees:</strong> {{AttendeeCount}}</p>
                <p><strong>Total Amount:</strong> {{Currency}} {{TotalAmount}}</p>
            </div>

            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{{PaymentLink}}"" class=""cta-button"">
                    💳 Complete Payment Now
                </a>
            </div>

            <div class=""expiry-notice"">
                <strong>⏰ Checkout Expires:</strong> {{ExpiresAt}}<br>
                <em>If you do not complete payment within 24 hours, your registration will be automatically cancelled and your spot will be released to other attendees.</em>
            </div>

            <p style=""color: #6b7280; font-size: 14px; margin-top: 30px;"">
                Questions? Contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #10b981;"">{{SupportEmail}}</a>
            </p>
        </div>
        <div class=""footer"">
            <p>&copy; {{Year}} LankaConnect. All rights reserved.</p>
            <p>You''re receiving this email because you started a registration for an event on LankaConnect.</p>
        </div>
    </div>
</body>
</html>',
                    'EventRegistration',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-preliminary-registration-payment-pending'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1136));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1201));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1092));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1166));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1181));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1276));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1151));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1121));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1250));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1237));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1215));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 20, 27, 36, 604, DateTimeKind.Utc).AddTicks(1263));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.81 Part 3: Remove Preliminary Registration Payment Pending email template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-preliminary-registration-payment-pending';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(4841));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(5032));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(4700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(4953));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(4988));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(5286));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(4915));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(4768));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(5182));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(5136));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(5083));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 26, 14, 37, 6, 97, DateTimeKind.Utc).AddTicks(5232));
        }
    }
}
