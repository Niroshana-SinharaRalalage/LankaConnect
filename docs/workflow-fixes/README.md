# Workflow Fix Files

This directory contains fix patches for the email deployment configuration issue.

## Files

- `deploy-staging.yml.patch` - Patch file showing exact changes needed for staging workflow
- `deploy-production.yml.patch` - Patch file showing exact changes needed for production workflow (if exists)
- `apply-fixes.sh` - Shell script to apply all fixes automatically
- `verify-secrets.sh` - Shell script to verify required secrets exist in Key Vault

## Prerequisites

Before applying these fixes, ensure:

1. Azure Communication Services resource exists
2. Email domain is verified in Azure
3. Secrets added to Key Vault:
   - `AZURE-EMAIL-CONNECTION-STRING`
   - `AZURE-EMAIL-SENDER-ADDRESS`

## How to Apply

### Option 1: Manual (Recommended for review)

1. Review the patch file: `deploy-staging.yml.patch`
2. Open `.github/workflows/deploy-staging.yml`
3. Find lines 144-152 (email configuration)
4. Replace SMTP configuration with Azure configuration as shown in patch
5. Commit changes
6. Push to trigger deployment

### Option 2: Automatic (Using patch command)

```bash
# From repository root
patch .github/workflows/deploy-staging.yml < docs/workflow-fixes/deploy-staging.yml.patch
git add .github/workflows/deploy-staging.yml
git commit -m "fix: Use Azure Communication Services for email"
git push origin develop
```

### Option 3: Using provided script

```bash
cd docs/workflow-fixes
chmod +x apply-fixes.sh
./apply-fixes.sh
```

## Verification

After deployment, verify:

```bash
# Check container app environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env' -o table

# Should show:
# EmailSettings__Provider = Azure
# EmailSettings__AzureConnectionString = secretref:azure-email-connection-string
# EmailSettings__AzureSenderAddress = secretref:azure-email-sender-address
```

## Rollback

If issues occur:

```bash
# Revert the commit
git revert HEAD
git push origin develop
```

Or manually restore SMTP configuration from git history.

## Related Documentation

- [RCA: Email Deployment Configuration](../RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md)
- [Proposed Fix: Email Deployment](../PROPOSED_FIX_EMAIL_DEPLOYMENT.md)
