using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.34 Enhancement: Update registration-confirmation email template with LankaConnect branding
    /// - Saffron (#FF6600) and Maroon (#8B1538) theme colors
    /// - Event primary image support (conditional display)
    /// - LankaConnect logo in footer
    /// - Fixed footer text (removed "reply to this email" since it's do-not-reply)
    /// </summary>
    public partial class UpdateRegistrationTemplateWithBranding_Phase6A34 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update the existing registration-confirmation template with LankaConnect branding
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""html_template"" = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; background: white; }
        .header { background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 28px; font-weight: bold; }
        .event-image { width: 100%; max-height: 300px; object-fit: cover; }
        .content { padding: 30px 20px; background: #ffffff; }
        .event-info { background: #fff8f5; padding: 20px; margin: 20px 0; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0; }
        .event-info h3 { color: #8B1538; margin-top: 0; margin-bottom: 15px; font-size: 18px; }
        .event-info p { margin: 8px 0; }
        .attendee-list { background: #fef7f0; padding: 20px; margin: 20px 0; border-radius: 8px; border: 1px solid #ffe4cc; }
        .attendee-list h3 { color: #8B1538; margin-top: 0; margin-bottom: 15px; font-size: 18px; }
        .footer { background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center; }
        .footer-logo { width: 60px; height: 60px; margin-bottom: 10px; }
        .footer-brand { color: white; font-size: 18px; font-weight: bold; margin: 5px 0; }
        .footer-tagline { color: rgba(255,255,255,0.9); font-size: 12px; margin: 5px 0; }
        .footer-copyright { color: rgba(255,255,255,0.8); font-size: 11px; margin-top: 15px; }
        .highlight { color: #FF6600; font-weight: bold; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Registration Confirmed!</h1>
        </div>
        {{#HasEventImage}}
        <img src=""{{EventImageUrl}}"" alt=""{{EventTitle}}"" class=""event-image"">
        {{/HasEventImage}}
        <div class=""content"">
            <p>Hi <span class=""highlight"">{{UserName}}</span>,</p>
            <p>Thank you for registering for <strong>{{EventTitle}}</strong>!</p>
            <div class=""event-info"">
                <h3>Event Details</h3>
                <p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Quantity:</strong> {{Quantity}} attendee(s)</p>
            </div>
            <div class=""attendee-list"">
                <h3>Registered Attendees</h3>
                {{Attendees}}
            </div>
            <div class=""event-info"">
                <h3>Contact Information</h3>
                <p><strong>Email:</strong> {{ContactEmail}}</p>
                <p><strong>Phone:</strong> {{ContactPhone}}</p>
            </div>
            <p>We look forward to seeing you at the event!</p>
        </div>
        <div class=""footer"">
            <img src=""https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png"" alt=""LankaConnect"" class=""footer-logo"">
            <p class=""footer-brand"">LankaConnect</p>
            <p class=""footer-tagline"">Sri Lankan Community Hub</p>
            <p class=""footer-copyright"">&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    ""text_template"" = 'Hi {{UserName}},

Thank you for registering for {{EventTitle}}!

EVENT DETAILS
-------------
Date: {{EventStartDate}} at {{EventStartTime}}
Location: {{EventLocation}}
Quantity: {{Quantity}} attendee(s)

REGISTERED ATTENDEES
--------------------
{{Attendees}}

CONTACT INFORMATION
-------------------
Email: {{ContactEmail}}
Phone: {{ContactPhone}}

We look forward to seeing you at the event!

---
LankaConnect - Sri Lankan Community Hub
(c) 2025 LankaConnect. All rights reserved.',
                    ""updated_at"" = NOW()
                WHERE ""name"" = 'registration-confirmation';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the original blue-themed template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""html_template"" = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .event-info { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #2563eb; border-radius: 4px; }
        .attendee-list { background: #f3f4f6; padding: 10px; margin: 10px 0; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Registration Confirmed!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Thank you for registering for <strong>{{EventTitle}}</strong>!</p>
            <div class=""event-info"">
                <h3>Event Details</h3>
                <p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Quantity:</strong> {{Quantity}} attendee(s)</p>
            </div>
            <div class=""attendee-list"">
                <h3>Registered Attendees</h3>
                {{Attendees}}
            </div>
            <div class=""event-info"">
                <h3>Contact Information</h3>
                <p><strong>Email:</strong> {{ContactEmail}}</p>
                <p><strong>Phone:</strong> {{ContactPhone}}</p>
            </div>
            <p>We look forward to seeing you at the event!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
            <p>Questions? Reply to this email or visit our support page.</p>
        </div>
    </div>
</body>
</html>',
                    ""text_template"" = 'Hi {{UserName}},

Thank you for registering for {{EventTitle}}!

EVENT DETAILS
-------------
Date: {{EventStartDate}} at {{EventStartTime}}
Location: {{EventLocation}}
Quantity: {{Quantity}} attendee(s)

REGISTERED ATTENDEES
--------------------
{{Attendees}}

CONTACT INFORMATION
-------------------
Email: {{ContactEmail}}
Phone: {{ContactPhone}}

We look forward to seeing you at the event!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    ""updated_at"" = NOW()
                WHERE ""name"" = 'registration-confirmation';
            ");
        }
    }
}
