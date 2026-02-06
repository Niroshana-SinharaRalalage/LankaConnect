const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

// Phase 6A.43: Paid event ticket-confirmation template
// Aligned with free event registration-confirmation template:
// 1. Same gradient header with "Registration Confirmed!"
// 2. Same 850px width, inline styles
// 3. Same footer branding (no copyright)
// 4. Attendee names only (no age)
// 5. Full date/time range
// 6. Added Payment Confirmation and Ticket sections

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

                    <!-- Header Section - Gradient with "Registration Confirmed!" -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 36px 50px; text-align: center;">
                                        <span style="font-size: 28px; font-weight: 600; color: #ffffff;">Registration Confirmed!</span>
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

                            <!-- Registered Attendees Card (conditional) -->
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

                            <!-- Payment Confirmation Card -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #f0fdf4; padding: 28px; border-left: 5px solid #166534; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #166534; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">Payment Confirmation</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Amount Paid:</strong> {{AmountPaid}}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Payment ID:</strong> <span style="font-family: monospace; background: #f3f4f6; padding: 2px 6px; border-radius: 4px;">{{PaymentIntentId}}</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Payment Date:</strong> {{PaymentDate}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Your Ticket Card (conditional) -->
                            {{#HasTicket}}
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 0 0 28px 0;">
                                <tr>
                                    <td style="background: #eff6ff; padding: 28px; border-left: 5px solid #2563eb; border-radius: 0 12px 12px 0;">
                                        <h3 style="color: #1d4ed8; margin: 0 0 18px 0; font-size: 20px; font-weight: 700;">Your Ticket</h3>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Ticket Code:</strong> <span style="font-family: monospace; background: #dbeafe; padding: 4px 10px; border-radius: 4px; font-weight: 600; color: #1d4ed8;">{{TicketCode}}</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    Your ticket is attached to this email as a PDF. Please present it at the event entrance.
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 10px 0; font-size: 16px; color: #374151;">
                                                    <strong style="color: #111827;">Valid Until:</strong> {{TicketExpiryDate}}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            {{/HasTicket}}

                            <!-- Contact Information Card (conditional) -->
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

                    <!-- Footer Section - Branding only -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 28px 50px; text-align: center;">
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                            <tr>
                                                <td style="text-align: center; padding-bottom: 4px;">
                                                    <span style="font-size: 24px; font-weight: 400; color: #ffffff; letter-spacing: 0.5px;">LankaConnect</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="text-align: center;">
                                                    <span style="font-size: 13px; font-weight: 400; color: #ffffff; opacity: 0.9;">Sri Lankan Community Hub</span>
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

const textTemplate = `Hi {{UserName}},

Thank you for registering for {{EventTitle}}!

EVENT DETAILS
-------------
Date: {{EventDateTime}}
Location: {{EventLocation}}

{{#HasAttendeeDetails}}
REGISTERED ATTENDEES
--------------------
{{Attendees}}
{{/HasAttendeeDetails}}

PAYMENT CONFIRMATION
--------------------
Amount Paid: {{AmountPaid}}
Payment ID: {{PaymentIntentId}}
Payment Date: {{PaymentDate}}

{{#HasTicket}}
YOUR TICKET
-----------
Ticket Code: {{TicketCode}}
Your ticket is attached to this email as a PDF. Please present it at the event entrance.
Valid Until: {{TicketExpiryDate}}
{{/HasTicket}}

{{#HasContactInfo}}
YOUR CONTACT INFORMATION
------------------------
Email: {{ContactEmail}}
Phone: {{ContactPhone}}
{{/HasContactInfo}}

---
LankaConnect - Sri Lankan Community Hub
If you have any questions, please contact your event organizer.`;

const subjectTemplate = `Your Ticket for {{EventTitle}}`;

async function applyTicketTemplate() {
  try {
    await client.connect();

    console.log('=== APPLYING TICKET-CONFIRMATION TEMPLATE (Phase 6A.43) ===');
    console.log('HTML Template size:', htmlTemplate.length, 'chars');
    console.log('Text Template size:', textTemplate.length, 'chars');
    console.log('Changes:');
    console.log('  1. Same gradient header as free event template');
    console.log('  2. 850px width with inline styles');
    console.log('  3. Attendee names only (no age)');
    console.log('  4. Full date/time range (EventDateTime)');
    console.log('  5. Payment Confirmation card (green)');
    console.log('  6. Your Ticket card (blue)');
    console.log('  7. Footer: LankaConnect + Sri Lankan Community Hub');
    console.log('  8. No copyright, no "We look forward..."');

    const updateResult = await client.query(
      `UPDATE communications.email_templates
       SET html_template = $1,
           text_template = $2,
           subject_template = $3,
           updated_at = NOW()
       WHERE name = 'ticket-confirmation'
       RETURNING name, updated_at`,
      [htmlTemplate, textTemplate, subjectTemplate]
    );

    if (updateResult.rowCount === 0) {
      console.log('Template not found, inserting new...');
      await client.query(
        `INSERT INTO communications.email_templates
         ("Id", name, description, subject_template, text_template, html_template, type, category, is_active, created_at)
         VALUES (gen_random_uuid(), 'ticket-confirmation', 'Paid event ticket confirmation (Phase 6A.43)', $1, $2, $3, 'Transactional', 'Event', true, NOW())`,
        [subjectTemplate, textTemplate, htmlTemplate]
      );
      console.log('Inserted new template');
    } else {
      console.log('Updated:', updateResult.rows);
    }

    // Verify the update
    console.log('\n=== VERIFYING TEMPLATE UPDATE ===');
    const verify = await client.query(`
      SELECT name, updated_at,
             LENGTH(html_template) as html_length,
             html_template LIKE '%850%' as has_850_width,
             html_template LIKE '%linear-gradient%' as has_gradient,
             html_template LIKE '%EventDateTime%' as has_datetime_range,
             html_template LIKE '%HasAttendeeDetails%' as has_conditional_attendees,
             html_template LIKE '%PaymentIntentId%' as has_payment_section,
             html_template LIKE '%TicketCode%' as has_ticket_section
      FROM communications.email_templates
      WHERE name = 'ticket-confirmation'
    `);
    console.log(JSON.stringify(verify.rows, null, 2));

    await client.end();
    console.log('\nTicket confirmation template applied successfully!');
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

applyTicketTemplate();
