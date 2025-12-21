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

// Phase 6A.37: Updated email template with:
// 1. CID-embedded header banner image (downloaded from Azure, not external URL)
// 2. CID-embedded event image (downloaded and embedded inline)
// 3. CID-embedded footer banner with logo
// 4. 650px width maintained
// 5. All inline styles for maximum email client compatibility
// 6. Images use src="cid:{ContentId}" for inline embedding
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
                    <!-- Header Banner (CID embedded) -->
                    <tr>
                        <td style="padding: 0;">
                            <img src="cid:email-header-banner" alt="Registration Confirmed!" width="650" style="width: 100%; max-width: 650px; height: auto; display: block; border: 0;">
                        </td>
                    </tr>
                    <!-- Event Image (CID embedded, conditional) -->
                    {{#HasEventImage}}
                    <tr>
                        <td style="background: #f9f9f9; padding: 0;">
                            <img src="cid:event-image" alt="{{EventTitle}}" width="650" style="width: 100%; max-width: 650px; height: auto; display: block; border: 0;">
                        </td>
                    </tr>
                    {{/HasEventImage}}
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
                    <!-- Footer Banner (CID embedded) -->
                    <tr>
                        <td style="padding: 0;">
                            <img src="cid:email-footer-banner" alt="LankaConnect" width="650" style="width: 100%; max-width: 650px; height: auto; display: block; border: 0;">
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
             html_template LIKE '%cid:email-header-banner%' as has_header_cid,
             html_template LIKE '%cid:email-footer-banner%' as has_footer_cid,
             html_template LIKE '%cid:event-image%' as has_event_image_cid,
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
