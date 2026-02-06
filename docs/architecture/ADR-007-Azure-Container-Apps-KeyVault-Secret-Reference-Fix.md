# ADR-007: Azure Container Apps Key Vault Secret Reference Configuration Fix

**Status**: Resolved
**Date**: 2025-12-16
**Context**: Phase 6A.24 Stripe Webhook Configuration
**Decision Makers**: System Architecture Team

---

## Executive Summary

**Problem**: Stripe webhook endpoint returning HTTP 400 "Invalid signature" despite correct webhook secret stored in Azure Key Vault.

**Root Cause**: Container App environment variable `Stripe__WebhookSecret` references secret `stripe-webhook-secret-direct` (which has `keyVaultUrl: null`), but the actual Key Vault secret is named `stripe-webhook-secret`.

**Impact**: All Stripe webhook deliveries fail signature verification, preventing automated ticket generation and payment processing.

**Solution**: Fix secret reference name mismatch and implement proper Key Vault secret configuration pattern.

---

## 1. Root Cause Analysis

### 1.1 Configuration Chain Investigation

```
Key Vault Secret Name: stripe-webhook-secret
   ↓ (SHOULD REFERENCE)
Container App Secret: stripe-webhook-secret-direct  ← MISMATCH!
   ↓ (REFERENCES)
Environment Variable: Stripe__WebhookSecret
   ↓ (BINDS TO)
StripeOptions.WebhookSecret
   ↓ (USED BY)
PaymentsController.Webhook() signature verification
```

### 1.2 Evidence

**Key Vault Configuration** (CORRECT):
```json
{
  "name": "stripe-webhook-secret",
  "value": "whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX",
  "id": "https://lankaconnect-staging-kv.vault.azure.net/secrets/stripe-webhook-secret/45a01fae512945878eba030505c7e0be",
  "enabled": true,
  "updated": "2025-12-16T15:06:56+00:00"
}
```

**Container App Secrets** (MISMATCH DETECTED):
```json
[
  {
    "name": "stripe-webhook-secret",
    "keyVaultUrl": "https://lankaconnect-staging-kv.vault.azure.net/secrets/stripe-webhook-secret/45a01fae512945878eba030505c7e0be"
  },
  {
    "name": "stripe-webhook-secret-direct",
    "keyVaultUrl": null  ← PROBLEM: No Key Vault reference!
  }
]
```

**Container App Environment Variable** (WRONG REFERENCE):
```json
{
  "name": "Stripe__WebhookSecret",
  "secretRef": "stripe-webhook-secret-direct"  ← References secret with null Key Vault URL
}
```

### 1.3 Why Restarts and Manual Updates Failed

1. **Secret Reference Name**: The environment variable points to `stripe-webhook-secret-direct`, which has no Key Vault URL
2. **Direct Secret Value**: The `stripe-webhook-secret-direct` likely contains an old hardcoded value or is empty
3. **Key Vault Changes Ignored**: Updates to `stripe-webhook-secret` in Key Vault are never read because the environment variable references the wrong secret
4. **Container Restarts Ineffective**: Restarting loads the same wrong reference

---

## 2. The Problem: Two Secrets Exist in Container App

### 2.1 Current State
```bash
# Container App has TWO webhook secret references:
1. stripe-webhook-secret          → Correct Key Vault URL (GOOD)
2. stripe-webhook-secret-direct   → No Key Vault URL (BAD)

# Environment variable uses the WRONG one:
Stripe__WebhookSecret → secretRef: stripe-webhook-secret-direct
```

### 2.2 How This Happened
Likely sequence of troubleshooting attempts:
1. Original configuration: `stripe-webhook-secret` with Key Vault reference
2. When signature validation failed, attempted to create direct secret value
3. Created new secret `stripe-webhook-secret-direct` with hardcoded value
4. Updated environment variable to use `-direct` variant
5. However, the direct value was still incorrect or got cached

---

## 3. Solution: Proper Key Vault Secret Reference Pattern

### 3.1 Immediate Fix

**Step 1: Remove the incorrect direct secret**
```bash
az containerapp secret remove \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --secret-names stripe-webhook-secret-direct
```

**Step 2: Update environment variable to use correct Key Vault reference**
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Step 3: Force new revision deployment**
```bash
az containerapp revision copy \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision-suffix webhook-fix-$(date +%s)
```

**Step 4: Verify the fix**
```bash
# Check environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']" \
  -o json

# Should show:
# {
#   "name": "Stripe__WebhookSecret",
#   "secretRef": "stripe-webhook-secret"
# }
```

### 3.2 Verification Strategy

**A. Verify Key Vault Secret Value**
```bash
# Get current webhook secret from Key Vault
WEBHOOK_SECRET=$(az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv)

echo "Key Vault webhook secret: $WEBHOOK_SECRET"
# Expected: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX
```

**B. Verify Stripe Dashboard Configuration**
```bash
# Log into Stripe Dashboard
# Navigate to: Developers → Webhooks → [Your webhook endpoint]
# Click "Signing secret" → "Reveal"
# Compare with Key Vault value - MUST MATCH EXACTLY
```

**C. Test Webhook Delivery**
```bash
# In Stripe Dashboard, send test webhook event
# Monitor Container App logs:
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow \
  --tail 50
```

**D. Create Diagnostic Endpoint (Optional but Recommended)**

Add temporary diagnostic endpoint to PaymentsController.cs:
```csharp
#if DEBUG
[HttpGet("webhook/config")]
[Authorize(Policy = "RequireAdministratorRole")]
public IActionResult GetWebhookConfig()
{
    return Ok(new
    {
        SecretConfigured = !string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret),
        SecretPrefix = _stripeOptions.WebhookSecret?.Substring(0, Math.Min(10, _stripeOptions.WebhookSecret.Length ?? 0)),
        SecretLength = _stripeOptions.WebhookSecret?.Length ?? 0,
        ExpectedPrefix = "whsec_",
        IsValid = _stripeOptions.WebhookSecret?.StartsWith("whsec_") ?? false
    });
}
#endif
```

Call endpoint after deployment:
```bash
# Get admin access token
TOKEN="your-admin-jwt-token"

# Check webhook configuration
curl -H "Authorization: Bearer $TOKEN" \
  https://lankaconnect-api-staging.azurecontainerapps.io/api/payments/webhook/config

# Expected response:
# {
#   "secretConfigured": true,
#   "secretPrefix": "whsec_vjs9",
#   "secretLength": 41,
#   "expectedPrefix": "whsec_",
#   "isValid": true
# }
```

---

## 4. Architectural Best Practices

### 4.1 Azure Container Apps + Key Vault Pattern

**Correct Configuration Flow**:
```
1. Store secret in Azure Key Vault
   ↓
2. Grant Container App system-assigned identity "Key Vault Secrets User" role
   ↓
3. Reference Key Vault secret in Container App (creates managed secret)
   ↓
4. Environment variable uses secretRef to managed secret
   ↓
5. ASP.NET Core configuration binds to application options
```

**DO**: Use Key Vault references for secrets
```bash
az containerapp update \
  --name myapp \
  --resource-group myrg \
  --set-env-vars MySecret=secretref:my-keyvault-secret
```

**DON'T**: Mix direct secret values with Key Vault references
```bash
# This creates confusion and cache issues
az containerapp secret set --name myapp --secrets my-secret="hardcoded-value"
az containerapp update --set-env-vars MySecret=secretref:my-secret
```

### 4.2 Secret Naming Convention

**Key Vault Secret Name**: `stripe-webhook-secret` (kebab-case)
**Container App Secret Name**: `stripe-webhook-secret` (SAME as Key Vault)
**Environment Variable Name**: `Stripe__WebhookSecret` (ASP.NET Core hierarchical config)
**Options Property Name**: `StripeOptions.WebhookSecret` (PascalCase)

### 4.3 Key Vault Secret Rotation Strategy

When rotating webhook secrets:

**Step 1: Create new webhook endpoint in Stripe**
```bash
# Create new webhook with new signing secret
# Store in Key Vault with versioning
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --value "whsec_NEW_SECRET_HERE" \
  --description "Rotated on $(date -u +%Y-%m-%dT%H:%M:%SZ)"
```

**Step 2: Wait for Container Apps to refresh**
```bash
# Container Apps refresh Key Vault references every ~30 minutes
# Force immediate refresh by creating new revision:
az containerapp revision copy \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision-suffix secret-rotation-$(date +%s)
```

**Step 3: Verify new secret is loaded**
```bash
# Test webhook with new secret
# Monitor logs for successful signature verification
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 20
```

**Step 4: Deactivate old webhook endpoint in Stripe**
```bash
# In Stripe Dashboard, disable old webhook endpoint
# Keep for 7 days for rollback capability
```

---

## 5. GitHub Actions Deployment Fix

### 5.1 Current Deployment Workflow Issue

The `deploy-staging.yml` workflow does NOT configure `Stripe__WebhookSecret`:

```yaml
# Line 154-155: Missing webhook secret configuration
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key
# ← Stripe__WebhookSecret is MISSING!
```

### 5.2 Required Fix to deploy-staging.yml

Update line 155 to include webhook secret:
```yaml
- name: Update Container App
  run: |
    az containerapp update \
      --name ${{ env.CONTAINER_APP_NAME }} \
      --resource-group ${{ env.RESOURCE_GROUP }} \
      --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
      --replace-env-vars \
        ASPNETCORE_ENVIRONMENT=Staging \
        ConnectionStrings__DefaultConnection=secretref:database-connection-string \
        ConnectionStrings__Redis=localhost:6379 \
        Jwt__Key=secretref:jwt-secret-key \
        Jwt__Issuer=secretref:jwt-issuer \
        Jwt__Audience=secretref:jwt-audience \
        EntraExternalId__IsEnabled=secretref:entra-enabled \
        EntraExternalId__TenantId=secretref:entra-tenant-id \
        EntraExternalId__ClientId=secretref:entra-client-id \
        EntraExternalId__Audience=secretref:entra-audience \
        EmailSettings__Provider=Smtp \
        EmailSettings__SmtpServer=secretref:smtp-host \
        EmailSettings__SmtpPort=secretref:smtp-port \
        EmailSettings__Username=secretref:smtp-username \
        EmailSettings__Password=secretref:smtp-password \
        EmailSettings__SenderEmail=secretref:email-from-address \
        EmailSettings__SenderName="LankaConnect Staging" \
        EmailSettings__EnableSsl=true \
        EmailSettings__TemplateBasePath=Templates/Email \
        AzureStorage__ConnectionString=secretref:azure-storage-connection-string \
        Stripe__SecretKey=secretref:stripe-secret-key \
        Stripe__PublishableKey=secretref:stripe-publishable-key \
        Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Why `--replace-env-vars` is important**: This flag ensures that on every deployment, the environment variables are reset to the exact configuration specified, removing any manual changes or incorrect references.

---

## 6. Prevention Strategy

### 6.1 Configuration Validation Checklist

Before deploying webhook endpoints:
- [ ] Webhook secret exists in Azure Key Vault
- [ ] Key Vault secret name matches Container App secret reference
- [ ] Container App system-assigned identity has Key Vault access
- [ ] Environment variable uses `secretref:` pattern
- [ ] GitHub Actions workflow includes webhook secret configuration
- [ ] No duplicate secrets with similar names exist
- [ ] Deployment uses `--replace-env-vars` flag

### 6.2 Monitoring and Alerting

**Application Insights Alert**: Webhook signature validation failures
```kusto
traces
| where message contains "Stripe webhook signature verification failed"
| summarize count() by bin(timestamp, 5m)
| where count_ > 3
```

**Container App Metrics**: Track webhook endpoint success rate
```bash
# Monitor HTTP 400 responses on /api/payments/webhook
az monitor metrics list \
  --resource "/subscriptions/{sub}/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging" \
  --metric "Requests" \
  --filter "StatusCode eq '400' and Path eq '/api/payments/webhook'"
```

### 6.3 Documentation Requirements

For each external webhook integration:
1. Document webhook endpoint URL
2. Document Key Vault secret name
3. Document Container App secret reference name
4. Document environment variable name
5. Document rotation procedure
6. Store webhook provider dashboard credentials in secure location

---

## 7. Testing Plan

### 7.1 Manual Testing

**Test 1: Verify Secret Loading**
```bash
# Check Container App configuration
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']"

# Expected: secretRef points to stripe-webhook-secret (not -direct)
```

**Test 2: Trigger Webhook Delivery**
```bash
# In Stripe Dashboard:
# 1. Go to Developers → Webhooks
# 2. Select webhook endpoint
# 3. Click "Send test webhook"
# 4. Select "checkout.session.completed" event
# 5. Check response - should be HTTP 200 OK
```

**Test 3: End-to-End Ticket Purchase**
```bash
# 1. Create test event with paid tickets
# 2. Complete checkout session
# 3. Verify webhook processed successfully
# 4. Verify ticket record created in database
# 5. Verify email sent with QR code
```

### 7.2 Automated Testing

**Integration Test: Webhook Signature Verification**
```csharp
[Fact]
public async Task Webhook_ValidSignature_ProcessesSuccessfully()
{
    // Arrange
    var webhookSecret = "whsec_test_secret";
    var payload = "{\"id\":\"evt_test\",\"type\":\"checkout.session.completed\"}";
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var signature = GenerateStripeSignature(payload, timestamp, webhookSecret);

    // Act
    var response = await _client.PostAsync("/api/payments/webhook",
        new StringContent(payload, Encoding.UTF8, "application/json"),
        headers: new { ["Stripe-Signature"] = signature });

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task Webhook_InvalidSignature_Returns400()
{
    // Arrange
    var payload = "{\"id\":\"evt_test\",\"type\":\"checkout.session.completed\"}";
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var invalidSignature = $"t={timestamp},v1=invalid_signature";

    // Act
    var response = await _client.PostAsync("/api/payments/webhook",
        new StringContent(payload, Encoding.UTF8, "application/json"),
        headers: new { ["Stripe-Signature"] = invalidSignature });

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

---

## 8. Rollback Procedure

If webhook processing fails after fix:

**Step 1: Check current configuration**
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']"
```

**Step 2: Verify Key Vault secret value**
```bash
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value
```

**Step 3: Roll back to previous revision**
```bash
# List recent revisions
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "[0:5].{name:name, active:properties.active, created:properties.createdTime}"

# Activate previous working revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision [PREVIOUS_REVISION_NAME]
```

**Step 4: Investigate logs**
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 \
  --follow
```

---

## 9. Success Criteria

The fix is successful when:
- [ ] Container App environment variable `Stripe__WebhookSecret` references `stripe-webhook-secret` (not `-direct`)
- [ ] Secret `stripe-webhook-secret-direct` is removed from Container App
- [ ] Test webhook delivery from Stripe returns HTTP 200 OK
- [ ] Container App logs show "Processing webhook event" messages
- [ ] No "Invalid signature" errors in logs
- [ ] Ticket purchase workflow generates QR code and sends email
- [ ] GitHub Actions deployment includes webhook secret configuration

---

## 10. Related Documentation

- [Phase 6A.24 Webhook Configuration](../PHASE_6A_24_REGISTRATION_EMAIL_TICKET_PLAN.md)
- [Stripe Webhook Documentation](https://stripe.com/docs/webhooks)
- [Azure Container Apps Secrets](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets?tabs=azure-cli#azure-key-vault-secrets)

---

## Appendix A: Complete Fix Script

```bash
#!/bin/bash
set -e

CONTAINER_APP="lankaconnect-api-staging"
RESOURCE_GROUP="lankaconnect-staging"
KEY_VAULT="lankaconnect-staging-kv"

echo "=== Azure Container Apps Key Vault Secret Reference Fix ==="
echo ""

# Step 1: Verify Key Vault secret exists
echo "Step 1: Verifying Key Vault secret..."
WEBHOOK_SECRET=$(az keyvault secret show \
  --vault-name "$KEY_VAULT" \
  --name stripe-webhook-secret \
  --query value -o tsv)

if [ -z "$WEBHOOK_SECRET" ]; then
  echo "ERROR: stripe-webhook-secret not found in Key Vault"
  exit 1
fi

echo "✓ Key Vault secret exists: ${WEBHOOK_SECRET:0:10}... (${#WEBHOOK_SECRET} characters)"
echo ""

# Step 2: Remove incorrect direct secret
echo "Step 2: Removing incorrect direct secret..."
az containerapp secret remove \
  --name "$CONTAINER_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --secret-names stripe-webhook-secret-direct \
  2>/dev/null || echo "Secret not found (already removed)"

echo "✓ Removed stripe-webhook-secret-direct"
echo ""

# Step 3: Update environment variable
echo "Step 3: Updating environment variable..."
az containerapp update \
  --name "$CONTAINER_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --set-env-vars Stripe__WebhookSecret=secretref:stripe-webhook-secret

echo "✓ Environment variable updated"
echo ""

# Step 4: Create new revision
echo "Step 4: Creating new revision..."
TIMESTAMP=$(date +%s)
az containerapp revision copy \
  --name "$CONTAINER_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --revision-suffix "webhook-fix-$TIMESTAMP"

echo "✓ New revision created"
echo ""

# Step 5: Verify configuration
echo "Step 5: Verifying configuration..."
ENV_CONFIG=$(az containerapp show \
  --name "$CONTAINER_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']" \
  -o json)

echo "Environment variable configuration:"
echo "$ENV_CONFIG" | jq '.'

SECRET_REF=$(echo "$ENV_CONFIG" | jq -r '.[0].secretRef')
if [ "$SECRET_REF" = "stripe-webhook-secret" ]; then
  echo "✓ Configuration correct!"
else
  echo "✗ Configuration incorrect: secretRef = $SECRET_REF"
  exit 1
fi

echo ""
echo "=== Fix Complete ==="
echo ""
echo "Next steps:"
echo "1. Test webhook delivery from Stripe Dashboard"
echo "2. Monitor Container App logs: az containerapp logs show --name $CONTAINER_APP --resource-group $RESOURCE_GROUP --follow"
echo "3. Verify ticket generation workflow end-to-end"
```

Save as `scripts/fix-stripe-webhook-keyvault.sh` and execute:
```bash
chmod +x scripts/fix-stripe-webhook-keyvault.sh
./scripts/fix-stripe-webhook-keyvault.sh
```

---

**Document Version**: 1.0
**Last Updated**: 2025-12-16
**Next Review**: After successful webhook deployment