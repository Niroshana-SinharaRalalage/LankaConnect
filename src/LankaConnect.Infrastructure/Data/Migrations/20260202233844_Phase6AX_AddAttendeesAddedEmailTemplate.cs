using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class Phase6AX_AddAttendeesAddedEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.X: Add email template for Attendees Added confirmation
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
                    'template-attendees-added-confirmation',
                    'Phase 6A.X: Attendees added confirmation - sent when additional attendees are added to a paid registration',
                    'Your Registration Has Been Updated - {{EventTitle}}',
                    'Hi {{UserName}},

Great news! Your registration for ""{{EventTitle}}"" has been updated with additional attendees.

SUMMARY OF CHANGES
Previous attendee count: {{PreviousCount}}
Attendees added: {{AddedCount}}
New total attendees: {{NewTotalCount}}

NEW ATTENDEES ADDED
{{NewAttendees}}

PAYMENT SUMMARY
Additional amount paid: ${{AdditionalAmount}}
Total paid for this registration: ${{TotalPaid}}

ALL ATTENDEES
{{AllAttendees}}

EVENT DETAILS
Date: {{EventStartDate}}
Time: {{EventStartTime}}
Location: {{EventLocation}}

View your registration: {{EventDetailsUrl}}

Your updated ticket is attached to this email.

See you at the event!

Best regards,
The LankaConnect Team

---
This is an automated message from LankaConnect.
Please do not reply directly to this email.',
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
        .summary-box { background: #f0fdf4; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .summary-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #d1fae5; }
        .summary-row:last-child { border-bottom: none; }
        .payment-box { background: #fffbeb; border: 2px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .attendees-section { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .cta-button { display: inline-block; background: #8B1538; color: white !important; padding: 14px 28px; border-radius: 6px; text-decoration: none; font-weight: 600; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Registration Updated!</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>
            <p>Great news! Your registration for <strong>{{EventTitle}}</strong> has been updated with additional attendees.</p>

            <div class=""summary-box"">
                <h3>Summary of Changes</h3>
                <div class=""summary-row""><span>Previous attendee count</span><span>{{PreviousCount}}</span></div>
                <div class=""summary-row""><span>Attendees added</span><span>+{{AddedCount}}</span></div>
                <div class=""summary-row""><span>New total attendees</span><span>{{NewTotalCount}}</span></div>
            </div>

            <div class=""attendees-section"">
                <h3>New Attendees Added</h3>
                {{NewAttendeesHtml}}
            </div>

            <div class=""payment-box"">
                <h3>Payment Summary</h3>
                <div class=""summary-row""><span>Additional amount paid</span><span>${{AdditionalAmount}}</span></div>
                <div class=""summary-row""><span>Total paid for registration</span><span>${{TotalPaid}}</span></div>
            </div>

            <div class=""attendees-section"">
                <h3>All Attendees ({{NewTotalCount}})</h3>
                {{AllAttendeesHtml}}
            </div>

            <center><a href=""{{EventDetailsUrl}}"" class=""cta-button"">View Event Details</a></center>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>&copy; {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Registration',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-attendees-added-confirmation'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(927));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1054));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(767));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(995));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1025));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1311));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(961));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(882));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1151));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1120));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1087));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 23, 38, 41, 193, DateTimeKind.Utc).AddTicks(1182));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the email template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-attendees-added-confirmation';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3539));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3669));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3405));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3607));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3638));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3841));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3572));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3499));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3778));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3746));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3702));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3811));
        }
    }
}
