# ADR-005: Stripe API Key Configuration in Azure Container Apps

**Date**: 2025-12-13
**Status**: RECOMMENDED
**Decision Makers**: System Architecture Designer
**Stakeholders**: DevOps, Backend Team, Security Team

## Context

The Stripe payment integration is failing in the staging environment with "Invalid API Key" errors. The deployment workflow references Stripe secrets stored in Azure Key Vault using the `secretref:` pattern, but the Container App is unable to access these secrets, resulting in deployment failures.

### Current State Analysis

**Infrastructure Discovery**:
- Container App: `lankaconnect-api-staging`
- Resource Group: `lankaconnect-staging`
- Key Vault: `lankaconnect-staging-kv`
- **Identity Type**: System-Assigned Managed Identity (ALREADY EXISTS)
  - Principal ID: `bf69f3f8-e9c6-464a-9d1f-0038f90e8d03`
  - Tenant ID: `369a3c47-33b7-4baa-98b8-6ddf16a51a31`

**Secrets in Key Vault**:
```
azure-storage-connection-string
DATABASE-CONNECTION-STRING
EMAIL-FROM-ADDRESS
ENTRA-AUDIENCE
ENTRA-CLIENT-ID
ENTRA-ENABLED
ENTRA-TENANT-ID
JWT-AUDIENCE
JWT-ISSUER
JWT-SECRET-KEY
SMTP-HOST
SMTP-PASSWORD
SMTP-PORT
SMTP-USERNAME
stripe-publishable-key  <-- NEWLY ADDED
stripe-secret-key       <-- NEWLY ADDED
```

**Deployment Workflow Pattern** (`.github/workflows/deploy-staging.yml`):
```yaml
ConnectionStrings__DefaultConnection=secretref:database-connection-string
Jwt__Key=secretref:jwt-secret-key
Stripe__SecretKey=secretref:stripe-secret-key
Stripe__PublishableKey=secretref:stripe-publishable-key
```

**Configuration File Pattern** (`appsettings.Staging.json`):
```json
{
  "Stripe": {
    "SecretKey": "sk_test_4eC39HqLyjWDarhtT1l8w65C",
    "PublishableKey": "pk_test_51234567890abcdefghijklmnop"
  }
}
```

## Problem Statement

The deployment fails with:
```
ERROR: managed Identity with resource Id /subscriptions/.../lankaconnect-staging-identity
was not found when trying to get secret stripe-secret-key from Azure Key Vault.
```

### Root Cause Analysis

The error message references `lankaconnect-staging-identity`, but the Container App already has a **System-Assigned Managed Identity**. The issue has multiple causes:

1. **Secret Name Mismatch**: Key Vault secrets use lowercase with hyphens (`stripe-secret-key`), but the deployment workflow references them correctly via `secretref:`

2. **Managed Identity Permissions**: The System-Assigned Managed Identity may not have "Get" permissions for the newly added Stripe secrets

3. **Case Sensitivity Issue**: Azure Key Vault secret names are case-insensitive, but Container App secret references may be case-sensitive

4. **appsettings.Staging.json Hardcoded Values**: The configuration file contains hardcoded test API keys that will override environment variables if not properly configured

## Decision

**RECOMMENDED APPROACH: Fix System-Assigned Identity Permissions**

Use the EXISTING System-Assigned Managed Identity and grant it proper Key Vault access permissions. This aligns with the current infrastructure pattern used for all other secrets.

### Why This Approach?

1. **Consistency**: All other secrets (JWT, database, email) use the same pattern
2. **Least Privilege**: System-Assigned identities are tied to the Container App lifecycle
3. **No Additional Resources**: No need to create user-assigned identities
4. **Azure Best Practice**: System-Assigned is recommended for single-resource scenarios
5. **Working Pattern**: Other secrets work, proving the pattern is correct

## Implementation Steps

### Step 1: Verify Key Vault Access Policy

Grant the System-Assigned Managed Identity access to the Stripe secrets:

```bash
# Get the principal ID of the Container App's System-Assigned Identity
PRINCIPAL_ID=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "identity.principalId" -o tsv)

# Grant Key Vault "Get" and "List" permissions to the identity
az keyvault set-policy \
  --name lankaconnect-staging-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Step 2: Verify Secret Names Match

Ensure Key Vault secret names exactly match the deployment workflow references:

```bash
# Verify secrets exist with correct names
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-secret-key \
  --query "name" -o tsv

az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-publishable-key \
  --query "name" -o tsv
```

### Step 3: Remove Hardcoded Values from appsettings.Staging.json

**CRITICAL**: The `appsettings.Staging.json` file contains hardcoded test API keys that will override environment variables. This must be fixed:

**Current (WRONG)**:
```json
{
  "Stripe": {
    "SecretKey": "sk_test_4eC39HqLyjWDarhtT1l8w65C",
    "PublishableKey": "pk_test_51234567890abcdefghijklmnop"
  }
}
```

**Correct Pattern**:
```json
{
  "Stripe": {
    "SecretKey": "${STRIPE_SECRET_KEY}",
    "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"
  }
}
```

### Step 4: Update Deployment Workflow

The deployment workflow is already correct, but verify the environment variable names match ASP.NET Core configuration binding:

```yaml
Stripe__SecretKey=secretref:stripe-secret-key
Stripe__PublishableKey=secretref:stripe-publishable-key
```

### Step 5: Test Deployment

1. Trigger deployment via push to `develop` branch
2. Monitor Container App logs:
   ```bash
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --tail 50
   ```
3. Verify Stripe integration:
   ```bash
   # Test health endpoint
   curl https://<container-app-url>/health

   # Test Stripe webhook endpoint (should return 400 for invalid payload)
   curl -X POST https://<container-app-url>/api/webhooks/stripe \
     -H "Content-Type: application/json" \
     -d '{"test": "data"}'
   ```

## Alternatives Considered

### Alternative 1: User-Assigned Managed Identity

**Approach**: Create a separate user-assigned identity and assign it to the Container App.

**Pros**:
- Can be shared across multiple resources
- Persists after Container App deletion
- Better for complex multi-resource scenarios

**Cons**:
- Additional Azure resource to manage
- Overkill for single Container App scenario
- Requires extra configuration in deployment workflow
- Inconsistent with current infrastructure pattern

**Verdict**: REJECTED - System-Assigned is sufficient and follows current pattern

### Alternative 2: Direct Environment Variables (No Key Vault)

**Approach**: Store Stripe API keys directly as environment variables in GitHub Secrets and pass them to Container App.

**Pros**:
- Simpler configuration
- No managed identity permissions required
- Faster troubleshooting

**Cons**:
- Less secure (secrets visible in GitHub Actions logs if misconfigured)
- Inconsistent with other secrets (JWT, database)
- No central secret rotation via Key Vault
- Violates Azure best practices for production systems

**Verdict**: REJECTED - Key Vault is the right pattern for production secrets

### Alternative 3: Azure App Configuration

**Approach**: Use Azure App Configuration service for all application settings.

**Pros**:
- Centralized configuration management
- Feature flags support
- Dynamic configuration updates

**Cons**:
- Additional service costs
- Significant architecture change
- Overkill for current needs
- Requires major refactoring

**Verdict**: REJECTED - Not justified for current requirements

## Architecture Diagrams

### Current Secret Flow (After Fix)

```
┌─────────────────────────────────────────────────────────────┐
│                   GitHub Actions Workflow                   │
│                                                              │
│  1. Build & Test                                            │
│  2. Push Docker Image to ACR                                │
│  3. Run EF Migrations (reads DB connection from Key Vault)  │
│  4. Update Container App with environment variables:        │
│     Stripe__SecretKey=secretref:stripe-secret-key           │
│     Stripe__PublishableKey=secretref:stripe-publishable-key │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      │ az containerapp update
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│          Azure Container App (lankaconnect-api-staging)     │
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │  System-Assigned Managed Identity                  │    │
│  │  Principal ID: bf69f3f8-e9c6-464a-9d1f-0038f90e8d03│    │
│  └────────────────┬───────────────────────────────────┘    │
│                   │                                          │
│                   │ Uses identity to resolve secretref:     │
│                   │                                          │
│  Environment Variables:                                     │
│  - Stripe__SecretKey=secretref:stripe-secret-key            │
│  - Stripe__PublishableKey=secretref:stripe-publishable-key  │
└───────────────────┼──────────────────────────────────────────┘
                    │
                    │ Key Vault Access Policy:
                    │ - Get Secret Permission
                    │ - List Secret Permission
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│         Azure Key Vault (lankaconnect-staging-kv)           │
│                                                              │
│  Secrets:                                                    │
│  ├── stripe-secret-key: "sk_live_xxx..."                    │
│  ├── stripe-publishable-key: "pk_live_xxx..."               │
│  ├── database-connection-string: "Host=..."                 │
│  ├── jwt-secret-key: "xxx..."                               │
│  └── ... (other secrets)                                    │
└─────────────────────────────────────────────────────────────┘
                    │
                    │ Resolved values injected into:
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Configuration System              │
│                                                              │
│  appsettings.Staging.json:                                  │
│  {                                                           │
│    "Stripe": {                                               │
│      "SecretKey": "${STRIPE_SECRET_KEY}",                   │
│      "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"          │
│    }                                                         │
│  }                                                           │
│                                                              │
│  Environment variables OVERRIDE config file values          │
│  Final runtime configuration:                               │
│  - Stripe.SecretKey = "sk_live_xxx..." (from Key Vault)     │
│  - Stripe.PublishableKey = "pk_live_xxx..." (from KV)       │
└─────────────────────────────────────────────────────────────┘
```

### ASP.NET Core Configuration Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│            ASP.NET Core Configuration Sources               │
│                   (Lowest to Highest Priority)              │
└─────────────────────────────────────────────────────────────┘

1. appsettings.json (Base configuration)
   Priority: LOWEST
   ├── "Stripe": { "SecretKey": "", "PublishableKey": "" }
   └── Used for structure and defaults

2. appsettings.Staging.json (Environment-specific)
   Priority: LOW
   ├── "Stripe": { "SecretKey": "${STRIPE_SECRET_KEY}", ... }
   └── Should use variable placeholders, NOT hardcoded values

3. Environment Variables (Container App configuration)
   Priority: HIGH
   ├── Stripe__SecretKey=secretref:stripe-secret-key
   ├── Stripe__PublishableKey=secretref:stripe-publishable-key
   └── Resolved from Key Vault via managed identity

4. Command Line Arguments (Not used in Container Apps)
   Priority: HIGHEST
   └── Not applicable to this scenario

RESULT: Environment variables WIN, overriding appsettings.*.json
```

## Risks and Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Managed Identity permissions not propagated immediately | Deployment fails | Low | Wait 30-60 seconds after granting permissions before deploying |
| Hardcoded values in appsettings.Staging.json override env vars | Wrong API keys used | Medium | Update appsettings.Staging.json to use variable placeholders |
| Secret name case mismatch | Key Vault lookup fails | Low | Verify exact secret names match deployment workflow references |
| Key Vault access policy gets reset | Future deployments fail | Low | Document access policy in infrastructure-as-code (Bicep/Terraform) |
| Stripe webhook secret not configured | Webhook signature validation fails | Medium | Add `Stripe__WebhookSecret` to Key Vault and deployment workflow |

## Quality Attributes Addressed

### Security
- Secrets stored in Azure Key Vault (encrypted at rest)
- No secrets in source code or GitHub Actions logs
- Managed identity provides secure, credential-less authentication
- Follows Azure Well-Architected Framework security pillar

### Reliability
- Consistent with existing secrets management pattern
- System-Assigned identity tied to Container App lifecycle
- No orphaned identities after resource deletion

### Maintainability
- Centralized secret management via Key Vault
- Easy secret rotation without code changes
- Clear separation of configuration and secrets

### Operational Excellence
- Deployment workflow automation unchanged
- Minimal additional configuration required
- Easy to troubleshoot via Container App logs

## Success Criteria

1. Deployment completes without "Invalid API Key" errors
2. Container App logs show Stripe service initialized successfully
3. Stripe webhook endpoint responds to test requests
4. Payment flow works end-to-end in staging environment
5. No hardcoded secrets in application configuration files

## Implementation Checklist

- [ ] Grant Key Vault "Get" and "List" permissions to System-Assigned Managed Identity
- [ ] Verify Stripe secrets exist in Key Vault with correct names
- [ ] Update `appsettings.Staging.json` to remove hardcoded Stripe API keys
- [ ] Add webhook secret to Key Vault if not already present
- [ ] Update deployment workflow to include `Stripe__WebhookSecret` environment variable
- [ ] Trigger deployment to staging environment
- [ ] Verify Container App logs show successful Stripe initialization
- [ ] Test Stripe payment flow in staging
- [ ] Document the fix in deployment runbook
- [ ] Create Bicep/Terraform template for infrastructure-as-code

## References

- [Azure Container Apps Managed Identity](https://learn.microsoft.com/en-us/azure/container-apps/managed-identity)
- [Azure Key Vault Access Policies](https://learn.microsoft.com/en-us/azure/key-vault/general/assign-access-policy)
- [Azure Container Apps Secrets](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Stripe API Keys Best Practices](https://stripe.com/docs/keys)

## Follow-Up Actions

1. Create infrastructure-as-code templates (Bicep or Terraform) to codify:
   - Key Vault access policies
   - Managed identity configuration
   - Container App secret references

2. Add monitoring and alerting for:
   - Stripe API rate limits
   - Payment processing errors
   - Webhook delivery failures

3. Document secret rotation procedures:
   - How to rotate Stripe API keys
   - Testing checklist after rotation
   - Rollback procedures

4. Consider implementing Key Vault secret versioning:
   - Enable automatic secret rotation
   - Implement blue-green deployment for secret updates
