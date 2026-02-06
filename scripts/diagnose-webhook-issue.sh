#!/bin/bash
# Diagnostic script to identify payment webhook root cause
# Run this to determine if webhooks are reaching the server

echo "=================================="
echo "Payment Webhook Diagnostic Tool"
echo "=================================="
echo ""

# Check 1: Look for webhook requests in Azure logs
echo "Check 1: Searching Azure logs for webhook requests..."
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --tail 1000 \
  --output table \
  | grep -i -E "webhook|stripe-signature|checkout.session.completed" || echo "❌ NO webhook logs found"

echo ""
echo "Check 2: Searching for payment completion logs..."
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --tail 1000 \
  --output table \
  | grep -i "completing payment" || echo "❌ NO payment completion logs found"

echo ""
echo "Check 3: Searching for domain event dispatching logs..."
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --tail 1000 \
  --output table \
  | grep -i -E "phase 6a.24|domain event|PaymentCompletedEvent" || echo "❌ NO domain event logs found"

echo ""
echo "Check 4: Searching for PaymentCompletedEventHandler logs..."
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --tail 1000 \
  --output table \
  | grep -i "PaymentCompletedEventHandler INVOKED" || echo "❌ NO handler invocation logs found"

echo ""
echo "Check 5: Looking for ANY errors in last 100 log entries..."
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --tail 100 \
  --output table \
  | grep -i -E "error|exception|failed"

echo ""
echo "=================================="
echo "Diagnosis Complete"
echo "=================================="
echo ""
echo "Next steps:"
echo "1. If NO webhook logs found → Check Stripe Dashboard webhook configuration"
echo "2. If webhook logs exist but no payment completion → Check metadata/routing"
echo "3. If payment completion logs exist but no domain events → Check EF Core tracking"
echo "4. If domain events logged but no handler → Check MediatR registration"
echo ""
echo "Full analysis: docs/architecture/PAYMENT_WEBHOOK_ROOT_CAUSE_ANALYSIS.md"
