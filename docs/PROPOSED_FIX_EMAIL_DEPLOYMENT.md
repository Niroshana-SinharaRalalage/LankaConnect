# Proposed Fix: Email Deployment Configuration

## Date: 2025-12-19
## Related RCA: RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md

---

## Fix Overview

Update GitHub Actions workflows to use Azure Communication Services instead of SMTP for deployed environments.

---

## Prerequisites

### 1. Azure Communication Services Must Be Provisioned

**Check if resource exists:**
```bash
az resource list --resource-type "Microsoft.Communication/CommunicationServices" --query "[].{name:name, resourceGroup:resourceGroup}" -o table
```

**If not exists, create one:**
```bash
# Create Azure Communication Services resource
az communication create \
  --name lankaconnect-email \
  --resource-group lankaconnect-staging \
  --data-location unitedstates \
  --location global

# Get connection string
az communication list-key \
  --name lankaconnect-email \
  --resource-group lankaconnect-staging \
  --query primaryConnectionString -o tsv
```

### 2. Email Domain Verification

**Required steps:**
1. Add custom domain or use Azure-provided domain
2. Verify domain ownership (DNS records)
3. Create verified sender address (e.g., noreply@lankaconnect.com)

**Azure Portal steps:**
1. Navigate to Communication Services resource
2. Email → Domains → Add domain
3. Follow DNS verification process
4. Configure sender addresses

### 3. Add Secrets to Key Vault

**Once you have the connection string and sender address:**

```bash
# Add Azure Email Connection String to staging Key Vault
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-CONNECTION-STRING \
  --value "endpoint=https://lankaconnect-email.communication.azure.com/;accesskey=<YOUR_KEY>"

# Add Azure Email Sender Address to staging Key Vault
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-SENDER-ADDRESS \
  --value "DoNotReply@<your-verified-domain>.azurecomm.net"

# Repeat for production Key Vault (if different)
az keyvault secret set \
  --vault-name lankaconnect-production-kv \
  --name AZURE-EMAIL-CONNECTION-STRING \
  --value "endpoint=https://lankaconnect-email.communication.azure.com/;accesskey=<YOUR_KEY>"

az keyvault secret set \
  --vault-name lankaconnect-production-kv \
  --name AZURE-EMAIL-SENDER-ADDRESS \
  --value "DoNotReply@<your-verified-domain>.azurecomm.net"
```

---

## Workflow Changes

### File: `.github/workflows/deploy-staging.yml`

**Lines to change: 144-152**

**BEFORE (Current - WRONG):**
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

**AFTER (Proposed - CORRECT):**
```yaml
EmailSettings__Provider=Azure \
EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
EmailSettings__SenderName="LankaConnect Staging" \
EmailSettings__TemplateBasePath=Templates/Email \
```

### File: `.github/workflows/deploy-production.yml` (if exists)

**Same change - update email provider from SMTP to Azure**

---

## Implementation Steps

### Phase 1: Preparation (15-30 minutes)

1. **Verify Azure Communication Services exists**
   ```bash
   az resource list --resource-type "Microsoft.Communication/CommunicationServices"
   ```

2. **Get connection string**
   ```bash
   az communication list-key --name <resource-name> --resource-group <rg-name>
   ```

3. **Verify email domain is configured and verified**
   - Check Azure Portal → Communication Services → Email → Domains
   - Ensure at least one verified sender address exists

4. **Add secrets to Key Vault** (see commands above)

5. **Verify secrets were added**
   ```bash
   az keyvault secret show --vault-name lankaconnect-staging-kv --name AZURE-EMAIL-CONNECTION-STRING --query value -o tsv
   az keyvault secret show --vault-name lankaconnect-staging-kv --name AZURE-EMAIL-SENDER-ADDRESS --query value -o tsv
   ```

### Phase 2: Update Workflows (5 minutes)

1. **Update `deploy-staging.yml`**
   - Change lines 144-152 as shown above
   - Remove SMTP-related environment variables
   - Add Azure-related environment variables

2. **Update `deploy-production.yml` (if exists)**
   - Same changes as staging
   - Use production Key Vault secrets

3. **Commit changes**
   ```bash
   git add .github/workflows/deploy-staging.yml
   git add .github/workflows/deploy-production.yml  # if exists
   git commit -m "fix: Use Azure Communication Services instead of SMTP for email"
   git push origin develop
   ```

### Phase 3: Deployment & Testing (15 minutes)

1. **Trigger staging deployment**
   - Push to develop branch triggers automatic deployment
   - Or manually trigger via GitHub Actions UI

2. **Monitor deployment**
   ```bash
   # Watch deployment logs
   gh run watch

   # Check container app logs after deployment
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --tail 100
   ```

3. **Test email functionality**
   ```bash
   # Test user registration (triggers verification email)
   curl -X POST https://<staging-url>/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test123!@#",
       "firstName": "Test",
       "lastName": "User"
     }'

   # Check logs for email send confirmation
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --follow
   ```

4. **Verify email received** (check test email inbox)

### Phase 4: Production Deployment (if applicable)

1. **Merge to master/main** (after staging verification)
2. **Trigger production deployment**
3. **Verify production email functionality**

---

## Validation Checklist

### Pre-Deployment
- [ ] Azure Communication Services resource exists
- [ ] Email domain verified in Azure
- [ ] Sender address created and verified
- [ ] Connection string obtained
- [ ] Secrets added to Key Vault (staging)
- [ ] Secrets added to Key Vault (production)
- [ ] Secrets verified readable from Key Vault
- [ ] Workflow files updated with Azure provider
- [ ] Changes committed and pushed

### Post-Deployment (Staging)
- [ ] Deployment completed successfully
- [ ] Container app shows no errors in logs
- [ ] Health endpoint responding
- [ ] Email provider shows as "Azure" in logs
- [ ] Test registration email sent successfully
- [ ] Test registration email received
- [ ] Password reset email works
- [ ] Event notification email works

### Post-Deployment (Production)
- [ ] Same checks as staging
- [ ] Real user emails working
- [ ] No errors in production logs
- [ ] Monitoring alerts not triggered

---

## Rollback Plan

### If Email Fails After Deployment

**Option 1: Quick rollback to SMTP (temporary fix)**
```bash
# Update Container App environment variables manually
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

**Option 2: Revert workflow changes**
```bash
git revert <commit-hash>
git push origin develop
# Wait for automatic deployment
```

**Option 3: Rollback container image**
```bash
# Find previous working image
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging

# Activate previous revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision <previous-revision-name>
```

---

## Alternative Approaches

### Alternative 1: Minimal Override Pattern

**Only override secrets, let appsettings.json define provider:**

```yaml
# In workflow - only set connection details
EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
```

**Pros:**
- Less duplication
- Configuration lives in code (appsettings.json)
- Environment variables only for secrets

**Cons:**
- Less explicit
- Harder to debug (have to check multiple sources)

### Alternative 2: Hybrid with SMTP Fallback

**Keep both providers configured:**

```yaml
EmailSettings__Provider=Azure \
EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
EmailSettings__SmtpServer=secretref:smtp-host \
EmailSettings__SmtpPort=secretref:smtp-port \
EmailSettings__Username=secretref:smtp-username \
EmailSettings__Password=secretref:smtp-password \
EmailSettings__SenderName="LankaConnect Staging" \
EmailSettings__TemplateBasePath=Templates/Email \
```

**Pros:**
- Automatic fallback if Azure fails
- Easier disaster recovery

**Cons:**
- More complex configuration
- Need fallback logic in EmailService
- Higher maintenance burden

**Recommendation**: Use primary proposal (Azure only). Add SMTP fallback only if required by business continuity requirements.

---

## Testing Strategy

### Unit Tests (No changes needed)
- Existing email service tests cover both providers
- Mock IEmailService in integration tests

### Integration Tests (Manual)

**Test 1: User Registration**
```bash
POST /api/auth/register
Expected: Verification email sent via Azure Communication Services
Verify: Email received, Azure logs show send confirmation
```

**Test 2: Password Reset**
```bash
POST /api/auth/forgot-password
Expected: Reset email sent via Azure Communication Services
Verify: Email received with reset link
```

**Test 3: Event Notification**
```bash
POST /api/events/{id}/signup
Expected: Confirmation email sent to user
Verify: Email received, event details correct
```

### Monitoring

**Add Application Insights alerts:**
```bash
# Alert on email send failures
az monitor metrics alert create \
  --name email-send-failures \
  --resource-group lankaconnect-staging \
  --scopes <app-insights-resource-id> \
  --condition "count customMetrics/EmailSendFailure > 5" \
  --window-size 5m \
  --evaluation-frequency 1m
```

---

## Documentation Updates

### Files to Update

1. **README.md** (if email setup documented)
   - Update email provider from SMTP to Azure
   - Add Azure Communication Services setup instructions

2. **docs/DEPLOYMENT.md** (if exists)
   - Update deployment configuration section
   - Document required Key Vault secrets

3. **Architecture Decision Record** (create new)
   - Document decision to use Azure Communication Services
   - Explain why over SMTP
   - Link to this RCA

---

## Cost Considerations

**Azure Communication Services Email Pricing:**
- Free tier: 500 emails/month
- Paid tier: $0.00025 per email (0.25 USD per 1000 emails)

**vs SMTP Hosting:**
- Varies by provider
- Typically $10-50/month for basic plans

**Recommendation**: Azure Communication Services is cost-effective for low-medium volume.

---

## Security Considerations

### Secrets Management
- Connection string has full access to Communication Services resource
- Store in Key Vault (already done)
- Use Container App system-assigned identity for Key Vault access
- Rotate keys periodically

### Email Security
- SPF/DKIM configured by Azure automatically
- Sender domain verification required
- Rate limiting handled by Azure
- Monitor for abuse/spam complaints

---

## Success Criteria

**Deployment is successful when:**
1. Workflow deploys without errors
2. Container App starts successfully
3. Email provider initialized as "Azure" (check logs)
4. User registration sends verification email
5. Password reset sends email
6. Event notifications send emails
7. All emails received within 2 minutes
8. No errors in Application Insights

---

## Contact & Support

**Azure Communication Services:**
- Documentation: https://learn.microsoft.com/azure/communication-services/
- Support: Azure Portal → Support Tickets

**GitHub Actions:**
- Workflow logs: https://github.com/{org}/{repo}/actions

**Key Vault:**
- Access policies: Verify Container App identity has Get/List on secrets

---

## Appendix: Useful Commands

### Debug Email Issues

```bash
# Check Container App environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' -o table

# Check Key Vault access
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name AZURE-EMAIL-CONNECTION-STRING

# Check Communication Services resource
az communication list \
  --query "[].{name:name, resourceGroup:resourceGroup, provisioningState:provisioningState}"

# Check email domain verification
az communication email domain list \
  --communication-service-name lankaconnect-email \
  --resource-group lankaconnect-staging

# View recent email sends (if logging enabled)
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where message contains 'Email sent' | take 20"
```

### Verify Configuration

```bash
# Check what provider is actually being used
curl https://<staging-url>/api/health | jq

# Get logs showing email provider initialization
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 | grep -i email
```

---

## Timeline Estimate

**Total Time: 1-2 hours** (assuming Azure Communication Services already provisioned)

| Phase | Duration | Notes |
|-------|----------|-------|
| Preparation | 15-30 min | Get secrets, add to Key Vault |
| Update Workflows | 5 min | Code changes |
| Deployment | 10 min | Automatic via GitHub Actions |
| Testing | 20-30 min | Manual verification |
| Monitoring | 15 min | Check logs, alerts |
| Documentation | 15 min | Update relevant docs |

**If Azure Communication Services needs provisioning: Add 1-2 days for domain verification**

---

## Sign-off

**Fix Proposed By**: System Architect Agent
**Date**: 2025-12-19
**Risk Level**: LOW (configuration-only change, easy rollback)
**Testing Required**: Manual integration testing

**Approval Required From**:
- [ ] Infrastructure Team (Azure resources ready)
- [ ] DevOps Team (Key Vault secrets created)
- [ ] Development Team (Review workflow changes)
- [ ] QA Team (Test plan approved)
