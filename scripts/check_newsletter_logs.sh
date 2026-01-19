#!/bin/bash
# Deep Dive Log Analysis for Newsletter d57233a0-dd5c-4e50-bc26-7aef255d539f
# Senior Engineer Approach: Check ACTUAL execution, not hypotheses

NEWSLETTER_ID="d57233a0-dd5c-4e50-bc26-7aef255d539f"
CONTAINER_NAME="lankaconnect-api-staging"
RESOURCE_GROUP="lankaconnect-staging"

echo "============================================================"
echo "SENIOR ENGINEER DEEP DIVE - Newsletter Log Analysis"
echo "Newsletter ID: $NEWSLETTER_ID"
echo "============================================================"
echo ""

echo "📋 STEP 1: Check if job started for this newsletter"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep -E "$NEWSLETTER_ID|NewsletterEmailJob" \
  | grep "STARTED"

echo ""
echo "📊 STEP 2: Check recipient resolution (lines 103-110)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -E "Resolved.*recipients|EmailGroupCount|MetroCount"

echo ""
echo "🚨 STEP 3: Check if 0 recipients triggered early exit (line 114)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep "No recipients found"

echo ""
echo "✉️ STEP 4: Check if emails were sent (lines 152-194)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -E "Sending newsletter email|emails sent successfully|failed to send"

echo ""
echo "🔄 STEP 5: Check if newsletter was reloaded (line 209)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -E "Reloading newsletter|not found when reloading"

echo ""
echo "⚠️ STEP 6: Check MarkAsSent() call and result (line 228-236)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -E "Failed to mark newsletter.*as sent|MarkAsSent"

echo ""
echo "💾 STEP 7: Check database commit attempt (line 275-278)"
echo "─────────────────────────────────────────────────────────"
echo "THIS IS THE CRITICAL TRIGGER POINT!"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -E "Attempting to commit newsletter|marked as sent at"

echo ""
echo "🔍 STEP 8: Check EF Core diagnostics (DIAG logs)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep -E "\[DIAG-" \
  | grep -A 5 -B 5 "$NEWSLETTER_ID"

echo ""
echo "⚡ STEP 9: Check if concurrency exception occurred (line 284-299)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -E "CONCURRENCY EXCEPTION|DbUpdateConcurrencyException"

echo ""
echo "✅ STEP 10: Check job completion (line 320)"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep "NewsletterEmailJob COMPLETED"

echo ""
echo "❌ STEP 11: Check for ANY errors or exceptions"
echo "─────────────────────────────────────────────────────────"
az containerapp logs show \
  --name $CONTAINER_NAME \
  --resource-group $RESOURCE_GROUP \
  --type console \
  --tail 300 \
  --follow false \
  | grep "$NEWSLETTER_ID" \
  | grep -iE "error|exception|failed|critical"

echo ""
echo "============================================================"
echo "ANALYSIS COMPLETE"
echo "============================================================"
echo ""
echo "KEY QUESTIONS TO ANSWER:"
echo "1. Was line 278 (CommitAsync) executed? → Look for 'Attempting to commit'"
echo "2. Did CommitAsync succeed? → Look for 'marked as sent at'"
echo "3. Were DIAG logs present? → Look for [DIAG-11], [DIAG-13], [DIAG-16]"
echo "4. Any exceptions? → Look for error messages"
echo ""
echo "If line 278 was NOT executed → Find which early exit path was taken"
echo "If line 278 WAS executed but no DIAG logs → UnitOfWork not calling AppDbContext.CommitAsync"
echo "If DIAG logs show 0 entities → Entity tracking issue confirmed"
