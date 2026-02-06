using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.34 Enhancement: Improve registration email template
    /// - Increased width from 600px to 650px for better readability
    /// - Added decorative Sri Lankan cultural patterns (stars) in header using text
    /// - Table-based layout for better email client compatibility
    /// - Inline styles for maximum compatibility
    /// </summary>
    public partial class EnhanceEmailTemplateLayoutAndDecorations_Phase6A34 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update the registration-confirmation template with enhanced layout
            // Using table-based layout and inline styles for email client compatibility
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""html_template"" = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Registration Confirmed</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f5f5f5;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f5f5f5;"">
        <tr>
            <td align=""center"" style=""padding: 20px 10px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
                    <!-- Header with gradient and decorations -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <!-- Top decoration row -->
                                <tr>
                                    <td align=""center"" style=""padding: 10px 0 5px 0; font-size: 14px; letter-spacing: 8px; color: rgba(255,255,255,0.25);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <!-- Main title -->
                                <tr>
                                    <td align=""center"" style=""padding: 15px 25px; color: white;"">
                                        <h1 style=""margin: 0; font-size: 28px; font-weight: bold;"">Registration Confirmed!</h1>
                                    </td>
                                </tr>
                                <!-- Bottom decoration row -->
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0 10px 0; font-size: 14px; letter-spacing: 8px; color: rgba(255,255,255,0.25);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Event Image (conditional) -->
                    {{#HasEventImage}}
                    <tr>
                        <td style=""background: #f9f9f9; padding: 0;"">
                            <img src=""{{EventImageUrl}}"" alt=""{{EventTitle}}"" width=""650"" style=""width: 100%; max-width: 650px; height: auto; display: block; border: 0;"">
                        </td>
                    </tr>
                    {{/HasEventImage}}
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px 25px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 15px 0;"">Hi <span style=""color: #FF6600; font-weight: bold;"">{{UserName}}</span>,</p>
                            <p style=""margin: 0 0 20px 0;"">Thank you for registering for <span style=""color: #8B1538; font-weight: bold;"">{{EventTitle}}</span>!</p>

                            <!-- Event Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #fff8f5; padding: 20px; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                                        <h3 style=""color: #8B1538; margin: 0 0 15px 0; font-size: 18px; font-weight: bold;"">Event Details</h3>
                                        <p style=""margin: 10px 0; font-size: 15px;""><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style=""margin: 10px 0; font-size: 15px;""><strong>Location:</strong> {{EventLocation}}</p>
                                        <p style=""margin: 10px 0; font-size: 15px;""><strong>Quantity:</strong> {{Quantity}} attendee(s)</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Registered Attendees Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #fef7f0; padding: 20px; border-radius: 8px; border: 1px solid #ffe4cc;"">
                                        <h3 style=""color: #8B1538; margin: 0 0 15px 0; font-size: 18px; font-weight: bold;"">Registered Attendees</h3>
                                        {{Attendees}}
                                    </td>
                                </tr>
                            </table>

                            <!-- Contact Information Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #fff8f5; padding: 20px; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                                        <h3 style=""color: #8B1538; margin: 0 0 15px 0; font-size: 18px; font-weight: bold;"">Contact Information</h3>
                                        <p style=""margin: 10px 0; font-size: 15px;""><strong>Email:</strong> <a href=""mailto:{{ContactEmail}}"" style=""color: #FF6600; text-decoration: none;"">{{ContactEmail}}</a></p>
                                        <p style=""margin: 10px 0; font-size: 15px;""><strong>Phone:</strong> {{ContactPhone}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 25px 0 0 0; font-size: 15px; color: #555555;"">We look forward to seeing you at the event!</p>
                        </td>
                    </tr>
                    <!-- Footer with gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <!-- Top decoration -->
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 10px 0; font-size: 12px; letter-spacing: 6px; color: rgba(255,255,255,0.2);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <!-- Logo -->
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <img src=""https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png"" alt=""LankaConnect"" width=""70"" height=""70"" style=""width: 70px; height: 70px; border-radius: 50%; background: white; padding: 5px; display: block;"">
                                    </td>
                                </tr>
                                <!-- Brand name -->
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0 0 0;"">
                                        <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0;"">LankaConnect</p>
                                    </td>
                                </tr>
                                <!-- Tagline -->
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <p style=""color: rgba(255,255,255,0.9); font-size: 13px; margin: 0;"">Sri Lankan Community Hub</p>
                                    </td>
                                </tr>
                                <!-- Copyright -->
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 20px 0;"">
                                        <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    ""updated_at"" = NOW()
                WHERE ""name"" = 'registration-confirmation';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the previous version (simpler branded template)
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
                    ""updated_at"" = NOW()
                WHERE ""name"" = 'registration-confirmation';
            ");
        }
    }
}
