# Phase 6A.80: Email Delivery Verification Guide

## Quick Reference: How to Check if Email Was Sent

### Option 1: Check Email by Registration ID

```sql
-- Find email by registration ID
SELECT
    em."Id" as email_id,
    em.to_emails,
    em.template_name,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    em.error_message,
    em.retry_count
FROM communications.email_messages em
WHERE em.template_data::text LIKE '%YOUR_REGISTRATION_ID_HERE%'
ORDER BY em."CreatedAt" DESC;
```

### Option 2: Check Email by Email Address

```sql
-- Find recent emails sent to an address
SELECT
    "Id",
    template_name,
    status,
    "CreatedAt" as queued_at,
    sent_at,
    EXTRACT(EPOCH FROM (sent_at - "CreatedAt")) as seconds_to_send,
    error_message
FROM communications.email_messages
WHERE to_emails::text LIKE '%your.email@example.com%'
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

### Option 3: Check Email by Event ID

```sql
-- Find all emails sent for an event
SELECT
    em."Id",
    em.to_emails,
    em.template_name,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    EXTRACT(EPOCH FROM (sent_at - "CreatedAt")) as seconds_to_send
FROM communications.email_messages em
WHERE em.template_data::text LIKE '%YOUR_EVENT_ID_HERE%'
ORDER BY em."CreatedAt" DESC;
```

## Email Status Values

| Status | Meaning |
|--------|---------|
| `Queued` | Email is waiting to be sent by background job |
| `Sent` | Email was successfully sent to Azure Email Service |
| `Failed` | Email failed to send (check `error_message` column) |
| `Delivered` | Email was delivered to recipient's inbox (requires delivery tracking) |

## Normal Processing Times

- **Queueing**: Instant (when registration completes)
- **Background Processing**: 30-60 seconds (Hangfire job runs every 30s)
- **Azure Delivery**: 1-5 minutes (varies by recipient's email server)
- **Total Time**: 2-6 minutes from registration to inbox

## Example: Checking Your Latest Registration

```sql
-- Replace with your actual email and registration time
SELECT
    em."Id",
    em.template_name,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    CASE
        WHEN em.sent_at IS NOT NULL
        THEN CONCAT(EXTRACT(EPOCH FROM (em.sent_at - em."CreatedAt")), ' seconds')
        ELSE 'Not yet sent'
    END as processing_time,
    r.id as registration_id,
    r.created_at as registration_created_at
FROM communications.email_messages em
LEFT JOIN events.registrations r ON r.contact->>'Email' = (em.to_emails->>0)::text
WHERE em.to_emails::text LIKE '%lankaconnect.app@gmail.com%'
  AND em."CreatedAt" >= '2026-01-24 16:00:00'
ORDER BY em."CreatedAt" DESC
LIMIT 5;
```

## Troubleshooting

### Email Not in Queue After 5 Minutes

**Check if domain event fired:**
```sql
-- Check if registration exists
SELECT id, event_id, status, payment_status, created_at
FROM events.registrations
WHERE id = 'YOUR_REGISTRATION_ID';
```

If registration exists but no email queued:
- Domain event handler may have failed silently
- Check Azure logs for `AnonymousRegistrationConfirmed` errors

### Email Stuck in "Queued" Status

**Check background job:**
```sql
-- Check if any emails are being processed
SELECT status, COUNT(*)
FROM communications.email_messages
WHERE "CreatedAt" >= NOW() - INTERVAL '1 hour'
GROUP BY status;
```

If many emails stuck in Queued:
- Background job may be paused
- Azure Email Service credentials may be invalid
- Check Azure logs for `EmailQueueProcessor` errors

### Email Shows "Failed" Status

```sql
-- Get detailed error message
SELECT
    "Id",
    to_emails,
    error_message,
    retry_count,
    max_retries
FROM communications.email_messages
WHERE status = 'Failed'
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

Common failure reasons:
- Invalid email address
- Azure Email Service quota exceeded
- Network timeout
- Template rendering error

## Monitoring Dashboard Query

```sql
-- Email delivery metrics for last 24 hours
SELECT
    template_name,
    status,
    COUNT(*) as count,
    AVG(EXTRACT(EPOCH FROM (sent_at - "CreatedAt"))) as avg_seconds,
    MAX(EXTRACT(EPOCH FROM (sent_at - "CreatedAt"))) as max_seconds
FROM communications.email_messages
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
GROUP BY template_name, status
ORDER BY template_name, status;
```

## Phase 6A.80 Specific Verification

```sql
-- Verify anonymous registrations are using FreeEventRegistration template
SELECT
    em.template_name,
    COUNT(*) as email_count,
    AVG(EXTRACT(EPOCH FROM (em.sent_at - em."CreatedAt"))) as avg_processing_seconds
FROM communications.email_messages em
WHERE em.template_name = 'template-free-event-registration-confirmation'
  AND em."CreatedAt" >= '2026-01-24'
GROUP BY em.template_name;
```

Expected result: All anonymous free event registrations should use `template-free-event-registration-confirmation` (not `template-anonymous-rsvp-confirmation` which was deleted in Phase 6A.80).
