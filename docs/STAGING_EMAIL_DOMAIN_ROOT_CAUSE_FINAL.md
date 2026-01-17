# Staging Custom Email Domain - Final Root Cause Analysis

**Date**: 2026-01-17
**Issue**: Custom domain email not working in staging
**Status**: 🎯 ROOT CAUSE IDENTIFIED

---

## ✅ Definitive Root Cause

### The Problem:
**Custom domain `lankaconnect.app` is NOT linked to Communication Service `lankaconnect-communication`**

### Evidence:

#### Test 1: Azure CLI Email Send (FAILED)
```bash
$ az communication email send \
  --connection-string "..." \
  --sender "noreply@lankaconnect.app" \
  --to "niroshhh@gmail.com"

ERROR: (DomainNotLinked) The specified sender domain has not been linked.
```

#### Test 2: Check Linked Domains
```bash
$ az communication show --name lankaconnect-communication

{
  "linkedDomains": [
    "/subscriptions/.../emailServices/lankaconnect-email/domains/AzureManagedDomain"
  ]
}
```

**Result**: Only Azure Managed Domain is linked, NOT `lankaconnect.app`

---

## Why Previous Analysis Was Incomplete

### What Was Correct:
1. ✅ Custom domain exists in Email Service
2. ✅ DNS records are configured
3. ✅ Domain verification passed
4. ✅ DKIM/DKIM2 verification passed
5. ✅ Container App environment variables updated

### What Was MISSED:
❌ **Domain linking step** - Creating a domain ≠ Linking it to Communication Service

---

## Architecture Understanding

### Azure Communication Services Email Flow:

```
1. Email Service (lankaconnect-email)
   ├── Domain: AzureManagedDomain ✅
   └── Domain: lankaconnect.app ✅ (created, verified)

2. Communication Service (lankaconnect-communication)
   └── Linked Domains:
       └── AzureManagedDomain ONLY ❌
       └── lankaconnect.app MISSING ❌

3. Application (Container App)
   └── Uses Communication Service connection string
   └── Specifies sender: noreply@lankaconnect.app
   └── FAILS: Domain not linked to Communication Service
```

### The Missing Step:
The domain must be **explicitly linked** from Email Service to Communication Service.

---

## Comparison: Staging vs Production

| Component | Staging | Production |
|-----------|---------|------------|
| **Email Service** | `lankaconnect-email` | `lankaconnect-email-prod` |
| **Custom Domain Created** | ✅ Yes | ✅ Yes |
| **Domain Verified** | ✅ Yes (Domain + DKIM) | ✅ Yes (All) |
| **SPF Verified** | ⚠️ In Progress | ✅ Yes |
| **Communication Service** | `lankaconnect-communication` | `lankaconnect-communication-prod` |
| **Linked Domains** | ❌ Azure Managed ONLY | ✅ `lankaconnect.app` ONLY |
| **Emails Working** | ❌ DomainNotLinked error | ✅ Working |

---

## Why Domain Linking Failed

### Attempt to Link Domain:
```bash
$ az communication update \
  --name lankaconnect-communication \
  --linked-domains ".../lankaconnect.app"

ERROR: (DomainValidationError) Requested domain is not in a valid state
for linking to CommunicationServices resource
```

### Root Cause of Linking Failure:
**SPF verification status: `VerificationFailed`**

Azure requires **all DNS verifications** to pass before allowing domain linking:
- ✅ Domain Verification: Verified
- ✅ DKIM: Verified
- ✅ DKIM2: Verified
- ❌ SPF: VerificationFailed → **BLOCKS LINKING**

---

## The SPF Problem

### Why SPF Fails in Staging but Works in Production:

Both use the SAME DNS record:
```
Type: TXT
Host: @
Value: v=spf1 include:spf.protection.outlook.com -all
```

**Production Status**: `SPF: Verified` ✅
**Staging Status**: `SPF: VerificationFailed` ❌

### Possible Reasons:
1. **DNS Caching**: Azure's DNS checker cached old results
2. **Timing**: Production domain created first, DNS fully propagated
3. **CNAME Interference**: Root CNAME may cause intermittent TXT resolution issues
4. **Azure Bug**: SPF verification may be inconsistent across regions/services

### Current Action:
Re-initiated SPF verification:
```bash
$ az communication email domain initiate-verification --verification-type SPF
Status: VerificationInProgress ⏳
```

---

## Solution Paths

### Option A: Wait for SPF Verification (Current Approach) ⏳
1. ✅ Initiated SPF re-verification
2. ⏳ Waiting for Azure to re-check DNS
3. ⏳ Once SPF passes → Link domain to Communication Service
4. ⏳ Test email sending

**Timeline**: Could take 5-30 minutes for DNS re-verification

### Option B: Use Azure Managed Domain (Temporary Workaround) ✅
1. Keep using `DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net`
2. Accept 30 emails/hour limit
3. Wait for SPF to verify
4. Switch to custom domain later

**Impact**: Rate limit remains at 30/hour until SPF passes

### Option C: Contact Azure Support (If SPF won't verify) 🆘
If SPF verification keeps failing:
1. Open Azure support ticket
2. Provide: Subscription ID, Resource Group, Domain name
3. Request manual SPF verification override
4. Link domain once approved

---

## Why Container App Showed Custom Domain

### The Confusion:
Container App logs showed:
```
Azure Email Service initialized with sender: noreply@lankaconnect.app ✅
```

This made it appear the configuration was correct!

### Why This Happened:
1. ✅ Key Vault secret was updated: `AZURE-EMAIL-SENDER-ADDRESS=noreply@lankaconnect.app`
2. ✅ Container App read the secret correctly
3. ✅ Application initialized with custom domain sender
4. ❌ **But domain not linked** → Email sends fail with `DomainNotLinked`

**Lesson**: Application configuration ≠ Azure resource configuration

---

## Next Steps (Pending SPF Verification)

### Step 1: Monitor SPF Verification
```bash
# Check every 5 minutes
az communication email domain show \
  --email-service-name lankaconnect-email \
  --resource-group lankaconnect-staging \
  --domain-name lankaconnect.app \
  --query "verificationStates.SPF"
```

### Step 2: Link Domain Once SPF Passes
```powershell
az communication update `
  --name lankaconnect-communication `
  --resource-group lankaconnect-staging `
  --linked-domains '/subscriptions/ebb8304a-6374-4db0-8de5-e8678afbb5b5/resourceGroups/lankaconnect-staging/providers/Microsoft.Communication/emailServices/lankaconnect-email/domains/lankaconnect.app'
```

### Step 3: Verify Linking
```bash
az communication show \
  --name lankaconnect-communication \
  --resource-group lankaconnect-staging \
  --query linkedDomains
```

### Step 4: Test Email Sending
```bash
az communication email send \
  --connection-string "..." \
  --sender "noreply@lankaconnect.app" \
  --to "niroshhh@gmail.com"
```

### Step 5: Test via Application
1. Login to staging UI
2. Send newsletter
3. Verify emails arrive from `noreply@lankaconnect.app`

---

## Summary

| Question | Answer |
|----------|--------|
| **Is custom domain created?** | ✅ Yes |
| **Is DNS configured correctly?** | ✅ Yes |
| **Is domain verified?** | ✅ Partially (Domain + DKIM yes, SPF in progress) |
| **Is domain linked to Communication Service?** | ❌ **NO - THIS IS THE ISSUE** |
| **Why not linked?** | SPF verification must pass first |
| **Will it work after SPF passes?** | ✅ Yes, once linked |
| **What's the rate limit now?** | 30/hour (Azure Managed Domain) |
| **What will it be after?** | 1,800/hour (Custom Domain) |

---

## Files Referenced

- Test Instructions: [AZURE_EMAIL_DOMAIN_TEST_INSTRUCTIONS.md](./AZURE_EMAIL_DOMAIN_TEST_INSTRUCTIONS.md)
- Initial Setup: [STAGING_CUSTOM_EMAIL_DOMAIN_SETUP.md](./STAGING_CUSTOM_EMAIL_DOMAIN_SETUP.md)
- Container App Issue: [STAGING_EMAIL_DOMAIN_ISSUE_RESOLUTION.md](./STAGING_EMAIL_DOMAIN_ISSUE_RESOLUTION.md)

---

**Status**: ⏳ Waiting for SPF verification to complete
**ETA**: 5-30 minutes
**Action Required**: Monitor SPF status, then link domain
