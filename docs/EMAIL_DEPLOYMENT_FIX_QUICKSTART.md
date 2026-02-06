# Email Deployment Fix - Quick Start Guide

## TL;DR

**Problem**: Workflow uses SMTP, but appsettings.json says Azure.
**Solution**: Update workflow to match appsettings.json.

---

## 5-Minute Fix (If Secrets Already Exist)

```bash
# 1. Verify secrets exist
./docs/workflow-fixes/verify-secrets.sh staging

# 2. Apply fix (dry-run first)
./docs/workflow-fixes/apply-fixes.sh --dry-run --environment staging

# 3. Apply fix for real
./docs/workflow-fixes/apply-fixes.sh --environment staging

# 4. Review changes
git diff .github/workflows/deploy-staging.yml

# 5. Commit and deploy
git add .github/workflows/deploy-staging.yml
git commit -m "fix: Use Azure Communication Services for email"
git push origin develop

# 6. Monitor deployment
gh run watch

# 7. Test email
curl -X POST https://<staging-url>/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!@#","firstName":"Test","lastName":"User"}'
```

---

## If Secrets Don't Exist

### Step 1: Check Azure Communication Services

```bash
# Does resource exist?
az resource list --resource-type "Microsoft.Communication/CommunicationServices"
```

**If NO resource exists**: Contact infrastructure team to provision Azure Communication Services.

**If resource exists**: Continue to Step 2.

### Step 2: Get Connection String

```bash
# List Communication Services resources
az communication list --query "[].{name:name, rg:resourceGroup}"

# Get connection string (replace <name> and <rg>)
az communication list-key \
  --name <communication-service-name> \
  --resource-group <resource-group> \
  --query primaryConnectionString -o tsv
```

### Step 3: Get Sender Address

**From Azure Portal:**
1. Navigate to Communication Services resource
2. Click "Email" → "Domains"
3. Ensure domain is "Verified"
4. Click "MailFrom addresses"
5. Copy sender address (e.g., DoNotReply@xxx.azurecomm.net)

**Or use Azure-provided domain:**
- Format: `DoNotReply@<guid>.azurecomm.net`
- No custom domain verification needed
- Available immediately after resource creation

### Step 4: Add Secrets to Key Vault

```bash
# Staging
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-CONNECTION-STRING \
  --value "endpoint=https://xxx.communication.azure.com/;accesskey=xxx"

az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-SENDER-ADDRESS \
  --value "DoNotReply@xxx.azurecomm.net"

# Production (if different)
az keyvault secret set \
  --vault-name lankaconnect-production-kv \
  --name AZURE-EMAIL-CONNECTION-STRING \
  --value "endpoint=https://xxx.communication.azure.com/;accesskey=xxx"

az keyvault secret set \
  --vault-name lankaconnect-production-kv \
  --name AZURE-EMAIL-SENDER-ADDRESS \
  --value "DoNotReply@xxx.azurecomm.net"
```

### Step 5: Proceed with 5-Minute Fix Above

---

## Manual Fix (Without Scripts)

### Edit `.github/workflows/deploy-staging.yml`

**Find lines 144-152** (email configuration section)

**Replace:**
```yaml
EmailSettings__Provider=Smtp \
EmailSettings__SmtpServer=secretref:smtp-host \
EmailSettings__SmtpPort=secretref:smtp-port \
EmailSettings__Username=secretref:smtp-username \
EmailSettings__Password=secretref:smtp-password \
EmailSettings__SenderEmail=secretref:email-from-address \
EmailSettings__SenderName="LankaConnect Staging" \
EmailSettings__EnableSsl=true \
EmailSettings__TemplateBasePath=Templates/Email \
```

**With:**
```yaml
EmailSettings__Provider=Azure \
EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
EmailSettings__SenderName="LankaConnect Staging" \
EmailSettings__TemplateBasePath=Templates/Email \
```

**Repeat for `.github/workflows/deploy-production.yml` if it exists.**

---

## Verification Commands

### Before Deployment

```bash
# Check secrets exist
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-CONNECTION-STRING \
  --query value -o tsv

az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-SENDER-ADDRESS \
  --query value -o tsv

# Should show values, not errors
```

### After Deployment

```bash
# Check container app environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' \
  | grep -i email

# Should show:
# EmailSettings__Provider = Azure
# EmailSettings__AzureConnectionString = secretref:azure-email-connection-string
# EmailSettings__AzureSenderAddress = secretref:azure-email-sender-address

# Check logs for email provider initialization
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 \
  | grep -i email
```

### Test Email Functionality

```bash
# Test user registration (triggers verification email)
curl -X POST https://lankaconnect-api-staging.xxxxxx.azurecontainerapps.io/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "firstName": "Test",
    "lastName": "User"
  }'

# Check if email was sent (check logs)
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow

# Look for log entries like:
# "Email sent successfully via Azure Communication Services"
```

---

## Troubleshooting

### Issue: Secret not found

```bash
# Error: Secret AZURE-EMAIL-CONNECTION-STRING not found

# Solution: Add the secret
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-CONNECTION-STRING \
  --value "<your-connection-string>"
```

### Issue: Invalid connection string

```bash
# Error: Unable to parse connection string

# Check format - must be:
endpoint=https://xxx.communication.azure.com/;accesskey=xxx
```

### Issue: Sender address not verified

```bash
# Error: Sender address not verified

# Solution: Verify domain in Azure Portal
# Or use Azure-provided domain (no verification needed)
```

### Issue: Email not received

**Check logs:**
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 | grep -i email
```

**Common causes:**
1. Email went to spam/junk folder
2. Sender domain not verified
3. Rate limiting (Azure free tier: 500/month)
4. Invalid recipient email address

---

## Rollback (If Something Goes Wrong)

### Option 1: Revert Git Commit

```bash
git revert HEAD
git push origin develop
# Wait for automatic deployment
```

### Option 2: Quick Manual Rollback

```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    EmailSettings__Provider=Smtp \
    EmailSettings__SmtpServer=secretref:smtp-host \
    EmailSettings__SmtpPort=secretref:smtp-port \
    EmailSettings__Username=secretref:smtp-username \
    EmailSettings__Password=secretref:smtp-password
```

### Option 3: Restore Previous Container Revision

```bash
# List revisions
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "[].{name:name, createdTime:properties.createdTime, active:properties.active}" \
  -o table

# Activate previous revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision <previous-revision-name>
```

---

## Key Vault Secret Names Reference

### Required for Azure Communication Services

| Secret Name | Example Value | Where to Get |
|-------------|---------------|--------------|
| `AZURE-EMAIL-CONNECTION-STRING` | `endpoint=https://xxx.communication.azure.com/;accesskey=xxx` | `az communication list-key` |
| `AZURE-EMAIL-SENDER-ADDRESS` | `DoNotReply@xxx.azurecomm.net` | Azure Portal → Communication Services → Email → Domains |

### Optional (SMTP - for fallback)

| Secret Name | Example Value | Notes |
|-------------|---------------|-------|
| `SMTP-HOST` | `smtp.gmail.com` | Not needed if using Azure only |
| `SMTP-PORT` | `587` | Not needed if using Azure only |
| `SMTP-USERNAME` | `user@example.com` | Not needed if using Azure only |
| `SMTP-PASSWORD` | `password123` | Not needed if using Azure only |
| `EMAIL-FROM-ADDRESS` | `noreply@example.com` | Not needed if using Azure only |

---

## Questions?

**For Azure Communication Services setup:**
- See: [PROPOSED_FIX_EMAIL_DEPLOYMENT.md](./PROPOSED_FIX_EMAIL_DEPLOYMENT.md)

**For root cause analysis:**
- See: [RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md](./RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md)

**For detailed workflow changes:**
- See: [workflow-fixes/README.md](./workflow-fixes/README.md)

**For infrastructure questions:**
- Contact: Infrastructure team for Azure Communication Services provisioning
- Contact: DevOps team for Key Vault access

---

## Success Checklist

After completing the fix, verify:

- [ ] Secrets exist in Key Vault (run `verify-secrets.sh`)
- [ ] Workflow updated with Azure provider
- [ ] Changes committed and pushed
- [ ] Deployment completed successfully
- [ ] Container app shows no errors in logs
- [ ] Email provider shows as "Azure" in logs
- [ ] Test registration email sent and received
- [ ] Password reset email sent and received
- [ ] Event notification email sent and received
- [ ] No errors in Application Insights

**If all checked**: Fix complete!

**If any unchecked**: Review troubleshooting section or contact team.
