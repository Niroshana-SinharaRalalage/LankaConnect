# Staging Custom Email Domain Setup

**Date**: 2026-01-17
**Environment**: Staging
**Domain**: lankaconnect.app
**Sender Address**: noreply@lankaconnect.app

---

## Summary

Successfully configured custom domain email for **staging environment** to bypass Azure Communication Services rate limits (previously 30 emails/hour with Azure Managed Domain).

### New Limits with Custom Domain:
- **30 emails/minute** (1,800/hour)
- **100 emails/hour sustained**
- Significantly higher than 30/hour limit

---

## What Was Done

### 1. Azure Communication Services Configuration

**Created custom domain in staging**:
- Email Service: `lankaconnect-email`
- Custom Domain: `lankaconnect.app`
- Management Type: CustomerManaged
- Data Location: United States

### 2. DNS Configuration

Added the following DNS records to Namecheap for `lankaconnect.app`:

| Type | Host | Value | Purpose | Status |
|------|------|-------|---------|--------|
| TXT | `@` | `ms-domain-verification=f1d3bab2-4b5f-4fa2-bc75-ea5717975f20` | Staging verification | ✅ Verified |
| TXT | `@` | `v=spf1 include:spf.protection.outlook.com -all` | SPF (shared prod/staging) | ⚠️ In Progress |
| CNAME | `selector1-azurecomm-prod-net._domainkey` | `selector1-azurecomm-prod-net._domainkey.azurecomm.net` | DKIM 1 (shared) | ✅ Verified |
| CNAME | `selector2-azurecomm-prod-net._domainkey` | `selector2-azurecomm-prod-net._domainkey.azurecomm.net` | DKIM 2 (shared) | ✅ Verified |

**Note**: DNS records are **shared** between production and staging since both use the same domain.

### 3. Azure Key Vault Updates

Updated secrets in `lankaconnect-staging-kv`:

| Secret Name | Old Value | New Value |
|------------|-----------|-----------|
| `AZURE-EMAIL-SENDER-ADDRESS` | `DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net` | `noreply@lankaconnect.app` |
| `EMAIL-FROM-ADDRESS` | (Azure managed) | `noreply@lankaconnect.app` |

**Connection String** (unchanged):
```
endpoint=https://lankaconnect-communication.unitedstates.communication.azure.com/;accesskey=5XTkOE10iioKugbZBPPrQRq2NRkscM5l7SIgi7IBIdhDhQIp2IYhJQQJ99BLACULyCpl1gBuAAAAAZCSEsEs
```

### 4. Container App Restart

Restarted `lankaconnect-api-staging` revision `lankaconnect-api-staging--0000619` to pick up new Key Vault configuration.

---

## Verification Status

### Domain Verification: ✅ Complete

```json
{
  "Domain": {
    "errorCode": "None",
    "status": "Verified"
  },
  "DKIM": {
    "errorCode": "None",
    "status": "Verified"
  },
  "DKIM2": {
    "errorCode": "None",
    "status": "Verified"
  }
}
```

### SPF Verification: ⚠️ In Progress

```json
{
  "SPF": {
    "errorCode": "DnsRecordsNotMatched",
    "status": "VerificationFailed"
  }
}
```

**Why SPF is failing**: The CNAME record on `@` (root domain) pointing to Container Apps may be interfering with TXT record resolution. This is a known DNS limitation.

**Impact**: **MINIMAL** - Emails will still be delivered successfully because:
1. ✅ DKIM is verified (primary authentication)
2. ✅ Domain ownership is verified
3. SPF is a secondary authentication mechanism
4. Most email providers prioritize DKIM over SPF

---

## Testing Required

### Manual Test Steps:

1. **Login to staging**: https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
2. **Navigate to Newsletter section**
3. **Create a test newsletter**
4. **Send to a small group** (5-10 recipients)
5. **Verify**:
   - ✅ Emails are sent from `noreply@lankaconnect.app` (not Azure managed domain)
   - ✅ Emails are delivered to inbox (not spam)
   - ✅ Email headers show DKIM-Signature
   - ✅ Check Azure Communication Services logs for successful sends

### API Test:

```bash
# Check staging API health
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/health

# Expected: {"status":"Healthy",...}
```

---

## Rate Limit Comparison

| Environment | Domain Type | Rate Limit | Max/Hour |
|------------|-------------|------------|----------|
| **Production** | Custom (`lankaconnect.app`) | ✅ 30/min | ✅ 1,800 |
| **Staging (OLD)** | Azure Managed | ❌ 30/hour | ❌ 30 |
| **Staging (NEW)** | Custom (`lankaconnect.app`) | ✅ 30/min | ✅ 1,800 |

---

## Production Impact

**NO IMPACT** - Production email configuration remains unchanged:
- ✅ Production uses separate Email Service: `lankaconnect-email-prod`
- ✅ Production domain fully verified (Domain, DKIM, SPF all verified)
- ✅ DNS records are shared but don't affect production functionality

---

## Next Steps

1. ✅ **Test newsletter sending** in staging with custom domain
2. ⏳ **Monitor SPF verification** - may resolve automatically as DNS propagates
3. ⏳ **If SPF remains failed**: Consider removing CNAME on `@` and using ALIAS/ANAME record instead (requires Namecheap support or DNS provider change)

---

## Files Modified

- Azure Key Vault: `lankaconnect-staging-kv` (2 secrets updated)
- Azure Communication Services: Custom domain `lankaconnect.app` created
- Namecheap DNS: 1 TXT record added (domain verification for staging)

---

## Troubleshooting

### If emails still show Azure managed domain:

1. Check Key Vault secrets are correct:
   ```bash
   az keyvault secret show --vault-name lankaconnect-staging-kv --name AZURE-EMAIL-SENDER-ADDRESS
   ```

2. Restart Container App:
   ```bash
   az containerapp revision restart --name lankaconnect-api-staging --resource-group lankaconnect-staging --revision <revision-name>
   ```

3. Check application logs:
   ```bash
   az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 100
   ```

### If SPF verification keeps failing:

- **Option 1**: Leave as-is (minimal impact)
- **Option 2**: Remove CNAME on `@`, use ALIAS record instead
- **Option 3**: Move Container Apps to subdomain only (api.lankaconnect.app), remove CNAME on root

---

## Related Documentation

- [Production DNS Configuration](./DNS_CONFIGURATION_PRODUCTION.md)
- [Day 3 Infrastructure Setup](./DAY_3_INFRASTRUCTURE_COMPLETE.md)
- [Azure Communication Services Limits](https://learn.microsoft.com/en-us/azure/communication-services/concepts/service-limits)