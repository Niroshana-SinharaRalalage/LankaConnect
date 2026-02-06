-- Check newsletter 9b351ee0-9561-4d8a-82b0-d4011abb6139 status
SELECT
    id,
    title,
    status,
    sent_at,
    event_id,
    include_newsletter_subscribers,
    created_at,
    updated_at
FROM communications.newsletters
WHERE id = '9b351ee0-9561-4d8a-82b0-d4011abb6139';

-- Check if history exists
SELECT *
FROM communications.newsletter_email_history
WHERE newsletter_id = '9b351ee0-9561-4d8a-82b0-d4011abb6139';