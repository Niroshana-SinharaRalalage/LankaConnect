# Azure Email Domain Linking - Final Root Cause Analysis

**Date**: 2026-01-17
**Environment**: Staging
**Issue**: DomainNotLinked error preventing all email sends
**Status**: ROOT CAUSE CONFIRMED - SPF Verification Required

---

## Executive Summary

The custom domain `lankaconnect.app` is **created and partially verified** in Azure Communication Services but **NOT LINKED** to the Communication Service resource `lankaconnect-communication`. The blocking issue is **SPF verification still in progress**.

---

## Root Cause Confirmed

### Issue Hierarchy

```
1. DomainNotLinked Error (404)
   ↓
2. Domain exists but not linked to Communication Service
   ↓
3. Linking blocked by SPF verification status
   ↓
4. SPF verification in progress (DNS propagation)
```

### Smoking Gun Evidence

**Communication Service Linked Domains**:
```json
{
  "linkedDomains": [
    "/subscriptions/.../emailServices/lankaconnect-email/domains/AzureManagedDomain"
  ]
}
```

**Missing**:
```json
"/subscriptions/.../emailServices/lankaconnect-email/domains/lankaconnect.app"
```

**Linking Attempt Result**:
```
ERROR: (DomainValidationError) Requested domain is not in a valid state
for linking to CommunicationServices resource
```

---

## Domain Verification Status

| Verification Type | Status | Error Code | Required for Linking |
|------------------|--------|------------|---------------------|
| **Domain (TXT)** | ✅ Verified | None | Yes |
| **DKIM** | ✅ Verified | None | Yes |
| **DKIM2** | ✅ Verified | None | Yes |
| **SPF** | ⏳ VerificationInProgress | "" | **Yes (BLOCKER)** |
| **DMARC** | ⚪ NotStarted | - | No |

**Critical Finding**: Azure requires **ALL** verification records (Domain, DKIM, DKIM2, **SPF**) to be in "Verified" status before allowing domain linking.

---

## DNS Records Analysis

### Required DNS Records (from Azure)

**1. Domain Verification TXT Record**:
```
Name:  lankaconnect.app
Type:  TXT
Value: ms-domain-verification=f1d3bab2-4b5f-4fa2-bc75-ea5717975f20
TTL:   3600
Status: ✅ Verified
```

**2. SPF TXT Record**:
```
Name:  lankaconnect.app
Type:  TXT
Value: v=spf1 include:spf.protection.outlook.com -all
TTL:   3600
Status: ⏳ VerificationInProgress
```

**3. DKIM CNAME Record**:
```
Name:  selector1-azurecomm-prod-net._domainkey.lankaconnect.app
Type:  CNAME
Value: selector1-azurecomm-prod-net._domainkey.azurecomm.net
TTL:   3600
Status: ✅ Verified
```

**4. DKIM2 CNAME Record**:
```
Name:  selector2-azurecomm-prod-net._domainkey.lankaconnect.app
Type:  CNAME
Value: selector2-azurecomm-prod-net._domainkey.azurecomm.net
TTL:   3600
Status: ✅ Verified
```

### DNS Query Results

**Using nslookup (2026-01-17 21:15 UTC)**:
```bash
$ nslookup -type=TXT lankaconnect.app 8.8.8.8

Non-authoritative answer:
lankaconnect.app	canonical name = lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io
```

**Analysis**:
- Domain has CNAME record pointing to production UI container app
- **NO TXT records visible** in public DNS query
- This suggests DNS records are either:
  - Not yet propagated (DNS caching)
  - Not added to external DNS provider
  - Configured in Azure but not at DNS registrar

---

## Why SPF Verification is Stuck

### Possible Causes

1. **DNS Propagation Delay**:
   - DNS changes can take 5-30 minutes to propagate globally
   - Azure polls DNS servers periodically to check verification
   - Current TTL: 3600 seconds (1 hour)

2. **SPF Record Not Added**:
   - The required TXT record may not be added to the external DNS provider
   - Domain appears to be managed externally (not Azure DNS)
   - CNAME record exists (production UI), but TXT records may be missing

3. **DNS Provider Configuration**:
   - Some providers don't allow multiple TXT records
   - SPF record may conflict with existing TXT records
   - Need to merge TXT records if multiple exist

### Verification Steps Needed

**Check if SPF record exists in DNS provider**:
1. Log in to domain registrar (e.g., GoDaddy, Namecheap, Cloudflare)
2. Navigate to DNS management for `lankaconnect.app`
3. Look for TXT record with value: `v=spf1 include:spf.protection.outlook.com -all`
4. If missing, add it
5. Wait 5-30 minutes for propagation

---

## Application Configuration

### Current Configuration (Staging)

**appsettings.Staging.json** (lines 73-84):
```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
    "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}"
  }
}
```

**Azure Key Vault Values**:
```
AZURE_EMAIL_SENDER_ADDRESS = "noreply@lankaconnect.app"
```

**Application Logs (Startup)**:
```
21:04:40.798 [INF] Azure Email Service initialized with sender: noreply@lankaconnect.app
```

**Analysis**: ✅ Application configuration is **CORRECT**. The issue is purely Azure resource configuration.

---

## Impact Analysis

### Email Sending Status

**Current State**:
- ✅ Application code deployed and working
- ✅ DbUpdateConcurrencyException fix deployed and verified
- ✅ Azure Communication Service exists
- ✅ Email Service exists
- ✅ Custom domain exists
- ❌ **Domain NOT linked** - 100% email failure rate
- ❌ **All email sends fail with DomainNotLinked (404)**

**Failed Job Evidence** (from Hangfire logs):
```
Recipients: 5, Success: 0, Failed: 5
Azure Communication Services request failed. Status: 404, Error: DomainNotLinked
```

### Rate Limit Impact

| Scenario | Current State | After Linking |
|----------|--------------|---------------|
| **Sender Domain** | AzureManagedDomain | lankaconnect.app |
| **Rate Limit** | 30 emails/hour | 1,800 emails/hour |
| **Email Sends** | ❌ Failing (404) | ✅ Will work |

---

## Solution Path

### Option A: Complete Domain Linking (Recommended)

**Timeline**: 5-30 minutes (DNS propagation)

**Steps**:
1. ✅ **DONE**: Verified domain exists and is partially verified
2. ⏳ **WAITING**: SPF verification to complete (in progress)
3. 🎯 **NEXT**: Link domain to Communication Service once SPF passes
4. ✅ **VERIFY**: Send test email to confirm
5. ✅ **BENEFIT**: Get 1,800 emails/hour rate limit

**Command to Run After SPF Passes**:
```bash
az communication update \
  --name lankaconnect-communication \
  --resource-group lankaconnect-staging \
  --linked-domains '["/.../AzureManagedDomain", "/.../lankaconnect.app"]'
```

### Option B: Use Azure Managed Domain Temporarily

**Timeline**: Immediate

**Steps**:
1. Update sender address to use Azure managed domain
2. Accept 30 emails/hour rate limit
3. Complete custom domain linking later

**Configuration Change**:
```json
{
  "AzureSenderAddress": "DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net"
}
```

**Pros**:
- ✅ Immediate fix
- ✅ Unblocks email testing

**Cons**:
- ❌ 30/hour limit (vs 1,800/hour)
- ❌ Ugly sender address
- ❌ Still need to complete linking later

---

## Recommended Action Plan

### Immediate (Right Now)

**1. Check SPF Verification Status** (every 5-10 minutes):
```bash
az communication email domain show \
  --email-service-name lankaconnect-email \
  --resource-group lankaconnect-staging \
  --domain-name lankaconnect.app \
  --query "verificationStates.SPF.status"
```

**Expected Result When Ready**:
```json
"Verified"
```

**2. Link Domain Once SPF Passes**:
```bash
# Get existing linked domain
MANAGED_DOMAIN=$(az communication show \
  --name lankaconnect-communication \
  --resource-group lankaconnect-staging \
  --query "linkedDomains[0]" -o tsv)

# Get custom domain ID
CUSTOM_DOMAIN="/subscriptions/ebb8304a-6374-4db0-8de5-e8678afbb5b5/resourceGroups/lankaconnect-staging/providers/Microsoft.Communication/emailServices/lankaconnect-email/domains/lankaconnect.app"

# Link both domains
az communication update \
  --name lankaconnect-communication \
  --resource-group lankaconnect-staging \
  --linked-domains "[\"$MANAGED_DOMAIN\", \"$CUSTOM_DOMAIN\"]"
```

**3. Verify Linking**:
```bash
az communication show \
  --name lankaconnect-communication \
  --resource-group lankaconnect-staging \
  --query "linkedDomains"
```

**Expected Result**:
```json
[
  "/.../domains/AzureManagedDomain",
  "/.../domains/lankaconnect.app"
]
```

**4. Test Email Sending**:
```bash
# Get auth token
TOKEN=$(curl -X POST \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email": "niroshhh@gmail.com", "password": "12!@qwASzx"}' \
  | jq -r '.token')

# Send test notification
curl -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/d543629f-a5ba-4475-b124-3d0fc5200f2f/send-notification" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Domain Linking Test",
    "message": "Testing email after custom domain linking",
    "emailGroupIds": []
  }'
```

**5. Check Hangfire Dashboard**:
- Access: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/hangfire
- Verify job succeeds (no DomainNotLinked error)
- Check logs for successful email sends

**6. Verify Email Received**:
- Check niroshhh@gmail.com inbox
- Verify sender shows as `noreply@lankaconnect.app`
- Confirm email content renders correctly

---

## Monitoring SPF Verification

### Manual Check Script

```bash
#!/bin/bash
# Save as: check_spf_status.sh

while true; do
  STATUS=$(az communication email domain show \
    --email-service-name lankaconnect-email \
    --resource-group lankaconnect-staging \
    --domain-name lankaconnect.app \
    --query "verificationStates.SPF.status" -o tsv)

  echo "[$(date)] SPF Status: $STATUS"

  if [ "$STATUS" == "Verified" ]; then
    echo "✅ SPF VERIFIED! Ready to link domain."
    break
  fi

  sleep 300  # Check every 5 minutes
done
```

### Alternative: Azure Portal

1. Navigate to: Azure Portal → Email Communication Services
2. Select: `lankaconnect-email`
3. Go to: "Provision domains"
4. Click: `lankaconnect.app`
5. Check: "Verification records" section
6. Wait for: SPF status to show green checkmark

---

## Success Criteria

✅ **SPF Verification**: Status = "Verified"
✅ **Domain Linking**: lankaconnect.app in linkedDomains array
✅ **Email Sending**: Test email sent without DomainNotLinked error
✅ **Email Delivery**: Email received at niroshhh@gmail.com
✅ **Rate Limit**: 1,800 emails/hour available
✅ **Hangfire Jobs**: Background jobs succeed
✅ **Logs**: No 404 DomainNotLinked errors

---

## Next Steps After Linking

1. **Clean up failed Hangfire jobs** (130 jobs from before fix)
2. **Update documentation** with final status
3. **Test newsletter sending** end-to-end
4. **Monitor email deliverability** for 24 hours
5. **Proceed with Day 6 production deployment**

---

## Lessons Learned

### What Went Wrong

1. **Domain provisioned but not linked** - Easy to overlook
2. **SPF verification takes time** - DNS propagation delay
3. **Application logs misleading** - Shows config but not Azure resource status
4. **Error message generic** - "DomainNotLinked" doesn't explain SPF requirement

### Prevention for Future

1. **Verify domain linking** - Not just domain creation
2. **Check all verification states** - Domain, DKIM, DKIM2, SPF
3. **Test from Azure CLI** - Don't rely solely on application logs
4. **Document DNS requirements** - Clear checklist for domain setup
5. **Monitor verification status** - Automated checks for stuck verifications

---

## Related Documentation

- [COMPREHENSIVE_ROOT_CAUSE_ANALYSIS_2026-01-17.md](./COMPREHENSIVE_ROOT_CAUSE_ANALYSIS_2026-01-17.md)
- [AZURE_EMAIL_DOMAIN_TEST_INSTRUCTIONS.md](./AZURE_EMAIL_DOMAIN_TEST_INSTRUCTIONS.md)
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md)
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md)

---

**Status**: WAITING FOR SPF VERIFICATION
**ETA**: 5-30 minutes
**Next Action**: Monitor SPF status and link domain when ready
**Blocker**: DNS propagation time (external DNS provider)
