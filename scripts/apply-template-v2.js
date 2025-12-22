const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

// Phase 6A.40: Updated template with all user feedback:
// 1. LankaConnect NOT bold, with "Sri Lankan Community Hub" underneath
// 2. Removed bottom "LankaConnect / Sri Lankan Community Hub" - just centered copyright
// 3. Year changed to 2026
// 4. Removed all emojis
// 5. Date shows full range: start date/time to end date/time (handled by backend)
// 6. Removed "Attendees: X" count
// 7. Registered Attendees shows names only (no age) - handled by backend
// 8. Contact info uses correct phone from registration
// 9. Removed "We look forward to seeing you at the event!"
// 10. Changed footer text to "contact your event organizer"
// 11. Width: 850px

const htmlTemplate = `<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Registration Confirmed - LankaConnect</title>
</head>
<body style="font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f3f4f6;">
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f3f4f6;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <!-- Main Container - 850px width -->
                <table role="presentation" width="850" cellspacing="0" cellpadding="0" border="0" style="width: 850px; max-width: 850px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);">

                    <!-- Header Section - Gradient with LankaConnect branding -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 36px 50px; text-align: center;">
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="text-align: center; padding-bottom: 4px;">
                                                    <span style="font-size: 28px; font-weight: 400; color: #ffffff; letter-spacing: 0.5px;">LankaConnect</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="text-align: center; padding-bottom: 20px;">
                                                    <span style="font-size: 13px; font-weight: 400; color: #ffffff; opacity: 0.9;">Sri Lankan Community Hub</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="text-align: center;">
                                                    <span style="font-size: 26px; font-weight: 600; color: #ffffff;">Registration Confirmed!</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Event Image (conditional) - Direct URL -->
                    {{#HasEventImage}}
                    <tr>
                        <td style="padding: 0; line-height: 0; background: #f9fafb;">
                            <img src="{{EventImageUrl}}" alt="{{EventTitle}}" width="850" style="width: 850px; max-width: 850px; height: auto; display: block; border: 0;">
                        </td>
                    </tr>
                    {{/HasEventImage}}

                    <!-- Main Content Area -->
                    <tr>
                        <td style="padding: 36px 50px; background: #ffffff;">
                            <!-- Greeting -->
                            <p style="font-size: 20px; margin: 0 0 18px 0; color: #111827;">
                                Hi <span style="color: #ea580c; font-weight: 600;">{{UserName}}</span>,
                            </p>
                            <p style="font-size: 17px; margin: 0 0 28px 0; color: #374151;">
                                Thank you for registering for <span style="color: #9f1239; font-weight: 600;">{{EventTitle}}</span>!
                            </p>

                            <!-- Event Details Card -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #fff7ed; padding: 28px; border-left: 5px solid #ea580c; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #9f1239; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">Event Details</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Date:</strong> {{EventDateTime}}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Location:</strong> {{EventLocation}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Registered Attendees Card (only if has attendee details) -->
                            {{#HasAttendeeDetails}}
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #fef2f2; padding: 28px; border-radius: 12px; border: 1px solid #fecaca;">
                                        <h3 style="color: #9f1239; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">Registered Attendees</h3>
                                        <div style="color: #374151; font-size: 16px;">
                                            {{Attendees}}
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            {{/HasAttendeeDetails}}

                            <!-- Contact Information Card -->
                            {{#HasContactInfo}}
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #ecfdf5; padding: 28px; border-left: 5px solid #166534; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #166534; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">Your Contact Information</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Email:</strong>
                                                    <a href="mailto:{{ContactEmail}}" style="color: #ea580c; text-decoration: none;">{{ContactEmail}}</a>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Phone:</strong> {{ContactPhone}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            {{/HasContactInfo}}
                        </td>
                    </tr>

                    <!-- Footer Section - Simple centered copyright -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 24px 50px; text-align: center;">
                                        <span style="font-size: 14px; color: #ffffff;">© 2026 LankaConnect. All rights reserved.</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                </table>

                <!-- Footer Text -->
                <table role="presentation" width="850" cellspacing="0" cellpadding="0" border="0" style="width: 850px; max-width: 850px;">
                    <tr>
                        <td style="padding: 28px 50px; text-align: center;">
                            <p style="margin: 0; font-size: 13px; color: #9ca3af;">
                                This email was sent by LankaConnect. If you have any questions, please contact your event organizer.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>`;

async function applyTemplateMigration() {
  try {
    await client.connect();

    console.log('=== APPLYING TEMPLATE V2 (Phase 6A.40) ===');
    console.log('Template size:', htmlTemplate.length, 'chars');
    console.log('Changes:');
    console.log('  1. LankaConnect NOT bold, with tagline underneath');
    console.log('  2. Footer simplified - just centered copyright');
    console.log('  3. Year: 2026');
    console.log('  4. NO emojis');
    console.log('  5. Date shows {{EventDateTime}} (full range from backend)');
    console.log('  6. NO attendee count');
    console.log('  7. Attendees section conditional {{#HasAttendeeDetails}}');
    console.log('  8. Contact info from registration');
    console.log('  9. NO "We look forward..." message');
    console.log(' 10. Footer: "contact your event organizer"');

    const updateResult = await client.query(
      `UPDATE communications.email_templates
       SET html_template = $1, updated_at = NOW()
       WHERE name = 'registration-confirmation'
       RETURNING name, updated_at`,
      [htmlTemplate]
    );

    console.log('Updated:', updateResult.rows);

    // Verify the update
    console.log('\n=== VERIFYING TEMPLATE UPDATE ===');
    const verify = await client.query(`
      SELECT name, updated_at,
             LENGTH(html_template) as html_length,
             html_template LIKE '%850%' as has_850_width,
             html_template LIKE '%2026%' as has_2026_year,
             html_template LIKE '%contact your event organizer%' as has_new_footer,
             html_template LIKE '%EventDateTime%' as has_datetime_range,
             html_template LIKE '%HasAttendeeDetails%' as has_conditional_attendees
      FROM communications.email_templates
      WHERE name = 'registration-confirmation'
    `);
    console.log(JSON.stringify(verify.rows, null, 2));

    await client.end();
    console.log('\nTemplate V2 applied successfully!');
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

applyTemplateMigration();
