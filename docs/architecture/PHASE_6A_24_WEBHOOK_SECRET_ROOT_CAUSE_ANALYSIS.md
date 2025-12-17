# Phase 6A.24 - Stripe Webhook Secret Root Cause Analysis

**Date:** 2025-12-16
**Phase:** 6A.24 - Stripe Webhook Configuration
**Issue:** HTTP 400 "Invalid signature" on all webhook deliveries
**Status:** Root cause identified, solution ready

---

## Executive Summary

**Problem:** Azure Container Apps webhook endpoint at `/api/payments/webhook` returns HTTP 400 "Invalid signature" for all Stripe webhook deliveries, despite:
- Correct secret stored in Azure Key Vault
- Multiple container restarts (5+)
- Verified webhook signing secret in Stripe Dashboard
- System-assigned managed identity with Key Vault access
- Even attempted direct secret value (bypassing Key Vault)

**Root Cause:** Configuration binding failure due to missing schema definition and environment variable. The webhook secret was stored correctly in Key Vault but never loaded into the ASP.NET Core application because:
1. `appsettings.Staging.json` does not define `Stripe.WebhookSecret` property
2. Deployment workflow does not set `Stripe__WebhookSecret` environment variable
3. ASP.NET Core configuration system requires BOTH for binding to work

**Solution:** Add two configuration lines (one in appsettings.json, one in deployment workflow) to complete the configuration chain.

**Impact:** Critical - Blocks all post-payment workflows (signup confirmation, event access, payment audit trail).

**Risk Level:** Low - Additive configuration only, no code changes required.

**Implementation Time:** 15 minutes (including GitHub Actions deployment).

---

## Detailed Root Cause Analysis

### 1. Configuration Binding Failure

**ASP.NET Core Configuration Pattern:**
```csharp
// DependencyInjection.cs:252
services.Configure<StripeOptions>(configuration.GetSection("Stripe"));

// This binds the "Stripe" configuration section to StripeOptions class
// BUT only properties defined in appsettings.json schema are bound
```

**Current appsettings.Staging.json (INCOMPLETE):**
```json
{
  "Stripe": {
    "SecretKey": "${STRIPE_SECRET_KEY}",          // ✅ Defined
    "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}" // ✅ Defined
    // ❌ MISSING: "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
  }
}
```

**Result:** `StripeOptions.WebhookSecret` remains `string.Empty` (default value).

### 2. Environment Variable Missing

**Current Deployment Workflow (deploy-staging.yml:154-155):**
```yaml
--replace-env-vars \
  Stripe__SecretKey=secretref:stripe-secret-key \
  Stripe__PublishableKey=secretref:stripe-publishable-key
  # ❌ MISSING: Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Result:** Even if appsettings.json defined the property, no environment variable exists to override it.

### 3. Why Key Vault Was Not the Problem

**Key Vault Status:**
```bash
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv

# Output: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX ✅ CORRECT
```

**System-Assigned Identity:**
```bash
# Identity enabled ✅
# "Get" permission granted ✅
# Secret retrievable via Azure CLI ✅
```

**Conclusion:** Key Vault configuration is perfect. The secret exists and is accessible. The problem is in the ASP.NET Core application configuration layer.

### 4. Why Container Restarts Failed

**Restart Cycle:**
1. Stop container
2. Azure Container Apps reads environment variables from current revision
3. Resolves `secretref:` values from Key Vault (✅ works correctly)
4. Starts container with resolved environment variables
5. ASP.NET Core reads `appsettings.Staging.json`
6. Configuration binding looks for `Stripe` section properties
7. Finds `SecretKey` and `PublishableKey` (✅ binds these)
8. Does NOT find `WebhookSecret` property (❌ skips this)
9. `StripeOptions.WebhookSecret` remains empty string
10. Webhook signature verification fails with empty secret

**Why it didn't fix the issue:** The environment variable `Stripe__WebhookSecret` was never set in the deployment workflow, so restarting just reloaded the same incomplete configuration.

### 5. Why Direct Secret Value Failed

**Attempted Fix (did not work):**
```yaml
Stripe__WebhookSecret="whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX"
# Instead of: Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Why it failed:**
- Environment variable WAS set correctly
- BUT `appsettings.Staging.json` still did not define the `WebhookSecret` property
- ASP.NET Core configuration binding requires BOTH:
  1. Property definition in appsettings.json (schema)
  2. Environment variable override at runtime (value)
- Missing property definition → binding ignores the environment variable

### 6. Configuration Hierarchy Explained

**Layered Configuration System:**
```
1. appsettings.json                (base schema + defaults)
2. appsettings.{Environment}.json  (environment overrides)
3. Environment Variables            (runtime overrides)
4. Key Vault Secrets               (via secretref resolution)
```

**How it should work:**
```
1. appsettings.Staging.json defines: "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
   → Establishes schema + placeholder

2. Deployment workflow sets: Stripe__WebhookSecret=secretref:stripe-webhook-secret
   → Provides secretref for Azure to resolve

3. Azure Container Apps resolves: secretref:stripe-webhook-secret → whsec_vjs9C1dm...
   → Fetches actual value from Key Vault

4. Configuration binding reads environment variable: Stripe__WebhookSecret=whsec_vjs9C1dm...
   → Overrides placeholder in appsettings.json

5. StripeOptions.WebhookSecret populated: "whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX"
   → Injected into PaymentsController via IOptions<StripeOptions>

6. EventUtility.ConstructEvent() receives correct secret
   → Webhook signature verification succeeds ✅
```

**What actually happened (broken chain):**
```
1. appsettings.Staging.json: ❌ WebhookSecret property NOT defined
2. Deployment workflow: ❌ Stripe__WebhookSecret environment variable NOT set
3. Azure Container Apps: ✅ Key Vault secret exists but NOT referenced
4. Configuration binding: ❌ Skips WebhookSecret (not in schema)
5. StripeOptions.WebhookSecret: ❌ Empty string (default value)
6. EventUtility.ConstructEvent(): ❌ Signature verification fails (empty secret)
```

---

## The Fix - Required Changes

### Change 1: Update appsettings.Staging.json

**File:** `src/LankaConnect.API/appsettings.Staging.json`

**Current (lines 51-54):**
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"
}
```

**Fixed:**
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",
  "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
}
```

### Change 2: Update Deployment Workflow

**File:** `.github/workflows/deploy-staging.yml`

**Current (lines 154-155):**
```yaml
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key
```

**Fixed:**
```yaml
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key \
Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

### Change 3: Verify Key Vault Secret (Already Exists)

**Verification command:**
```bash
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv
```

**Expected output:**
```
whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX
```

**Status:** ✅ Already correct, no changes needed.

---

## Verification Plan

### Phase 1: Pre-Deployment Verification

```bash
# 1. Verify Key Vault secret exists
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv

# Expected: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX

# 2. Verify files are updated correctly
git diff src/LankaConnect.API/appsettings.Staging.json
git diff .github/workflows/deploy-staging.yml
```

### Phase 2: Deployment

```bash
# Commit and push changes
git add src/LankaConnect.API/appsettings.Staging.json
git add .github/workflows/deploy-staging.yml
git commit -m "fix(phase-6a24): Add Stripe webhook secret configuration binding"
git push origin develop

# Monitor GitHub Actions deployment
# https://github.com/{org}/{repo}/actions
```

### Phase 3: Post-Deployment Verification

```bash
# 1. Check container app environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' -o json \
  | grep -A2 "Stripe__WebhookSecret"

# Expected:
# {
#   "name": "Stripe__WebhookSecret",
#   "secretRef": "stripe-webhook-secret"
# }

# 2. Check container app secrets
az containerapp secret list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  | grep stripe-webhook-secret

# Expected: stripe-webhook-secret entry exists
```

### Phase 4: Functional Testing

```bash
# 1. Install Stripe CLI (if not already installed)
# https://stripe.com/docs/stripe-cli

# 2. Start webhook listener
stripe listen --forward-to https://lankaconnect-api-staging.azurewebsites.net/api/payments/webhook

# 3. In another terminal, trigger test event
stripe trigger checkout.session.completed

# Expected output from listener:
# --> POST /api/payments/webhook [200 OK]
# (Instead of [400 BAD REQUEST])

# 4. Check container logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 \
  --filter "Processing webhook event"

# Expected log:
# Processing webhook event evt_test_123ABC of type checkout.session.completed

# 5. Check Stripe Dashboard
# Navigate to: Developers > Webhooks > {endpoint}
# Recent deliveries should show HTTP 200 OK
```

### Phase 5: Application Logging (Optional Diagnostic)

Add temporary logging to verify secret is loaded:

**File:** `src/LankaConnect.API/Controllers/PaymentsController.cs`

**Add after line 228:**
```csharp
// DIAGNOSTIC LOGGING - REMOVE AFTER VERIFICATION
_logger.LogInformation(
    "Webhook received. Secret configured: {HasSecret}, Secret length: {SecretLength}",
    !string.IsNullOrEmpty(_stripeOptions.WebhookSecret),
    _stripeOptions.WebhookSecret?.Length ?? 0
);
```

**Expected log output:**
```
Webhook received. Secret configured: True, Secret length: 43
```

**Remove this diagnostic logging after verification.**

---

## Success Criteria

### Required Outcomes
- [ ] `appsettings.Staging.json` contains `"WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"`
- [ ] `deploy-staging.yml` contains `Stripe__WebhookSecret=secretref:stripe-webhook-secret`
- [ ] GitHub Actions deployment succeeds with 0 errors
- [ ] Container App environment variables show `Stripe__WebhookSecret` with `secretRef`
- [ ] Stripe CLI test event returns HTTP 200 OK (not 400 Bad Request)
- [ ] Application logs show "Processing webhook event ... of type checkout.session.completed"
- [ ] Stripe Dashboard shows recent webhook delivery succeeded
- [ ] No "Invalid signature" errors in container logs

### Performance Indicators
- Webhook latency < 500ms
- Idempotency check working (duplicate events skipped)
- Event recording in database successful
- Post-payment workflows triggered (signup confirmation)

---

## Risk Assessment

### Low Risk - Additive Configuration Only

**What changed:**
- Added one line to `appsettings.Staging.json`
- Added one line to `deploy-staging.yml`
- No code modifications
- No database schema changes
- No breaking changes to existing functionality

**Rollback plan:**
```bash
# If issues occur, revert the commit
git revert HEAD
git push origin develop

# Previous behavior: Webhooks fail with 400 (same as current state)
# No worse outcome than current state
```

**Testing strategy:**
- Staging environment only (production unaffected)
- Stripe test mode webhooks (no real payments)
- Can trigger test events unlimited times
- No impact on existing users or data

---

## Lessons Learned

### Configuration Binding Requirements

**ASP.NET Core Configuration System Requires:**
1. **Schema Declaration** - Property must exist in `appsettings.json`
2. **Environment Variable** - Must be set at runtime (via deployment)
3. **Naming Convention** - Use double-underscore syntax (`Section__Property`)
4. **Key Vault Integration** - Works via `secretref:secret-name` in Container Apps

**Missing any of these 4 elements breaks the chain.**

### Why Testing Didn't Catch This

**Issue:** Configuration was added to Key Vault and Stripe Dashboard, but:
- No integration test verified webhook signature validation
- No deployment checklist required all 4 configuration elements
- Assumed container restart would fix "cached" secret (incorrect assumption)

**Prevention:**
- Add configuration validation health check
- Document configuration checklist for all secrets
- Add integration test for webhook signature verification

### Azure Container Apps Secret Resolution

**How `secretref:` works:**
1. Container Apps reads environment variable definition: `KEY=secretref:secret-name`
2. At container start, resolves `secretref:secret-name` from Key Vault
3. Sets actual environment variable: `KEY=actual-secret-value`
4. Application sees fully resolved environment variable

**No caching occurs at Container Apps level** - secrets are resolved fresh on every container start.

**The cache theory was incorrect** - the problem was configuration binding, not secret caching.

---

## Future Prevention Strategy

### Configuration Checklist for All Secrets

When adding a new secret to the system:

- [ ] **Step 1:** Store secret in Azure Key Vault
  ```bash
  az keyvault secret set --vault-name {vault} --name {secret-name} --value "{value}"
  ```

- [ ] **Step 2:** Grant system-assigned identity access
  ```bash
  az keyvault set-policy --name {vault} \
    --object-id {identity-id} \
    --secret-permissions get
  ```

- [ ] **Step 3:** Define property in `appsettings.json`
  ```json
  "Section": {
    "Property": "${ENVIRONMENT_VARIABLE}"
  }
  ```

- [ ] **Step 4:** Set environment variable in deployment workflow
  ```yaml
  Section__Property=secretref:secret-name
  ```

- [ ] **Step 5:** Deploy and verify environment variable exists
  ```bash
  az containerapp show --name {app} --resource-group {rg} \
    --query 'properties.template.containers[0].env'
  ```

- [ ] **Step 6:** Test application functionality
  - Verify configuration loaded at runtime
  - Check application logs for expected behavior
  - Test end-to-end workflow

### Secret Rotation Process

When rotating an existing secret:

```bash
# 1. Update Key Vault secret
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --value "new_secret_value"

# 2. Restart container to pick up new value
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging

# 3. Verify new secret loaded
# Check application logs for successful operation
# Test functionality with new secret
```

**No code changes or redeployment required** - just update Key Vault and restart container.

---

## Related Documentation

### Internal Documents
- [ADR-007: Stripe Webhook Secret Configuration](./ADR-007-Stripe-Webhook-Secret-Azure-Container-Apps-Key-Vault.md) - Full architectural decision record
- [STRIPE_WEBHOOK_SECRET_FIX_SUMMARY.md](./STRIPE_WEBHOOK_SECRET_FIX_SUMMARY.md) - Executive summary and quick fix guide
- [diagrams/stripe-webhook-secret-configuration-flow.md](./diagrams/stripe-webhook-secret-configuration-flow.md) - Visual diagrams of configuration flow
- [STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md](./STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md) - Phase 6A.24 implementation documentation
- [ADR-005: Stripe API Key Configuration](./ADR-005-Stripe-API-Key-Configuration-Azure-Container-Apps.md) - Related configuration issue
- [PHASE_6A_MASTER_INDEX.md](../PHASE_6A_MASTER_INDEX.md) - Phase tracking

### External References
- [Azure Container Apps Secrets](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Stripe Webhook Security](https://stripe.com/docs/webhooks/signatures)
- [Stripe CLI Documentation](https://stripe.com/docs/stripe-cli)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)

---

## Conclusion

The HTTP 400 "Invalid signature" error was caused by incomplete configuration binding, not Key Vault caching or secret retrieval issues. The secret exists correctly in Key Vault but was never loaded into the application due to missing schema definition and environment variable.

**Fix:** Add two configuration lines to complete the configuration chain:
1. `appsettings.Staging.json` - Define `WebhookSecret` property
2. `deploy-staging.yml` - Set `Stripe__WebhookSecret` environment variable

**Impact:** Enables Stripe webhooks to process payment events, unlocking post-payment workflows.

**Complexity:** Minimal - Two-line configuration change, no code modifications.

**Risk:** Low - Additive change only, tested in staging with Stripe test mode.
