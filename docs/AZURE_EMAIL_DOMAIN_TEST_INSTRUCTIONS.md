# Azure Email Domain Manual Test Instructions

## Objective
Verify that the custom domain `lankaconnect.app` is properly configured and linked to Azure Communication Services by sending a test email from the Azure Portal.

## Prerequisites
- Access to Azure Portal with appropriate permissions
- Azure Communication Services resource: `lankaconnect-communication`

## Step-by-Step Instructions

### Step 1: Navigate to Azure Communication Services
1. Log in to Azure Portal: https://portal.azure.com
2. Search for "Communication Services" in the top search bar
3. Select your Communication Services resource: `lankaconnect-communication`

### Step 2: Locate Email Service
1. In the left sidebar under "Email", click on **"Email"** or **"Domains"**
2. You should see your custom domain: `lankaconnect.app`
3. Check the domain status - it should show as **"Verified"** with green checkmarks for:
   - Domain Status
   - SPF
   - DKIM
   - DKIM2

### Step 3: Send Test Email
1. In the Communication Services resource, look for **"Try Email"** or **"Send Email"** option
2. If not available in the portal, you can use Azure CLI:

```bash
# Send test email using Azure CLI
az communication email send \
  --connection-string "endpoint=https://lankaconnect-communication.unitedstates.communication.azure.com/;accesskey=YOUR_ACCESS_KEY" \
  --sender "noreply@lankaconnect.app" \
  --subject "Azure Domain Test - Manual Verification" \
  --to "niroshhh@gmail.com" \
  --text "This is a test email to verify the custom domain lankaconnect.app is properly linked to Azure Communication Services."
```

### Step 4: Alternative - Use Postman/curl
If CLI is not available, use the Azure Communication Services REST API directly:

**Endpoint:**
```
POST https://lankaconnect-communication.unitedstates.communication.azure.com/emails:send?api-version=2023-03-31
```

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_ACCESS_TOKEN
```

**Body:**
```json
{
  "senderAddress": "noreply@lankaconnect.app",
  "recipients": {
    "to": [
      {
        "address": "niroshhh@gmail.com"
      }
    ]
  },
  "content": {
    "subject": "Azure Domain Test - Manual Verification",
    "plainText": "This is a test email to verify the custom domain lankaconnect.app is properly linked to Azure Communication Services."
  }
}
```

## Expected Results

### Scenario A: Email Sends Successfully ✅
**What it means:**
- Domain is properly linked to Azure Communication Services
- Problem is with Container App using stale environment variables
- **Next Action:** Redeploy the API to refresh environment variables

### Scenario B: DomainNotLinked Error (404) ❌
**What it means:**
- Domain is provisioned but NOT linked to the Communication Services resource
- DNS may be configured, but linking step was not completed
- **Next Action:** Complete domain linking process in Azure Portal

**Error Message:**
```json
{
  "error": {
    "code": "DomainNotLinked",
    "message": "The specified sender domain has not been linked."
  }
}
```

### Scenario C: Other Error
Document the exact error message and status code for further investigation.

## Verification Checklist

After sending the test email, check:

- [ ] Email sent without errors (Status 200/202)
- [ ] Email received in niroshhh@gmail.com inbox
- [ ] Email sender shows as `noreply@lankaconnect.app`
- [ ] No DomainNotLinked error
- [ ] No authentication errors
- [ ] Email not in spam folder

## Domain Verification Status Check

In Azure Portal, verify these DNS records are configured:

1. **SPF Record**
   ```
   Type: TXT
   Name: @
   Value: v=spf1 include:spf.protection.outlook.com -all
   ```

2. **DKIM Record**
   ```
   Type: CNAME
   Name: selector1-lankaconnect-app._domainkey
   Value: selector1-lankaconnect-app._domainkey.lankaconnectapp.onmicrosoft.com
   ```

3. **DKIM2 Record**
   ```
   Type: CNAME
   Name: selector2-lankaconnect-app._domainkey
   Value: selector2-lankaconnect-app._domainkey.lankaconnectapp.onmicrosoft.com
   ```

## Next Steps Based on Results

### If Test Succeeds:
1. Proceed with Container App redeployment to refresh environment variables
2. Trigger workflow: `deploy-staging.yml`
3. Monitor logs for email sending
4. Verify emails are received

### If Test Fails with DomainNotLinked:
1. In Azure Portal, navigate to Email Communication Service
2. Go to "Provision Domains"
3. Select `lankaconnect.app`
4. Click "Connect" or "Link Domain"
5. Follow the linking wizard
6. Wait for verification (can take 15-30 minutes)
7. Retry test email

### If Test Fails with Other Error:
Document the error and analyze based on:
- Authentication issues → Check connection string and access keys
- DNS issues → Verify DNS propagation (use nslookup)
- Geographic mismatch → Verify Email Service and Communication Service are in same region

## Documentation
After completing the test, update:
- This document with actual results
- [COMPREHENSIVE_ROOT_CAUSE_ANALYSIS_2026-01-17.md](./COMPREHENSIVE_ROOT_CAUSE_ANALYSIS_2026-01-17.md) with findings
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) with status

---

**Created:** 2026-01-17
**Purpose:** Manual verification of Azure custom domain configuration
**Related Issues:** DomainNotLinked error in staging environment
**Related Commits:** f24dbffb (concurrency fix)
