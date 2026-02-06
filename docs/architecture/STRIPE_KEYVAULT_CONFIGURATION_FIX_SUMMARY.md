# Stripe Key Vault Configuration Fix - Executive Summary

**Date**: 2025-12-13
**Issue**: Stripe payment integration failing with "Invalid API Key" error in staging
**Root Cause**: Missing Key Vault permissions for System-Assigned Managed Identity
**Status**: SOLUTION READY TO DEPLOY

## Quick Answer to Your Questions

### 1. Is the managed identity approach the right solution?

**YES** - But you were using the WRONG identity type. Here's what we discovered:

**The Container App ALREADY HAS a System-Assigned Managed Identity:**
- Principal ID: `bf69f3f8-e9c6-464a-9d1f-0038f90e8d03`
- Type: `SystemAssigned`

You were about to create a USER-ASSIGNED identity (`lankaconnect-staging-identity`), but that's unnecessary. The System-Assigned identity is already there and is the correct approach.

### 2. What's the REAL problem?

**THREE issues found:**

#### Issue 1: Missing Key Vault Permissions (PRIMARY ISSUE)
The System-Assigned Managed Identity exists but doesn't have "Get" and "List" permissions for the newly added Stripe secrets in Key Vault.

**Solution**: Grant permissions (not create new identity)
```bash
az keyvault set-policy \
  --name lankaconnect-staging-kv \
  --object-id bf69f3f8-e9c6-464a-9d1f-0038f90e8d03 \
  --secret-permissions get list
```

#### Issue 2: Hardcoded API Keys in appsettings.Staging.json (CRITICAL)
The configuration file had hardcoded test API keys that would override environment variables:

**Before (WRONG)**:
```json
{
  "Stripe": {
    "SecretKey": "sk_test_4eC39HqLyjWDarhtT1l8w65C",
    "PublishableKey": "pk_test_51234567890abcdefghijklmnop"
  }
}
```

**After (FIXED)**:
```json
{
  "Stripe": {
    "SecretKey": "${STRIPE_SECRET_KEY}",
    "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"
  }
}
```

#### Issue 3: ASP.NET Core Configuration Hierarchy
Environment variables in Container Apps override appsettings.json values, BUT only if appsettings.json doesn't have hardcoded values that are processed first.

### 3. What about other secrets (JWT, database, email)?

They work because:
1. The System-Assigned Managed Identity already has Key Vault access
2. The deployment workflow correctly uses `secretref:` pattern
3. Their appsettings.Staging.json files use variable placeholders (not hardcoded values)

**The Stripe secrets were following the same pattern EXCEPT:**
- They're NEW, so permissions might need explicit refresh
- The appsettings.Staging.json had hardcoded test keys

### 4. Recommended solution?

**Use the EXISTING System-Assigned Managed Identity** (DO NOT create user-assigned identity)

**Why System-Assigned is correct:**
1. Already exists and configured
2. Consistent with current infrastructure
3. Azure best practice for single-resource scenarios
4. Least privilege (tied to Container App lifecycle)
5. No additional resources to manage

## What We Fixed

### Change 1: Updated appsettings.Staging.json
**File**: `src/LankaConnect.API/appsettings.Staging.json`

Removed hardcoded Stripe test API keys and replaced with environment variable placeholders:
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"
}
```

This ensures environment variables from Container App configuration take precedence.

### Change 2: Created Fix Script
**File**: `scripts/fix-stripe-keyvault-access.ps1`

Automated script that:
1. Retrieves System-Assigned Managed Identity
2. Verifies Stripe secrets exist in Key Vault
3. Grants Key Vault permissions to the identity
4. Validates Container App environment variables
5. Checks for hardcoded values in appsettings files

**Usage**:
```powershell
# Dry run (see what would change)
.\scripts\fix-stripe-keyvault-access.ps1 -WhatIf

# Apply the fix
.\scripts\fix-stripe-keyvault-access.ps1
```

### Change 3: Created Architecture Decision Record
**File**: `docs/architecture/ADR-005-Stripe-API-Key-Configuration-Azure-Container-Apps.md`

Comprehensive documentation including:
- Root cause analysis
- Decision rationale
- Architecture diagrams
- Implementation steps
- Risk mitigation
- Success criteria

## Implementation Steps (In Order)

### Step 1: Grant Key Vault Permissions (CRITICAL)
```bash
# Get Container App's System-Assigned Identity Principal ID
PRINCIPAL_ID=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "identity.principalId" -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name lankaconnect-staging-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

**OR** use the automated script:
```powershell
.\scripts\fix-stripe-keyvault-access.ps1
```

### Step 2: Commit Configuration Changes
The appsettings.Staging.json fix is already done. Commit it:
```bash
git add src/LankaConnect.API/appsettings.Staging.json
git commit -m "fix: Remove hardcoded Stripe API keys from appsettings.Staging.json"
```

### Step 3: Verify Deployment Workflow
The workflow (`.github/workflows/deploy-staging.yml`) is already correct:
```yaml
Stripe__SecretKey=secretref:stripe-secret-key
Stripe__PublishableKey=secretref:stripe-publishable-key
```

No changes needed here.

### Step 4: Deploy to Staging
```bash
# Push to develop branch to trigger deployment
git push origin develop
```

### Step 5: Monitor Deployment
```bash
# Watch deployment logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 --follow

# Check for Stripe initialization messages
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -i stripe
```

### Step 6: Verify Stripe Integration
```bash
# Get Container App URL
URL=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

# Test health endpoint
curl https://$URL/health

# Test Stripe webhook endpoint (should return 400 for invalid payload)
curl -X POST https://$URL/api/webhooks/stripe \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

## Architecture Diagrams

### How Secrets Flow from Key Vault to Container App

```
GitHub Actions
     │
     │ 1. Deploy with: Stripe__SecretKey=secretref:stripe-secret-key
     │
     ▼
┌──────────────────────────────────────────────────────────┐
│  Azure Container App (lankaconnect-api-staging)          │
│                                                           │
│  Environment Variables (highest priority):               │
│  - Stripe__SecretKey=secretref:stripe-secret-key         │
│  - Stripe__PublishableKey=secretref:stripe-publishable...│
│                                                           │
│  ┌─────────────────────────────────────────────┐        │
│  │ System-Assigned Managed Identity             │        │
│  │ Principal: bf69f3f8-e9c6-464a-9d1f-0038f90e │        │
│  └─────────────────┬───────────────────────────┘        │
│                    │                                      │
│                    │ 2. Uses identity to resolve secrets │
└────────────────────┼──────────────────────────────────────┘
                     │
                     │ 3. Key Vault Access Policy:
                     │    - Get Secret Permission ✓
                     │    - List Secret Permission ✓
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│  Azure Key Vault (lankaconnect-staging-kv)               │
│                                                           │
│  Secrets:                                                 │
│  ├── stripe-secret-key: "sk_live_xxx..." ✓               │
│  ├── stripe-publishable-key: "pk_live_xxx..." ✓          │
│  ├── database-connection-string ✓                        │
│  ├── jwt-secret-key ✓                                    │
│  └── ... (other secrets)                                 │
└──────────────────┬───────────────────────────────────────┘
                   │
                   │ 4. Returns secret values
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  ASP.NET Core Runtime                                    │
│                                                           │
│  Configuration Hierarchy (lowest to highest priority):   │
│  1. appsettings.json (base defaults)                     │
│  2. appsettings.Staging.json (env-specific placeholders) │
│  3. Environment Variables (WINS - from Key Vault) ✓      │
│                                                           │
│  Final Configuration:                                     │
│  - Stripe.SecretKey = "sk_live_xxx..." (from Key Vault)  │
│  - Stripe.PublishableKey = "pk_live_xxx..." (from KV)    │
└──────────────────────────────────────────────────────────┘
```

### Why Other Secrets Work But Stripe Doesn't

```
┌─────────────────────────────────────────────────────────────┐
│  Existing Secrets (JWT, Database, Email) - WORK ✓          │
├─────────────────────────────────────────────────────────────┤
│  1. Key Vault Access Policy: GRANTED (existing)             │
│  2. appsettings.Staging.json: Variable placeholders ✓       │
│  3. Deployment workflow: secretref: pattern ✓               │
│  4. Environment variables override config ✓                 │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Stripe Secrets - DON'T WORK ✗ (Before Fix)                │
├─────────────────────────────────────────────────────────────┤
│  1. Key Vault Access Policy: GRANTED (after this fix) ✓     │
│  2. appsettings.Staging.json: HARDCODED test keys ✗ (FIXED) │
│  3. Deployment workflow: secretref: pattern ✓               │
│  4. Environment variables: Can't override hardcoded ✗       │
└─────────────────────────────────────────────────────────────┘
```

## What You Were About To Do vs What You Should Do

### What You Were Planning (WRONG)
```bash
# Create new user-assigned identity
az identity create --name lankaconnect-staging-identity \
  --resource-group lankaconnect-staging

# Assign it to Container App
az containerapp identity assign \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --user-assigned <identity-resource-id>

# Grant Key Vault access to new identity
az keyvault set-policy ...
```

**Problems with this approach:**
- Creates unnecessary Azure resource
- Container App already has System-Assigned identity
- Inconsistent with existing secrets pattern
- More complex to manage

### What You Should Do (CORRECT)
```bash
# Use existing System-Assigned identity
PRINCIPAL_ID=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "identity.principalId" -o tsv)

# Grant permissions to existing identity
az keyvault set-policy \
  --name lankaconnect-staging-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

**Why this is better:**
- Uses existing infrastructure
- Consistent with current pattern
- Simpler to manage
- Follows Azure best practices

## Success Criteria

- [ ] Key Vault permissions granted to System-Assigned Managed Identity
- [ ] appsettings.Staging.json updated (hardcoded values removed)
- [ ] Changes committed to git
- [ ] Deployment to staging completes without errors
- [ ] Container App logs show "Stripe service initialized" message
- [ ] Stripe webhook endpoint responds (even if with validation error for test data)
- [ ] Payment flow works end-to-end

## Troubleshooting Guide

### If Deployment Still Fails

**Error**: "managed Identity with resource Id ... was not found"
**Cause**: Key Vault permissions not propagated yet
**Solution**: Wait 60 seconds and retry deployment

**Error**: "Invalid API Key provided"
**Cause**: Environment variables not overriding appsettings.Staging.json
**Solution**: Verify appsettings.Staging.json uses `${VARIABLE_NAME}` placeholders

**Error**: Webhook signature validation fails
**Cause**: Missing Stripe webhook secret
**Solution**: Add `Stripe__WebhookSecret` to Key Vault and deployment workflow

### Diagnostic Commands

```bash
# Check Container App identity
az containerapp show --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "identity" -o json

# Check Key Vault access policies
az keyvault show --name lankaconnect-staging-kv \
  --query "properties.accessPolicies" -o json

# Check Stripe secrets exist
az keyvault secret list --vault-name lankaconnect-staging-kv \
  --query "[?starts_with(name, 'stripe')].name" -o table

# View Container App environment variables
az containerapp show --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env" -o json

# Stream Container App logs
az containerapp logs show --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 --follow
```

## Files Changed

1. `src/LankaConnect.API/appsettings.Staging.json` - Removed hardcoded Stripe API keys
2. `docs/architecture/ADR-005-Stripe-API-Key-Configuration-Azure-Container-Apps.md` - Architecture decision record
3. `scripts/fix-stripe-keyvault-access.ps1` - Automated fix script
4. `docs/architecture/STRIPE_KEYVAULT_CONFIGURATION_FIX_SUMMARY.md` - This summary

## Next Steps

1. **Immediate (Required)**:
   - Run `.\scripts\fix-stripe-keyvault-access.ps1` to grant Key Vault permissions
   - Commit appsettings.Staging.json changes
   - Deploy to staging

2. **Short-term (Recommended)**:
   - Add Stripe webhook secret to Key Vault
   - Update deployment workflow with `Stripe__WebhookSecret` environment variable
   - Test complete payment flow in staging

3. **Long-term (Best Practice)**:
   - Create infrastructure-as-code templates (Bicep/Terraform)
   - Document secret rotation procedures
   - Add monitoring/alerting for Stripe API errors
   - Implement secret versioning for zero-downtime rotations

## References

- ADR-005: Full architecture decision record
- Azure Container Apps Managed Identity: https://learn.microsoft.com/azure/container-apps/managed-identity
- Azure Key Vault Access Policies: https://learn.microsoft.com/azure/key-vault/general/assign-access-policy
- ASP.NET Core Configuration: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
