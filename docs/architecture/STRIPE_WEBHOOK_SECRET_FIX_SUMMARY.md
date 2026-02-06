# Stripe Webhook Secret Configuration Fix - Executive Summary

**Date:** 2025-12-16
**Issue:** HTTP 400 "Invalid signature" on all Stripe webhook deliveries
**Status:** Root cause identified, fix ready for implementation

---

## Problem Summary

Azure Container Apps webhook endpoint at `/api/payments/webhook` consistently returns HTTP 400 "Invalid signature" despite:
- Correct secret stored in Key Vault (`whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX`)
- Multiple container restarts
- Verified Stripe webhook signing secret matches
- System-assigned identity with Key Vault access configured

---

## Root Cause

**The webhook secret was never loaded into the application.**

Configuration binding chain was incomplete:
1. ❌ `appsettings.Staging.json` does NOT define `Stripe.WebhookSecret` property
2. ❌ Deployment workflow does NOT set `Stripe__WebhookSecret` environment variable
3. ❌ `StripeOptions.WebhookSecret` remains empty string (default value)
4. ❌ `EventUtility.ConstructEvent()` receives empty secret → signature verification fails

**Key Insight:** Even though Key Vault contains the correct secret, ASP.NET Core configuration system never binds it to `StripeOptions` because:
- The property must be declared in `appsettings.json` schema
- The environment variable must be set in deployment workflow
- Both were missing

---

## Solution

### Required Changes (3 files)

#### 1. appsettings.Staging.json
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",
  "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"  // ← ADD THIS LINE
}
```

#### 2. .github/workflows/deploy-staging.yml
```yaml
# Around line 155, add this to --replace-env-vars:
Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

#### 3. Verify Key Vault Secret
```bash
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv

# Expected: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX
```

---

## Why This Fixes The Issue

**Before Fix:**
```
Container App: Stripe__WebhookSecret not set
    ↓
appsettings.json: No WebhookSecret property defined
    ↓
Configuration Binding: Skips WebhookSecret (not in schema)
    ↓
StripeOptions.WebhookSecret: "" (empty)
    ↓
EventUtility.ConstructEvent("", signature): FAIL ❌
```

**After Fix:**
```
Container App: Stripe__WebhookSecret=secretref:stripe-webhook-secret
    ↓
Azure resolves secretref from Key Vault: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX
    ↓
appsettings.json: WebhookSecret property exists
    ↓
Configuration Binding: Populates StripeOptions.WebhookSecret
    ↓
EventUtility.ConstructEvent("whsec_...", signature): SUCCESS ✅
```

---

## Implementation Steps

### Step 1: Update Configuration Files
```bash
# 1. Edit appsettings.Staging.json (add WebhookSecret line)
# Location: src/LankaConnect.API/appsettings.Staging.json

# 2. Edit deploy-staging.yml (add Stripe__WebhookSecret env var)
# Location: .github/workflows/deploy-staging.yml
```

### Step 2: Commit and Deploy
```bash
git add src/LankaConnect.API/appsettings.Staging.json
git add .github/workflows/deploy-staging.yml
git commit -m "fix(phase-6a24): Add Stripe webhook secret configuration binding"
git push origin develop
```

### Step 3: Verify Deployment
```bash
# Wait for GitHub Actions deployment to complete (~5 minutes)

# Check environment variables are set correctly
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' -o json \
  | grep -A2 "Stripe__WebhookSecret"

# Expected output:
# {
#   "name": "Stripe__WebhookSecret",
#   "secretRef": "stripe-webhook-secret"
# }
```

### Step 4: Test Webhook
```bash
# Install Stripe CLI if not already installed
# https://stripe.com/docs/stripe-cli

# Listen and forward events
stripe listen --forward-to https://lankaconnect-api-staging.azurewebsites.net/api/payments/webhook

# In another terminal, trigger test event
stripe trigger checkout.session.completed

# Expected: HTTP 200 OK (not 400)
```

### Step 5: Verify Logs
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 \
  --filter "Processing webhook event"

# Expected log: "Processing webhook event evt_... of type checkout.session.completed"
```

---

## Verification Checklist

- [ ] `appsettings.Staging.json` contains `"WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"`
- [ ] `deploy-staging.yml` contains `Stripe__WebhookSecret=secretref:stripe-webhook-secret`
- [ ] GitHub Actions deployment succeeds
- [ ] Container App environment shows `Stripe__WebhookSecret` variable
- [ ] Stripe CLI test returns HTTP 200 OK
- [ ] Application logs show "Processing webhook event"
- [ ] Stripe Dashboard shows webhook delivery succeeded

---

## Why Previous Attempts Failed

### Container Restarts
**Why it didn't work:** The environment variable was never set in the deployment workflow, so restarting just reloaded the same incomplete configuration.

### Direct Secret Value (Not Key Vault Reference)
**Why it didn't work:** Even with a direct value, `appsettings.json` didn't define the property, so configuration binding ignored it.

### Versioned Key Vault URL
**Why it didn't work:** The problem wasn't Key Vault access; it was configuration schema definition.

---

## Key Learnings

### ASP.NET Core Configuration Binding Requires:
1. **Schema Declaration**: Property must exist in `appsettings.json`
2. **Environment Variable**: Must be set at runtime (via deployment workflow)
3. **Naming Convention**: Use double-underscore syntax (`Section__Property`)
4. **Key Vault Integration**: Works via `secretref:secret-name` in Azure Container Apps

### Configuration Hierarchy:
```
1. appsettings.json         (schema + defaults)
2. appsettings.{Env}.json   (environment overrides)
3. Environment Variables     (runtime values)
4. Key Vault Secrets        (via secretref resolution)
```

**All 4 layers must be configured correctly for secrets to work.**

---

## Future Prevention

### When Adding New Secrets:
1. ✅ Store secret in Azure Key Vault
2. ✅ Define property in `appsettings.json` with placeholder
3. ✅ Set environment variable in deployment workflow with `secretref:`
4. ✅ Verify system-assigned identity has Key Vault access
5. ✅ Test end-to-end after deployment

### Secret Rotation Process:
```bash
# 1. Update Key Vault
az keyvault secret set --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret --value "new_secret"

# 2. Restart container (picks up new value automatically)
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging

# 3. Verify
stripe trigger checkout.session.completed
```

---

## Architectural Impact

**Before:** Webhook configuration was incomplete, blocking post-payment workflows.

**After:** Complete configuration chain enables:
- Webhook signature verification
- Event idempotency tracking
- Post-payment processing (signup confirmation)
- Audit trail of payment events

**Risk Level:** Low - Additive change only, no breaking modifications.

**Deployment Time:** 15 minutes (including GitHub Actions build/deploy).

---

## Related Documents

- [ADR-007: Stripe Webhook Secret Configuration](./ADR-007-Stripe-Webhook-Secret-Azure-Container-Apps-Key-Vault.md) - Detailed architectural analysis
- [ADR-005: Stripe API Key Configuration](./ADR-005-Stripe-API-Key-Configuration-Azure-Container-Apps.md) - Related configuration issue
- [STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md](./STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md) - Phase 6A.24 implementation
- [PHASE_6A_MASTER_INDEX.md](../PHASE_6A_MASTER_INDEX.md) - Phase tracking

---

## Next Steps

1. **Immediate:** Apply configuration fixes to `appsettings.Staging.json` and `deploy-staging.yml`
2. **Deploy:** Push to develop branch, wait for GitHub Actions
3. **Verify:** Test with Stripe CLI and check logs
4. **Document:** Update Phase 6A.24 status to Complete
5. **Production:** Apply same fix to production configuration
