# ADR-007: Stripe Webhook Secret Configuration in Azure Container Apps with Key Vault

**Date:** 2025-12-16
**Status:** Proposed
**Context:** Phase 6A.24 Stripe Webhook Configuration
**Decision Makers:** System Architecture Team

## Executive Summary

**Problem:** Azure Container Apps webhook endpoint consistently returns HTTP 400 "Invalid signature" despite correct secret configuration in Key Vault, multiple container restarts, and even direct secret value injection. The signature verification fails at `PaymentsController.cs:235` using `EventUtility.ConstructEvent()`.

**Root Cause:** Missing `Stripe__WebhookSecret` configuration binding in appsettings.json AND deployment workflow, causing ASP.NET Core configuration system to never populate `StripeOptions.WebhookSecret` property.

**Impact:** Critical - Stripe webhooks cannot process payment events (checkout.session.completed), blocking post-payment workflows.

---

## Context and Problem Statement

### System Architecture
```
Stripe Webhook Request
    ↓
Azure Container Apps Ingress (HTTPS)
    ↓
ASP.NET Core Middleware Pipeline
    ↓
PaymentsController.Webhook() [Line 221]
    ↓
EventUtility.ConstructEvent(json, signature, _stripeOptions.WebhookSecret) [Line 232-235]
    ↓
HTTP 400 "Invalid signature" [Line 275]
```

### Configuration Chain
1. **Azure Key Vault**: Stores `stripe-webhook-secret` = `whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX`
2. **Container App Environment Variable**: `Stripe__WebhookSecret=secretref:stripe-webhook-secret`
3. **ASP.NET Core Configuration**: Binds to `StripeOptions.WebhookSecret` via `services.Configure<StripeOptions>()`
4. **PaymentsController**: Injects `IOptions<StripeOptions>` and uses `_stripeOptions.WebhookSecret`

### Evidence of Failure
- **Key Vault Value**: Confirmed correct (`whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX`)
- **Stripe Dashboard**: Confirmed matching signing secret
- **Container Restarts**: 5+ attempts, no effect
- **Direct Secret Value**: Tried bypassing Key Vault reference - still fails
- **Webhook Logs**: Consistent HTTP 400 "Invalid signature"

---

## Root Cause Analysis

### Investigation Findings

#### 1. Configuration Binding Gap Discovered

**File Analysis:**
```csharp
// src/LankaConnect.Infrastructure/DependencyInjection.cs:252
services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
```

This binds the `"Stripe"` configuration section to `StripeOptions` class:
```csharp
// src/LankaConnect.Infrastructure/Payments/Configuration/StripeOptions.cs
public class StripeOptions
{
    public const string SectionName = "Stripe";
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty; // ⚠️ NEVER POPULATED
}
```

**Current appsettings.Staging.json (Lines 51-54):**
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"
  // ❌ MISSING: "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
}
```

**Current Deployment Workflow (Lines 154-155):**
```yaml
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key
# ❌ MISSING: Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

#### 2. Why the Secret Was Never Loaded

**ASP.NET Core Configuration Hierarchy:**
```
1. appsettings.json (base defaults)
2. appsettings.{Environment}.json (environment overrides)
3. Environment Variables (runtime overrides)
4. Key Vault Secrets via secretref (Azure Container Apps feature)
```

**What Happened:**
1. `appsettings.Staging.json` defines `Stripe` section with only 2 properties (SecretKey, PublishableKey)
2. Deployment sets `Stripe__SecretKey` and `Stripe__PublishableKey` environment variables
3. Configuration binding reads `Stripe` section → finds 2 properties → binds to `StripeOptions`
4. `StripeOptions.WebhookSecret` remains `string.Empty` (default value)
5. `EventUtility.ConstructEvent()` receives empty string → signature verification fails

**Why Container Restarts Didn't Help:**
- Container Apps correctly resolved `secretref:stripe-webhook-secret` from Key Vault
- BUT the environment variable `Stripe__WebhookSecret` was never set in deployment workflow
- Even if it was set, `appsettings.Staging.json` didn't define the property for binding

**Why Direct Secret Value Didn't Work:**
- The deployment workflow line attempted to set the value directly
- BUT without the property in appsettings.json, ASP.NET Core configuration system ignores it
- Configuration binding only populates properties defined in the appsettings schema

#### 3. The Cache Theory Was Incorrect

**Initial Hypothesis:** Azure Container Apps caches Key Vault secrets and doesn't refresh on restart.

**Actual Reality:**
- Azure Container Apps resolves `secretref:` values on EVERY container start
- No caching occurs at the Container Apps level
- The problem was never about Key Vault secret retrieval
- The problem was configuration binding at the ASP.NET Core level

---

## Decision

### Solution Architecture

**Three-Part Fix Required:**

#### Part 1: Update appsettings.Staging.json
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",
  "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
}
```

#### Part 2: Update Deployment Workflow
```yaml
# .github/workflows/deploy-staging.yml (after line 155)
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key \
Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

#### Part 3: Verify Key Vault Secret Exists
```bash
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv
```

### Configuration Binding Flow (After Fix)
```
1. Azure Container Apps sets: Stripe__WebhookSecret=secretref:stripe-webhook-secret
2. Container Apps resolves secretref → retrieves from Key Vault
3. Environment variable set: Stripe__WebhookSecret=whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX
4. ASP.NET Core configuration reads environment variable
5. appsettings.Staging.json defines Stripe.WebhookSecret property
6. Configuration binding populates StripeOptions.WebhookSecret
7. PaymentsController receives correct value via IOptions<StripeOptions>
8. EventUtility.ConstructEvent() verifies signature successfully ✅
```

---

## Consequences

### Positive
1. **Webhook signature verification will work** - Correct secret loaded into application
2. **Post-payment workflows enabled** - checkout.session.completed events processed
3. **Secure secret management** - Key Vault integration maintained
4. **Consistent pattern** - Matches existing Stripe configuration approach

### Negative
1. **Deployment required** - Changes require new container revision
2. **Configuration complexity** - Three-layer configuration (appsettings + env vars + Key Vault)

### Risks Mitigated
1. **Secret exposure** - Using Key Vault secretref instead of direct values
2. **Configuration drift** - appsettings.json and deployment workflow now synchronized
3. **Future secret rotation** - Updating Key Vault secret will propagate correctly

---

## Verification Strategy

### Step-by-Step Verification

#### 1. Pre-Deployment Verification
```bash
# Verify Key Vault secret exists and has correct value
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv

# Expected: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX
```

#### 2. Post-Deployment Verification
```bash
# Check container app environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' -o json

# Verify presence of:
# {
#   "name": "Stripe__WebhookSecret",
#   "secretRef": "stripe-webhook-secret"
# }
```

#### 3. Runtime Verification
```bash
# Test webhook endpoint with Stripe CLI
stripe listen --forward-to https://lankaconnect-api-staging.azurewebsites.net/api/payments/webhook

# Trigger test event
stripe trigger checkout.session.completed

# Check logs for successful processing
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 \
  --filter "Processing webhook event"
```

#### 4. Application Logging Enhancement
Add temporary diagnostic logging to PaymentsController.cs:
```csharp
[HttpPost("webhook")]
[AllowAnonymous]
public async Task<IActionResult> Webhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

    // DIAGNOSTIC LOGGING (REMOVE AFTER VERIFICATION)
    _logger.LogInformation("Webhook received. Secret length: {SecretLength}, Signature: {Signature}",
        _stripeOptions.WebhookSecret?.Length ?? 0,
        signatureHeader?.Substring(0, Math.Min(20, signatureHeader?.Length ?? 0)));

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            signatureHeader,
            _stripeOptions.WebhookSecret
        );
        // ... rest of code
    }
}
```

### Success Criteria
1. ✅ Key Vault secret verified
2. ✅ Container App environment variable shows `secretref:stripe-webhook-secret`
3. ✅ Application logs show webhook secret length = 43 (correct for whsec_ format)
4. ✅ Stripe CLI test event returns HTTP 200 OK
5. ✅ Application logs show "Processing webhook event ... of type checkout.session.completed"

---

## Debugging Commands Reference

### Container App Configuration
```bash
# Show all environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' -o table

# Show secrets configuration
az containerapp secret list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging -o table

# Show system-assigned identity (for Key Vault access)
az containerapp identity show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging
```

### Key Vault Access
```bash
# List all secrets
az keyvault secret list \
  --vault-name lankaconnect-staging-kv \
  --query '[].name' -o table

# Show specific secret value (requires Get permission)
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query value -o tsv

# Check access policies
az keyvault show \
  --name lankaconnect-staging-kv \
  --query 'properties.accessPolicies' -o json
```

### Container Logs
```bash
# Real-time logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow

# Recent logs with filter
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 \
  --filter "Stripe"

# Logs from specific time range
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 \
  --since 30m
```

### Stripe Webhook Testing
```bash
# Install Stripe CLI
# https://stripe.com/docs/stripe-cli

# Listen to events and forward to staging
stripe listen --forward-to https://lankaconnect-api-staging.azurewebsites.net/api/payments/webhook

# Trigger test events
stripe trigger checkout.session.completed
stripe trigger payment_intent.succeeded
stripe trigger payment_intent.payment_failed

# View recent webhook attempts in Stripe Dashboard
# https://dashboard.stripe.com/webhooks/{endpoint_id}
```

---

## Prevention Strategy for Future Secret Rotation

### Secret Rotation Procedure
```bash
# 1. Update Stripe webhook endpoint (generates new signing secret)
# Via Stripe Dashboard or CLI:
stripe webhooks update we_1234 --url https://new-endpoint.com

# 2. Update Key Vault secret
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --value "whsec_NEW_SECRET_HERE"

# 3. Restart container to pick up new secret
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging

# 4. Verify webhook works with new secret
stripe listen --forward-to https://lankaconnect-api-staging.azurewebsites.net/api/payments/webhook
stripe trigger checkout.session.completed

# 5. Monitor logs for successful processing
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 \
  --filter "Processing webhook event"
```

### Configuration Checklist (For All Secrets)
- [ ] Secret exists in Key Vault with correct name
- [ ] Property defined in appsettings.json with placeholder `${SECRET_NAME}`
- [ ] Environment variable set in deployment workflow with `secretref:secret-name`
- [ ] Container App has system-assigned identity enabled
- [ ] System identity has "Get" permission in Key Vault access policies
- [ ] Secret name uses kebab-case in Key Vault (e.g., `stripe-webhook-secret`)
- [ ] Environment variable uses double-underscore syntax (e.g., `Stripe__WebhookSecret`)

---

## Architectural Best Practices

### 1. Secret Configuration Pattern
```
Secret Definition (Key Vault)
    ↓
Container App Secret Reference (secretref:secret-name)
    ↓
Environment Variable (Section__Property=secretref:secret-name)
    ↓
appsettings.json Schema ("Property": "${ENVIRONMENT_VARIABLE}")
    ↓
Options Pattern (IOptions<TOptions>)
    ↓
Dependency Injection (controller/service)
```

### 2. Configuration Binding Rules
- **appsettings.json** defines the SCHEMA (all properties must be declared)
- **Environment variables** override appsettings values at runtime
- **Key Vault secretref** is resolved by Container Apps BEFORE app startup
- **Options pattern** provides strongly-typed access to configuration

### 3. Security Principles
- **Never commit secrets** to source control (use placeholders only)
- **Use Key Vault** for all production secrets
- **System-assigned identity** for authentication (no credentials in code)
- **Least privilege access** - only "Get" permission on specific secrets
- **Audit logging** - enable Key Vault diagnostic logs for compliance

### 4. Operational Excellence
- **Health checks** should validate critical configuration loaded
- **Structured logging** should log configuration keys (NOT values)
- **Deployment verification** should test end-to-end functionality
- **Secret rotation** should be tested in staging before production

---

## Related Documentation

### Internal Documents
- [PHASE_6A_MASTER_INDEX.md](../PHASE_6A_MASTER_INDEX.md) - Phase tracking
- [ADR-005-Stripe-API-Key-Configuration-Azure-Container-Apps.md](./ADR-005-Stripe-API-Key-Configuration-Azure-Container-Apps.md) - Related Stripe config issue
- [STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md](./STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md) - Phase 6A.24 implementation

### External References
- [Azure Container Apps Secrets](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Stripe Webhook Security](https://stripe.com/docs/webhooks/signatures)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)

---

## Implementation Status

**Current Phase:** Analysis Complete
**Next Steps:**
1. Update `appsettings.Staging.json` with WebhookSecret property
2. Update `.github/workflows/deploy-staging.yml` with Stripe__WebhookSecret environment variable
3. Deploy to staging environment
4. Verify webhook signature validation succeeds
5. Test end-to-end payment flow with Stripe CLI

**Estimated Implementation Time:** 15 minutes
**Risk Level:** Low (additive change, no breaking modifications)