-- SENIOR ENGINEER APPROACH: Check ACTUAL database state

-- 1. Check the newsletter status
SELECT 
    id,
    title,
    status,
    sent_at,
    event_id,
    include_newsletter_subscribers,
    created_at
FROM communications.newsletters
WHERE id = 'd57233a0-dd5c-4e50-bc26-7aef255d539f';

-- 2. Check if NewsletterEmailHistory was created (SMOKING GUN TEST)
SELECT 
    id,
    newsletter_id,
    sent_at,
    total_recipients,
    email_group_recipients,
    subscriber_recipients,
    created_at
FROM communications.newsletter_email_history
WHERE newsletter_id = 'd57233a0-dd5c-4e50-bc26-7aef255d539f';

-- 3. Check email group email addresses
SELECT 
    id,
    name,
    email_addresses,
    LENGTH(email_addresses) as addr_length,
    is_active
FROM communications.email_groups
WHERE name LIKE '%Cleveland SL Community%' 
   OR name LIKE '%Test Group 1%';

