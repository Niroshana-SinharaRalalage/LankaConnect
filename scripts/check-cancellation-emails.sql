-- Query to check emails sent for cancelled event
-- Event ID: 63d4d2c2-e232-4abc-bd25-508361e3ab45

-- Check email messages sent in last hour
SELECT
    id,
    recipient_email,
    subject,
    status,
    sent_at,
    error_message,
    created_at
FROM communications.email_messages
WHERE created_at >= NOW() - INTERVAL '1 hour'
    AND subject LIKE '%cancel%'
ORDER BY created_at DESC;

-- Alternative: Search by event ID in email body or metadata
SELECT
    id,
    recipient_email,
    subject,
    status,
    sent_at,
    created_at
FROM communications.email_messages
WHERE created_at >= NOW() - INTERVAL '1 hour'
    AND (
        body LIKE '%63d4d2c2-e232-4abc-bd25-508361e3ab45%'
        OR body LIKE '%Test cancellation%'
    )
ORDER BY created_at DESC;
