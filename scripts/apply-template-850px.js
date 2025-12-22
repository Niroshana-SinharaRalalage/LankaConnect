const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

// Phase 6A.40: Updated template with:
// 1. 850px width (increased from 650px)
// 2. EXACT gradient from landing page: from-orange-600 via-rose-800 to-emerald-800
//    - orange-600: #ea580c
//    - rose-800: #9f1239
//    - emerald-800: #166534
// 3. Horizontal gradient (left to right) matching the landing page hero section

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

                    <!-- Header Section - EXACT Landing Page Gradient (horizontal: orange -> rose -> emerald) -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 36px 50px; text-align: center;">
                                        <!-- Logo Text -->
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="text-align: center; padding-bottom: 16px;">
                                                    <span style="font-size: 36px; font-weight: 700; color: #ffffff; letter-spacing: 1px;">LankaConnect</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="text-align: center;">
                                                    <span style="font-size: 26px; font-weight: 600; color: #ffffff;">✓ Registration Confirmed!</span>
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
                                        <h3 style="color: #9f1239; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">📅 Event Details</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Date:</strong> {{EventStartDate}} at {{EventStartTime}}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Location:</strong> {{EventLocation}}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Attendees:</strong> {{Quantity}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Registered Attendees Card -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #fef2f2; padding: 28px; border-radius: 12px; border: 1px solid #fecaca;">
                                        <h3 style="color: #9f1239; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">👥 Registered Attendees</h3>
                                        <div style="color: #374151; font-size: 16px;">
                                            {{Attendees}}
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Contact Information Card -->
                            {{#HasContactInfo}}
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #ecfdf5; padding: 28px; border-left: 5px solid #166534; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #166534; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">📧 Contact Information</h3>
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

                            <!-- Closing Message -->
                            <p style="margin: 28px 0 0 0; font-size: 17px; color: #6b7280; text-align: center;">
                                We look forward to seeing you at the event! 🎉
                            </p>
                        </td>
                    </tr>

                    <!-- Footer Section - EXACT Landing Page Gradient (horizontal: orange -> rose -> emerald) -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 28px 50px;">
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="color: #ffffff;">
                                                    <span style="font-size: 22px; font-weight: 700;">LankaConnect</span><br>
                                                    <span style="font-size: 14px; opacity: 0.9;">Sri Lankan Community Hub</span>
                                                </td>
                                                <td style="text-align: right; color: #ffffff; font-size: 13px; opacity: 0.9;">
                                                    © 2025 LankaConnect<br>
                                                    All rights reserved
                                                </td>
                                            </tr>
                                        </table>
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
                                This email was sent by LankaConnect. If you have any questions, please contact us.
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

    console.log('=== APPLYING 850px TEMPLATE WITH EXACT GRADIENT (Phase 6A.40) ===');
    console.log('Template size:', htmlTemplate.length, 'chars');
    console.log('Width: 850px');
    console.log('Gradient: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%)');
    console.log('         (orange-600 -> rose-800 -> emerald-800, horizontal left-to-right)');

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
             html_template LIKE '%to right%' as has_horizontal_gradient,
             html_template LIKE '%#ea580c%' as has_orange_600,
             html_template LIKE '%#9f1239%' as has_rose_800,
             html_template LIKE '%#166534%' as has_emerald_800
      FROM communications.email_templates
      WHERE name = 'registration-confirmation'
    `);
    console.log(JSON.stringify(verify.rows, null, 2));

    await client.end();
    console.log('\n850px template with exact landing page gradient applied successfully!');
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

applyTemplateMigration();
