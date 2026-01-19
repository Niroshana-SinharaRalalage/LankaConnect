#!/bin/bash
# RCA Log Capture Script for Newsletter Database Update Issue
# Run this IMMEDIATELY after creating and sending a test newsletter

echo "============================================================"
echo "Newsletter RCA Log Capture Script"
echo "============================================================"
echo ""

# Check if newsletter ID is provided
if [ -z "$1" ]; then
    echo "❌ ERROR: Newsletter ID required!"
    echo ""
    echo "Usage:"
    echo "  bash scripts/capture_newsletter_rca_logs.sh <newsletter-id>"
    echo ""
    echo "Example:"
    echo "  bash scripts/capture_newsletter_rca_logs.sh a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    echo ""
    exit 1
fi

NEWSLETTER_ID="$1"
CONTAINER_NAME="lankaconnect-api-staging"
RESOURCE_GROUP="lankaconnect-staging"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="newsletter_rca_logs_${NEWSLETTER_ID:0:8}_${TIMESTAMP}.txt"

echo "📋 Configuration:"
echo "  Newsletter ID: $NEWSLETTER_ID"
echo "  Container: $CONTAINER_NAME"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Output File: $LOG_FILE"
echo ""
echo "⏳ Capturing logs (this will run for 60 seconds to catch async job completion)..."
echo "   Press Ctrl+C if job completes earlier"
echo ""
echo "============================================================"
echo ""

# Capture logs with follow for 60 seconds, then stop
timeout 60 az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow true 2>&1 \
  | grep -E "$NEWSLETTER_ID|DIAG-|Phase 6A.74|UnitOfWork.CommitAsync" \
  | tee "$LOG_FILE"

EXIT_CODE=$?

echo ""
echo "============================================================"
echo "Log Capture Complete"
echo "============================================================"
echo ""

if [ $EXIT_CODE -eq 124 ]; then
    echo "✅ Captured 60 seconds of logs"
elif [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Log capture completed (ended early with Ctrl+C or stream closed)"
else
    echo "⚠️  Log capture exited with code: $EXIT_CODE"
fi

echo ""
echo "📄 Log file saved: $LOG_FILE"
echo ""

# Quick analysis
LINE_COUNT=$(wc -l < "$LOG_FILE")
echo "📊 Quick Analysis:"
echo "  Total log lines captured: $LINE_COUNT"
echo ""

if [ $LINE_COUNT -eq 0 ]; then
    echo "❌ WARNING: No matching logs found!"
    echo "   Possible reasons:"
    echo "   - Newsletter job hasn't started yet (wait 5-10 seconds and try again)"
    echo "   - Newsletter ID is incorrect"
    echo "   - Deployment hasn't picked up the new logging code"
    echo ""
    exit 1
fi

# Check for critical log markers
echo "🔍 Checking for critical log markers..."
echo ""

if grep -q "NewsletterEmailJob STARTED" "$LOG_FILE"; then
    echo "✅ Job started"
else
    echo "❌ Job start log NOT found"
fi

if grep -q "Resolved.*recipients" "$LOG_FILE"; then
    echo "✅ Recipient resolution completed"
    grep "Resolved.*recipients" "$LOG_FILE" | tail -1
else
    echo "❌ Recipient resolution log NOT found"
fi

if grep -q "Sent.*emails successfully" "$LOG_FILE"; then
    echo "✅ Emails sent"
    grep "Sent.*emails successfully" "$LOG_FILE" | tail -1
else
    echo "⚠️  Email sending log NOT found (might still be in progress)"
fi

if grep -q "UnitOfWork.CommitAsync called" "$LOG_FILE"; then
    echo "✅ CommitAsync was called - this is the DATABASE SAVE trigger!"
else
    echo "❌ CommitAsync NOT called - job exited early before database save"
fi

if grep -q "\[DIAG-" "$LOG_FILE"; then
    echo "✅ DIAG logs present - AppDbContext.CommitAsync executed"
    DIAG_COUNT=$(grep -c "\[DIAG-" "$LOG_FILE")
    echo "   Found $DIAG_COUNT DIAG log entries"
else
    echo "❌ No DIAG logs - AppDbContext.CommitAsync might not have been reached"
fi

if grep -q "Entity BEFORE DetectChanges.*Newsletter" "$LOG_FILE"; then
    echo "✅ Newsletter entity IS tracked by EF Core"
else
    echo "❌ Newsletter entity NOT tracked - ENTITY DETACHED ISSUE CONFIRMED"
fi

if grep -q "Entity BEFORE DetectChanges.*NewsletterEmailHistory" "$LOG_FILE"; then
    echo "✅ NewsletterEmailHistory entity IS tracked by EF Core"
else
    echo "❌ NewsletterEmailHistory entity NOT tracked"
fi

if grep -q "NewsletterEmailJob COMPLETED" "$LOG_FILE"; then
    echo "✅ Job completed"
else
    echo "⚠️  Job completion log NOT found (might still be running)"
fi

if grep -iq "error\|exception\|failed\|critical" "$LOG_FILE"; then
    echo "⚠️  Errors/exceptions found in logs:"
    grep -i "error\|exception\|failed\|critical" "$LOG_FILE" | head -5
fi

echo ""
echo "============================================================"
echo "Next Steps:"
echo "============================================================"
echo ""
echo "1. Review the full log file: $LOG_FILE"
echo "2. Share the log file for detailed analysis"
echo "3. Check database state with:"
echo "   psql -c \"SELECT id, status, sent_at FROM communications.newsletters WHERE id = '$NEWSLETTER_ID';\""
echo "4. Check if history was created:"
echo "   psql -c \"SELECT * FROM communications.newsletter_email_history WHERE newsletter_id = '$NEWSLETTER_ID';\""
echo ""