using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A75_AddOrganizerRoleApprovedEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.75: Add organizer-role-approved email template
            // Sent to users when their EventOrganizer role request is approved by admin
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
                    'organizer-role-approved',
                    'Email sent to users when their EventOrganizer role upgrade request is approved',
                    'Welcome to LankaConnect Event Organizers!',
                    'Hi {{UserName}},

Great news! Your request to become an Event Organizer on LankaConnect has been approved.

YOUR NEW ROLE
-------------
Role: Event Organizer
Approved On: {{ApprovedAt}}
Trial Period: 6-month free trial

WHAT YOU CAN DO NOW
-------------------
- Create and publish events for the Sri Lankan community
- Manage event registrations and attendees
- Access event analytics and insights
- Create sign-up lists for volunteer coordination
- Send event updates to registered attendees

GET STARTED
-----------
Visit your dashboard to create your first event:
{{DashboardUrl}}

Need help getting started? Check out our organizer guide or contact support.

Thank you for joining the LankaConnect community of organizers!

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome Event Organizer - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Welcome, Event Organizer!</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                Great news! Your request to become an <strong style=""color: #8B1538;"">Event Organizer</strong> on LankaConnect has been <strong style=""color: #10b981;"">approved</strong>.
                            </p>

                            <!-- Role Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #f0fdf4; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #065f46; font-size: 18px;"">Your New Role</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Role:</strong> Event Organizer</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Approved:</strong> {{ApprovedAt}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Trial:</strong> 6-month free trial included</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- What You Can Do Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">What You Can Do Now</p>
                                        <ul style=""margin: 0; padding-left: 20px; color: #666; font-size: 14px; line-height: 1.8;"">
                                            <li>Create and publish events for the Sri Lankan community</li>
                                            <li>Manage event registrations and attendees</li>
                                            <li>Access event analytics and insights</li>
                                            <li>Create sign-up lists for volunteer coordination</li>
                                            <li>Send event updates to registered attendees</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{DashboardUrl}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">Go to Dashboard</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                Thank you for joining the LankaConnect community of organizers. We can''t wait to see the amazing events you''ll create!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'transactional',
                    'System',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'organizer-role-approved'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.75: Remove organizer-role-approved email template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'organizer-role-approved';
            ");
        }
    }
}
