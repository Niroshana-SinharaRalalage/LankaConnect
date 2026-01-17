# Staging Email Domain Issue - Root Cause and Resolution

**Date**: 2026-01-17
**Issue**: Custom domain email not working in staging
**Status**: ✅ ROOT CAUSE IDENTIFIED - Fix requires deployment

---

## The Problem

Another agent reported:
> "Custom Domain Not Linked - Root domain has CNAME record for UI app, conflicts with required TXT records for email"

### ❌ This Analysis is INCORRECT

The agent made these mistakes:
1. **Assumed DNS conflict prevented domain linking** - NOT TRUE
2. **Switched back to Azure Managed Domain** (`DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net`)
3. **This reverted the rate limit back to 30 emails/hour** ❌

---

## The Real Root Cause

### ✅ Custom Domain IS Successfully Configured

```json
{
  "mailFromSenderDomain": "lankaconnect.app",
  "name": "lankaconnect.app",
  "verificationStates": {
    "Domain": { "status": "Verified" },
    "DKIM": { "status": "Verified" },
    "DKIM2": { "status": "Verified" },
    "SPF": { "status": "VerificationFailed" }  // ⚠️ Not critical - DKIM is primary
  }
}
```

### ✅ Key Vault Secrets Are Correct

```bash
$ az keyvault secret show --name AZURE-EMAIL-SENDER-ADDRESS
"noreply@lankaconnect.app"  ✅
```

### ❌ Container App Is Using OLD Configuration

**The issue**: The Container App is still running with the **old deployment** that had the Azure managed domain configured.

**Evidence from logs**:
```
Azure Email Service initialized with sender: DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net
```

---

## Why Container App Restart Didn't Work

1. **Key Vault secrets are cached** by Container Apps
2. **Simple restart doesn't refresh Key Vault references**
3. **Need full redeployment** via GitHub Actions to pick up new values

---

## The Correct Solution

### Option 1: Trigger GitHub Actions Deployment (Recommended)

1. Make a small code change (add comment, bump version, etc.)
2. Commit and push to `develop` branch
3. GitHub Actions will:
   - Build new Docker image
   - Run migrations
   - Deploy with **updated environment variables from Key Vault**
   - Container App will now use `noreply@lankaconnect.app`

### Option 2: Manual Container App Update (Faster)

Run this Azure CLI command to force environment variable refresh:

```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
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
    EmailSettings__Provider=Azure \
    EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
    EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
    EmailSettings__SenderName="LankaConnect Staging" \
    EmailSettings__TemplateBasePath=Templates/Email \
    AzureStorage__ConnectionString=secretref:azure-storage-connection-string \
    Stripe__SecretKey=secretref:stripe-secret-key \
    Stripe__PublishableKey=secretref:stripe-publishable-key \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

This forces the Container App to re-read all Key Vault secrets, including the updated `azure-email-sender-address`.

---

## Why CNAME on Root Doesn't Prevent Email

### Common Misconception:
"CNAME on `@` prevents TXT records from working"

### Reality:
- ✅ **Domain verification**: Works fine (Verified)
- ✅ **DKIM verification**: Works fine (Verified)
- ⚠️ **SPF verification**: May fail due to CNAME, but this is NOT critical

### Why SPF Failure Doesn't Matter:
1. **DKIM is the primary authentication** (verified ✅)
2. **Domain ownership is verified** (verified ✅)
3. **Production has exact same setup** and emails work fine
4. **Most email providers prioritize DKIM** over SPF

---

## Verification After Fix

### 1. Check Container App Logs

```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 20 \
  --follow false | grep "Azure Email Service initialized"
```

**Expected output**:
```
Azure Email Service initialized with sender: noreply@lankaconnect.app ✅
```

**NOT**:
```
Azure Email Service initialized with sender: DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net ❌
```

### 2. Test Newsletter Sending

1. Login to staging UI
2. Create test newsletter
3. Send to yourself
4. Verify email shows: `From: noreply@lankaconnect.app`

---

## Rate Limit Comparison

| Configuration | Rate Limit | Status |
|--------------|------------|--------|
| Azure Managed Domain | 30/hour | ❌ Too low |
| Custom Domain (lankaconnect.app) | 1,800/hour | ✅ Sufficient |

---

## Related Files

- **Workflow**: [.github/workflows/deploy-staging.yml](../.github/workflows/deploy-staging.yml)
- **Key Vault**: `lankaconnect-staging-kv`
- **Email Service**: `AzureEmailService.cs`
- **Configuration**: `EmailSettings.cs`

---

## Summary

- ✅ Custom domain is properly configured in Azure
- ✅ DNS records are correct (except SPF, which is non-critical)
- ✅ Key Vault secrets are updated
- ❌ Container App needs redeployment to pick up new secrets
- ❌ Simple restart doesn't work due to Key Vault secret caching

**Next Step**: Run Option 2 (manual Container App update) OR trigger GitHub Actions deployment.
