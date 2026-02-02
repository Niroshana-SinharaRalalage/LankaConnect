using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.X: Add email template for Attendees Added confirmation.
    /// Part of the Add-Only Attendees with Delta Payment feature.
    /// This template is sent when additional attendees are added to an existing paid registration.
    /// </summary>
    public partial class Phase6AX_AddAttendeesAddedEmailTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        .header .subtitle { margin-top: 8px; opacity: 0.9; font-size: 14px; }
        .content { padding: 30px 20px; }
        .success-badge { display: inline-block; background: #ecfdf5; color: #059669; padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: 600; margin-bottom: 20px; }
        .summary-box { background: #f0fdf4; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .summary-box h3 { margin: 0 0 15px 0; color: #065f46; font-size: 16px; }
        .summary-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #d1fae5; }
        .summary-row:last-child { border-bottom: none; }
        .summary-label { color: #065f46; }
        .summary-value { color: #047857; font-weight: 600; }
        .highlight-value { font-size: 20px; color: #059669; font-weight: 700; }
        .attendees-section { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .attendees-section h3 { margin: 0 0 15px 0; color: #374151; font-size: 16px; }
        .attendee-item { display: flex; align-items: center; padding: 10px 0; border-bottom: 1px solid #e5e7eb; }
        .attendee-item:last-child { border-bottom: none; }
        .attendee-icon { width: 32px; height: 32px; background: #dbeafe; color: #3b82f6; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin-right: 12px; font-weight: 600; }
        .attendee-icon.new { background: #dcfce7; color: #16a34a; }
        .attendee-name { flex: 1; }
        .attendee-badge { font-size: 10px; background: #dcfce7; color: #16a34a; padding: 2px 8px; border-radius: 10px; margin-left: 8px; }
        .payment-box { background: #fffbeb; border: 2px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .payment-box h3 { margin: 0 0 15px 0; color: #92400e; font-size: 16px; }
        .payment-row { display: flex; justify-content: space-between; padding: 8px 0; }
        .payment-total { border-top: 2px solid #fde68a; margin-top: 10px; padding-top: 10px; }
        .payment-total .label { font-weight: 600; color: #78350f; }
        .payment-total .amount { font-size: 20px; font-weight: 700; color: #b45309; }
        .event-details { background: #f3f4f6; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .event-details h3 { margin: 0 0 15px 0; color: #374151; font-size: 16px; }
        .detail-row { display: flex; padding: 8px 0; }
        .detail-icon { width: 24px; color: #6b7280; margin-right: 12px; }
        .detail-text { color: #374151; }
        .cta-button { display: inline-block; background: #8B1538; color: white !important; padding: 14px 28px; border-radius: 6px; text-decoration: none; font-weight: 600; margin: 20px 0; }
        .ticket-note { background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; border-radius: 0 6px 6px 0; }
        .ticket-note p { margin: 0; color: #1e40af; font-size: 14px; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Registration Updated!</h1>
            <div class=""subtitle"">Additional attendees have been added</div>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <span class=""success-badge"">&#10003; Attendees Added Successfully</span>

            <p>Great news! Your registration for <strong>{{EventTitle}}</strong> has been updated with additional attendees.</p>

            <div class=""summary-box"">
                <h3>Summary of Changes</h3>
                <div class=""summary-row"">
                    <span class=""summary-label"">Previous attendee count</span>
                    <span class=""summary-value"">{{PreviousCount}}</span>
                </div>
                <div class=""summary-row"">
                    <span class=""summary-label"">Attendees added</span>
                    <span class=""summary-value highlight-value"">+{{AddedCount}}</span>
                </div>
                <div class=""summary-row"">
                    <span class=""summary-label"">New total attendees</span>
                    <span class=""summary-value"">{{NewTotalCount}}</span>
                </div>
            </div>

            <div class=""attendees-section"">
                <h3>New Attendees Added</h3>
                {{NewAttendeesHtml}}
            </div>

            <div class=""payment-box"">
                <h3>Payment Summary</h3>
                <div class=""payment-row"">
                    <span>Additional amount paid</span>
                    <span><strong>${{AdditionalAmount}}</strong></span>
                </div>
                <div class=""payment-row payment-total"">
                    <span class=""label"">Total paid for registration</span>
                    <span class=""amount"">${{TotalPaid}}</span>
                </div>
            </div>

            <div class=""attendees-section"">
                <h3>All Attendees ({{NewTotalCount}})</h3>
                {{AllAttendeesHtml}}
            </div>

            <div class=""event-details"">
                <h3>Event Details</h3>
                <div class=""detail-row"">
                    <span class=""detail-icon"">&#128197;</span>
                    <span class=""detail-text"">{{EventStartDate}}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-icon"">&#128336;</span>
                    <span class=""detail-text"">{{EventStartTime}}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-icon"">&#128205;</span>
                    <span class=""detail-text"">{{EventLocation}}</span>
                </div>
            </div>

            <div class=""ticket-note"">
                <p><strong>&#127915; Your Updated Ticket:</strong> An updated ticket PDF with all attendees is attached to this email.</p>
            </div>

            <center>
                <a href=""{{EventDetailsUrl}}"" class=""cta-button"">View Event Details</a>
            </center>

            <p>See you at the event!</p>
            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>&copy; {{Year}} LankaConnect. All rights reserved.</p>
            <p>This is an automated message. Please do not reply directly to this email.</p>
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-attendees-added-confirmation';
            ");
        }
    }
}
