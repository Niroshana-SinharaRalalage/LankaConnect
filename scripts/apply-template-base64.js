const { Client } = require('pg');
const https = require('https');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

// Fetch image and convert to base64
async function fetchImageAsBase64(url) {
  return new Promise((resolve, reject) => {
    https.get(url, (res) => {
      const chunks = [];
      res.on('data', chunk => chunks.push(chunk));
      res.on('end', () => {
        const buffer = Buffer.concat(chunks);
        const base64 = buffer.toString('base64');
        resolve(`data:image/png;base64,${base64}`);
      });
      res.on('error', reject);
    }).on('error', reject);
  });
}

async function applyTemplateMigration() {
  try {
    console.log('=== FETCHING IMAGES ===');

    const headerBannerUrl = 'https://lankaconnectstrgaccount.blob.core.windows.net/business-images/email-assets/email-header-banner.png';
    const footerBannerUrl = 'https://lankaconnectstrgaccount.blob.core.windows.net/business-images/email-assets/email-footer-banner.png';

    const headerBase64 = await fetchImageAsBase64(headerBannerUrl);
    const footerBase64 = await fetchImageAsBase64(footerBannerUrl);

    console.log('Header banner size:', headerBase64.length, 'chars');
    console.log('Footer banner size:', footerBase64.length, 'chars');

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
                <!-- Main Container - 650px width -->
                <table role="presentation" width="650" cellspacing="0" cellpadding="0" border="0" style="width: 650px; max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);">

                    <!-- Header Banner - Base64 embedded -->
                    <tr>
                        <td style="padding: 0; line-height: 0;">
                            <img src="${headerBase64}" alt="Registration Confirmed!" width="650" height="120" style="width: 650px; max-width: 650px; height: 120px; display: block; border: 0;">
                        </td>
                    </tr>

                    <!-- Event Image (conditional) - Direct URL -->
                    {{#HasEventImage}}
                    <tr>
                        <td style="padding: 0; line-height: 0; background: #f9fafb;">
                            <img src="{{EventImageUrl}}" alt="{{EventTitle}}" width="650" style="width: 650px; max-width: 650px; height: auto; display: block; border: 0;">
                        </td>
                    </tr>
                    {{/HasEventImage}}

                    <!-- Main Content Area -->
                    <tr>
                        <td style="padding: 32px 40px; background: #ffffff;">
                            <!-- Greeting -->
                            <p style="font-size: 18px; margin: 0 0 16px 0; color: #111827;">
                                Hi <span style="color: #ea580c; font-weight: 600;">{{UserName}}</span>,
                            </p>
                            <p style="font-size: 16px; margin: 0 0 24px 0; color: #374151;">
                                Thank you for registering for <span style="color: #9f1239; font-weight: 600;">{{EventTitle}}</span>!
                            </p>

                            <!-- Event Details Card -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 24px 0;">
                                <tr>
                                    <td style="background: linear-gradient(135deg, #fff7ed 0%, #fef2f2 100%); padding: 24px; border-left: 4px solid #ea580c; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #9f1239; margin: 0 0 16px 0; font-size: 18px; font-weight: 700;">Event Details</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 15px; color: #374151;">
                                                    <strong style="color: #111827;">Date:</strong> {{EventStartDate}} at {{EventStartTime}}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 15px; color: #374151;">
                                                    <strong style="color: #111827;">Location:</strong> {{EventLocation}}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 15px; color: #374151;">
                                                    <strong style="color: #111827;">Attendees:</strong> {{Quantity}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Registered Attendees Card -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 24px 0;">
                                <tr>
                                    <td style="background: #fef2f2; padding: 24px; border-radius: 12px; border: 1px solid #fecaca;">
                                        <h3 style="color: #9f1239; margin: 0 0 16px 0; font-size: 18px; font-weight: 700;">Registered Attendees</h3>
                                        <div style="color: #374151;">
                                            {{Attendees}}
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Contact Information Card -->
                            {{#HasContactInfo}}
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 24px 0;">
                                <tr>
                                    <td style="background: #ecfdf5; padding: 24px; border-left: 4px solid #166534; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #166534; margin: 0 0 16px 0; font-size: 18px; font-weight: 700;">Contact Information</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 15px; color: #374151;">
                                                    <strong style="color: #111827;">Email:</strong>
                                                    <a href="mailto:{{ContactEmail}}" style="color: #ea580c; text-decoration: none;">{{ContactEmail}}</a>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 15px; color: #374151;">
                                                    <strong style="color: #111827;">Phone:</strong> {{ContactPhone}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            {{/HasContactInfo}}

                            <!-- Closing Message -->
                            <p style="margin: 24px 0 0 0; font-size: 16px; color: #6b7280; text-align: center;">
                                We look forward to seeing you at the event!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer Banner - Base64 embedded -->
                    <tr>
                        <td style="padding: 0; line-height: 0;">
                            <img src="${footerBase64}" alt="LankaConnect - Sri Lankan Community Hub" width="650" height="100" style="width: 650px; max-width: 650px; height: 100px; display: block; border: 0;">
                        </td>
                    </tr>

                </table>

                <!-- Footer Text -->
                <table role="presentation" width="650" cellspacing="0" cellpadding="0" border="0" style="width: 650px; max-width: 650px;">
                    <tr>
                        <td style="padding: 24px 40px; text-align: center;">
                            <p style="margin: 0; font-size: 12px; color: #9ca3af;">
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

    await client.connect();

    console.log('\n=== APPLYING TEMPLATE MIGRATION (BASE64) ===');
    console.log('Template size:', htmlTemplate.length, 'chars');

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
             html_template LIKE '%data:image/png;base64%' as has_base64_images,
             html_template LIKE '%650%' as has_650_width
      FROM communications.email_templates
      WHERE name = 'registration-confirmation'
    `);
    console.log(JSON.stringify(verify.rows, null, 2));

    await client.end();
    console.log('\nTemplate migration with base64 images applied successfully!');
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

applyTemplateMigration();
