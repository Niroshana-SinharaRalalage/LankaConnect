#!/bin/bash
# Phase 6A.74 Part 13 - Check newsletter logs after deployment
# This script helps verify Issue #1/#2 fix in staging environment

echo "=== Phase 6A.74 Part 13 - Newsletter Email History Fix Verification ==="
echo ""
echo "Instructions:"
echo "1. Wait for deployment to complete (commit 8500f85b)"
echo "2. Send a test newsletter from staging UI"
echo "3. Check Azure logs for Phase 6A.74 Part 13 Issue #1/#2 RCA entries"
echo "4. Run the SQL query below to verify Newsletter.SentAt and NewsletterEmailHistory"
echo ""
echo "=== Azure Log Query ==="
echo "Search for: [Phase 6A.74 Part 13 Issue #1/#2 RCA]"
echo "Expected log entries:"
echo "  1. 'Detaching EmailMessage entities before commit'"
echo "  2. 'Found X EmailMessage entities to detach'"
echo "  3. 'Detached X EmailMessage entities, Newsletter and NewsletterEmailHistory still tracked'"
echo "  4. 'Newsletter entity updated successfully'"
echo "  5. 'NewsletterEmailHistory record created successfully'"
echo ""
echo "=== SQL Verification Query ==="
cat << 'SQL'

-- Check most recent newsletter
SELECT
    n.id,
    n.title,
    n.status,
    n.sent_at,
    n.published_at,
    n.created_at
FROM communications.newsletters n
ORDER BY n.created_at DESC
LIMIT 1;

-- Check newsletter email history for that newsletter
SELECT
    neh.id,
    neh.newsletter_id,
    neh.total_recipients,
    neh.successful_sends,
    neh.failed_sends,
    neh.sent_at,
    neh.created_at
FROM communications.newsletter_email_history neh
WHERE neh.newsletter_id = (
    SELECT id FROM communications.newsletters
    ORDER BY created_at DESC
    LIMIT 1
);

-- Verify both entities were saved correctly
SELECT
    n.id as newsletter_id,
    n.title,
    n.status,
    n.sent_at as newsletter_sent_at,
    neh.id as history_id,
    neh.total_recipients,
    neh.successful_sends,
    neh.failed_sends,
    neh.sent_at as history_sent_at
FROM communications.newsletters n
LEFT JOIN communications.newsletter_email_history neh ON n.id = neh.newsletter_id
ORDER BY n.created_at DESC
LIMIT 5;

SQL

echo ""
echo "=== Expected Results ==="
echo "✅ Newsletter.sent_at should be populated"
echo "✅ NewsletterEmailHistory record should exist"
echo "✅ NewsletterEmailHistory.total_recipients > 0"
echo "✅ NewsletterEmailHistory.successful_sends > 0"
echo "✅ Dashboard should show recipient counts"
echo ""
echo "=== If Fix Failed ==="
echo "❌ Newsletter.sent_at is NULL"
echo "❌ No NewsletterEmailHistory record"
echo "❌ Dashboard shows 'No newsletters sent yet'"
echo "❌ Logs show DbUpdateConcurrencyException or commit failure"
