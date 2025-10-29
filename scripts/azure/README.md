# Azure Deployment Scripts

This directory contains automation scripts for provisioning LankaConnect infrastructure on Azure.

## Available Scripts

### `provision-staging.sh`

**Purpose:** Automated provisioning of complete staging environment

**What it creates:**
- Resource Group
- Azure Container Registry (Basic tier)
- PostgreSQL Flexible Server (Burstable B1ms)
- Azure Key Vault with 14 secrets
- Container Apps Environment
- Container App with Managed Identity
- Log Analytics Workspace
- Service Principal for GitHub Actions

**Prerequisites:**
- Azure CLI installed (`az --version`)
- Logged in to Azure (`az login`)
- Subscription set (`az account set --subscription "ID"`)

**Usage:**
```bash
# Make executable
chmod +x scripts/azure/provision-staging.sh

# Run script (interactive prompts for passwords)
./scripts/azure/provision-staging.sh

# Save credentials output for GitHub Secrets
```

**Estimated Time:** 15-20 minutes (automated)

**Output:**
- ACR credentials (for GitHub Secrets)
- Service Principal JSON (for GitHub Secrets)
- PostgreSQL connection string
- Container App URL

---

## Script Features

### Safety Features

✅ **Idempotent:** Safe to re-run, skips existing resources
✅ **Error handling:** Exits on first error (`set -euo pipefail`)
✅ **Secure passwords:** Prompts for passwords, never hardcoded
✅ **Color-coded output:** Info (blue), Success (green), Warning (yellow), Error (red)

### Interactive Prompts

The script will ask for:
1. Azure subscription confirmation
2. PostgreSQL admin password (min 8 chars, complexity enforced)
3. Microsoft Entra Tenant ID
4. Microsoft Entra Client ID
5. SendGrid API Key (optional, can skip)

### Resource Naming Convention

All resources use consistent naming:
```
lankaconnect-staging-*
├── lankaconnect-staging              (Resource Group)
├── lankaconnectstaging               (Container Registry)
├── lankaconnect-staging-db           (PostgreSQL Server)
├── lankaconnect-staging-kv           (Key Vault)
├── lankaconnect-staging              (Container Apps Environment)
├── lankaconnect-api-staging          (Container App)
└── lankaconnect-staging-logs         (Log Analytics Workspace)
```

---

## Post-Provisioning Steps

After running the script:

### 1. Save Credentials

**ACR Credentials (for GitHub Secrets):**
```
ACR_USERNAME_STAGING: lankaconnectstaging
ACR_PASSWORD_STAGING: <from script output>
```

**Service Principal (for GitHub Secrets):**
```
AZURE_CREDENTIALS_STAGING: <entire JSON from script output>
```

### 2. Apply Database Migration

```bash
# Connection string from script output
psql "Host=lankaconnect-staging-db.postgres.database.azure.com;..." \
  -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql

# Verify migration
psql "Host=lankaconnect-staging-db.postgres.database.azure.com;..." \
  -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"
```

### 3. Configure GitHub Actions

Navigate to: `Settings` → `Secrets and variables` → `Actions`

Add secrets:
1. `AZURE_CREDENTIALS_STAGING`
2. `ACR_USERNAME_STAGING`
3. `ACR_PASSWORD_STAGING`

### 4. Deploy Application

```bash
# Push to develop branch (triggers CI/CD)
git checkout develop
git push origin develop

# Monitor deployment
# https://github.com/yourorg/LankaConnect/actions
```

---

## Troubleshooting

### Issue: "Container Registry name already taken"

**Solution:** Container Registry names must be globally unique. Edit the script and change:
```bash
ACR_NAME="lankaconnectstaging2"  # Add suffix
```

### Issue: "Key Vault name already taken"

**Solution:** Key Vault names must be globally unique. Edit the script:
```bash
KEY_VAULT_NAME="lankaconnect-staging-kv2"  # Add suffix
```

### Issue: "PostgreSQL password complexity requirements not met"

**Solution:** Password must have:
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number
- At least 1 special character

Example: `P@ssw0rd123!`

### Issue: "Service Principal already exists"

**Solution:** Delete the old service principal first:
```bash
az ad sp delete --id $(az ad sp list --display-name 'lankaconnect-github-actions-staging' --query '[0].appId' -o tsv)

# Then re-run script
```

---

## Manual Cleanup

To delete the entire staging environment:

```bash
# Delete resource group (deletes all resources)
az group delete --name lankaconnect-staging --yes --no-wait

# Delete service principal
az ad sp delete --id $(az ad sp list --display-name 'lankaconnect-github-actions-staging' --query '[0].appId' -o tsv)
```

**Warning:** This action is irreversible. Backup any data first.

---

## Cost Estimation

### Staging Environment Monthly Cost

| Resource | Monthly Cost |
|----------|-------------|
| Container Apps (1 replica) | $15-20 |
| PostgreSQL (B1ms) | $12 |
| Container Registry (Basic) | $5 |
| Key Vault | $3 |
| Log Analytics | $5-10 |
| **Total** | **$45-55/month** |

**Cost Optimization:**
- Scale to zero on weekends: -$10/month
- Reduce log retention to 7 days: -$5/month

---

## Future Scripts (Planned)

### `provision-production.sh`

Similar to staging but with:
- General Purpose PostgreSQL (D2ds_v5) with HA
- Standard Container Registry with geo-replication
- Azure Cache for Redis (C1)
- Azure Blob Storage (LRS)
- Application Insights

**Estimated Cost:** $300-500/month

### `backup-restore.sh`

Automated backup and restore for disaster recovery.

### `scale-staging.sh`

Automated scaling script for GitHub Actions (scale to zero on weekends).

---

## Script Development Guidelines

### Adding New Scripts

1. **Naming:** Use kebab-case (e.g., `provision-production.sh`)
2. **Shebang:** Always start with `#!/bin/bash`
3. **Error handling:** `set -euo pipefail`
4. **Logging:** Use `log_info()`, `log_success()`, `log_warning()`, `log_error()`
5. **Idempotency:** Check if resource exists before creating
6. **Documentation:** Add header comments explaining purpose, prerequisites, usage

### Testing Scripts

```bash
# Test in dry-run mode (add --dry-run to az commands)
./provision-staging.sh

# Test in separate subscription
az account set --subscription "TEST_SUBSCRIPTION_ID"
./provision-staging.sh
```

---

## Contributing

When adding new scripts:
1. Test in development subscription first
2. Document all prerequisites
3. Add troubleshooting section
4. Update this README
5. Submit PR with script + documentation

---

## Support

- **Azure CLI Issues:** https://github.com/Azure/azure-cli/issues
- **LankaConnect DevOps:** devops@lankaconnect.com
- **Documentation:** `docs/deployment/AZURE_DEPLOYMENT_GUIDE.md`

---

**Last Updated:** 2025-10-28
**Maintained By:** DevOps Team
