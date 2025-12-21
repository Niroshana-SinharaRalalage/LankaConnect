const { Client } = require('pg');
const fs = require('fs');
const path = require('path');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

// Phase 6A.36: Updated email template with:
// 1. Corrected gradient direction to match landing page (orange → rose/maroon → emerald)
// 2. Removed external image URL (won't work in email clients) - using pure CSS logo fallback
// 3. 650px width maintained
// 4. Cross/plus pattern simulated with + symbols (closer to landing page SVG pattern)
// 5. All inline styles for maximum email client compatibility
// 6. REMOVED event image section - external images blocked by email clients by default
const htmlTemplate = `<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Registration Confirmed</title>
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f5f5f5;">
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f5f5f5;">
        <tr>
            <td align="center" style="padding: 20px 10px;">
                <table role="presentation" width="650" cellspacing="0" cellpadding="0" border="0" style="max-width: 650px; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                    <!-- Header with gradient matching landing page: orange → rose/maroon → emerald -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <!-- Top decoration row with cross/plus pattern -->
                                <tr>
                                    <td align="center" style="padding: 12px 0 8px 0; font-size: 16px; letter-spacing: 12px; color: rgba(255,255,255,0.15);">
                                        + + + + + + + + + + +
                                    </td>
                                </tr>
                                <!-- Main title -->
                                <tr>
                                    <td align="center" style="padding: 15px 25px; color: white;">
                                        <h1 style="margin: 0; font-size: 28px; font-weight: bold; text-shadow: 0 1px 2px rgba(0,0,0,0.2);">Registration Confirmed!</h1>
                                    </td>
                                </tr>
                                <!-- Bottom decoration row -->
                                <tr>
                                    <td align="center" style="padding: 8px 0 12px 0; font-size: 16px; letter-spacing: 12px; color: rgba(255,255,255,0.15);">
                                        + + + + + + + + + + +
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style="padding: 30px 25px; background: #ffffff;">
                            <p style="font-size: 16px; margin: 0 0 15px 0;">Hi <span style="color: #ea580c; font-weight: bold;">{{UserName}}</span>,</p>
                            <p style="margin: 0 0 20px 0;">Thank you for registering for <span style="color: #9f1239; font-weight: bold;">{{EventTitle}}</span>!</p>

                            <!-- Event Details Box -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 20px 0;">
                                <tr>
                                    <td style="background: #fff7ed; padding: 20px; border-left: 4px solid #ea580c; border-radius: 0 8px 8px 0;">
                                        <h3 style="color: #9f1239; margin: 0 0 15px 0; font-size: 18px; font-weight: bold;">Event Details</h3>
                                        <p style="margin: 10px 0; font-size: 15px;"><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style="margin: 10px 0; font-size: 15px;"><strong>Location:</strong> {{EventLocation}}</p>
                                        <p style="margin: 10px 0; font-size: 15px;"><strong>Quantity:</strong> {{Quantity}} attendee(s)</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Registered Attendees Box -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 20px 0;">
                                <tr>
                                    <td style="background: #fef2f2; padding: 20px; border-radius: 8px; border: 1px solid #fecaca;">
                                        <h3 style="color: #9f1239; margin: 0 0 15px 0; font-size: 18px; font-weight: bold;">Registered Attendees</h3>
                                        {{Attendees}}
                                    </td>
                                </tr>
                            </table>

                            <!-- Contact Information Box -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 20px 0;">
                                <tr>
                                    <td style="background: #fff7ed; padding: 20px; border-left: 4px solid #ea580c; border-radius: 0 8px 8px 0;">
                                        <h3 style="color: #9f1239; margin: 0 0 15px 0; font-size: 18px; font-weight: bold;">Contact Information</h3>
                                        <p style="margin: 10px 0; font-size: 15px;"><strong>Email:</strong> <a href="mailto:{{ContactEmail}}" style="color: #ea580c; text-decoration: none;">{{ContactEmail}}</a></p>
                                        <p style="margin: 10px 0; font-size: 15px;"><strong>Phone:</strong> {{ContactPhone}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style="margin: 25px 0 0 0; font-size: 15px; color: #555555;">We look forward to seeing you at the event!</p>
                        </td>
                    </tr>
                    <!-- Footer with gradient matching landing page -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <!-- Top decoration -->
                                <tr>
                                    <td align="center" style="padding: 15px 0 10px 0; font-size: 14px; letter-spacing: 10px; color: rgba(255,255,255,0.12);">
                                        + + + + + + + + + + +
                                    </td>
                                </tr>
                                <!-- CSS-based logo circle (no external image) -->
                                <tr>
                                    <td align="center" style="padding: 5px 0;">
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="width: 70px; height: 70px; background: white; border-radius: 35px; text-align: center; vertical-align: middle;">
                                                    <span style="color: #9f1239; font-size: 28px; font-weight: bold; line-height: 70px;">LC</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <!-- Brand name -->
                                <tr>
                                    <td align="center" style="padding: 10px 0 0 0;">
                                        <p style="color: white; font-size: 22px; font-weight: bold; margin: 0; text-shadow: 0 1px 2px rgba(0,0,0,0.2);">LankaConnect</p>
                                    </td>
                                </tr>
                                <!-- Tagline -->
                                <tr>
                                    <td align="center" style="padding: 5px 0;">
                                        <p style="color: rgba(255,255,255,0.9); font-size: 14px; margin: 0;">Sri Lankan Community Hub</p>
                                    </td>
                                </tr>
                                <!-- Copyright -->
                                <tr>
                                    <td align="center" style="padding: 15px 0 20px 0;">
                                        <p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;">&copy; 2025 LankaConnect. All rights reserved.</p>
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
</html>`;

async function applyTemplateMigration() {
  try {
    await client.connect();

    console.log('=== APPLYING TEMPLATE MIGRATION ===');

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
             html_template LIKE '%650%' as has_650_width,
             html_template LIKE '%✦%' as has_stars,
             html_template LIKE '%table role%' as has_table_layout
      FROM communications.email_templates
      WHERE name = 'registration-confirmation'
    `);
    console.log(JSON.stringify(verify.rows, null, 2));

    await client.end();
    console.log('\nTemplate migration applied successfully!');
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

applyTemplateMigration();
