START TRANSACTION;


                INSERT INTO communications.email_templates
                (
                    "Id",
                    "name",
                    "description",
                    "subject_template",
                    "text_template",
                    "html_template",
                    "type",
                    "category",
                    "is_active",
                    "created_at"
                )
                VALUES
                (
                    gen_random_uuid(),
                    'registration-confirmation',
                    'Free event registration confirmation email sent when user registers for an event',
                    'Registration Confirmed for {{EventTitle}}',
                    'Hi {{UserName}},

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
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
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
    <div class="container">
        <div class="header">
            <h1>Registration Confirmed!</h1>
        </div>
        <div class="content">
            <p>Hi {{UserName}},</p>
            <p>Thank you for registering for <strong>{{EventTitle}}</strong>!</p>
            <div class="event-info">
                <h3>Event Details</h3>
                <p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Quantity:</strong> {{Quantity}} attendee(s)</p>
            </div>
            <div class="attendee-list">
                <h3>Registered Attendees</h3>
                {{Attendees}}
            </div>
            <div class="event-info">
                <h3>Contact Information</h3>
                <p><strong>Email:</strong> {{ContactEmail}}</p>
                <p><strong>Phone:</strong> {{ContactPhone}}</p>
            </div>
            <p>We look forward to seeing you at the event!</p>
        </div>
        <div class="footer">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
            <p>Questions? Reply to this email or visit our support page.</p>
        </div>
    </div>
</body>
</html>',
                    'Transactional',
                    'Event',
                    true,
                    NOW()
                );
            

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251219164841_SeedRegistrationConfirmationTemplate_Phase6A34', '8.0.19');

COMMIT;

