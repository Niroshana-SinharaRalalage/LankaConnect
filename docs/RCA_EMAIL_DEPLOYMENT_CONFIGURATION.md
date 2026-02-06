# Root Cause Analysis: Email Deployment Configuration Issue

## Date: 2025-12-19
## Severity: HIGH
## Impact: Email functionality completely broken in deployed environments

---

## Executive Summary

**Root Cause**: GitHub Actions workflow hardcoded email provider to SMTP instead of Azure Communication Services, overriding application configuration files.

**Impact**:
- Email.cs regex fix deployed successfully
- Template migration successful
- But emails still failing in staging/production because deployment uses wrong provider

**Status**: Email system works locally but fails in deployed environments

---

## Problem Statement

### What Was Expected
- Application would use Azure Communication Services as configured in `appsettings.Production.json` and `appsettings.Staging.json`
- Email provider setting: `"Provider": "Azure"`
- Azure-specific configuration would be used

### What Actually Happened
- GitHub Actions workflow line 144 sets: `EmailSettings__Provider=Smtp`
- This OVERRIDES the JSON configuration files during deployment
- Application tries to use SMTP instead of Azure Communication Services
- Emails fail because SMTP credentials are incomplete/incorrect for production use

---

## Root Cause Analysis

### 1. Configuration Override Hierarchy

ASP.NET Core configuration hierarchy (lowest to highest priority):
1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables (Container App settings)
4. Command-line arguments

**The workflow sets environment variables which OVERRIDE the JSON files.**

### 2. Deployment Workflow Configuration

**File**: `.github/workflows/deploy-staging.yml` (lines 144-152)

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

### 3. Application Configuration Files

**File**: `src/LankaConnect.API/appsettings.Staging.json` (lines 69-80)

```json
"EmailSettings": {
  "Provider": "Azure",
  "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
  "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}",
  "SenderName": "LankaConnect Staging",
  "SmtpServer": "${SMTP_HOST}",
  "SmtpPort": 587,
  "SenderEmail": "${EMAIL_FROM_ADDRESS}",
  "Username": "${SMTP_USERNAME}",
  "Password": "${SMTP_PASSWORD}",
  "EnableSsl": true
}
```

**File**: `src/LankaConnect.API/appsettings.Production.json` (lines 61-73)

```json
"EmailSettings": {
  "Provider": "Azure",
  "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
  "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}",
  "SenderName": "LankaConnect",
  "SmtpServer": "${SMTP_HOST}",
  "SmtpPort": 587,
  "SenderEmail": "${EMAIL_FROM_ADDRESS}",
  "Username": "${SMTP_USERNAME}",
  "Password": "${SMTP_PASSWORD}",
  "EnableSsl": true,
  "TemplateBasePath": "Templates/Email"
}
```

### 4. Key Vault Secrets Inventory

**Current secrets in `lankaconnect-staging-kv`:**

```
azure-storage-connection-string  ✅ (exists)
DATABASE-CONNECTION-STRING       ✅
EMAIL-FROM-ADDRESS              ✅ (SMTP-related)
SMTP-HOST                       ✅ (SMTP-related)
SMTP-PASSWORD                   ✅ (SMTP-related)
SMTP-PORT                       ✅ (SMTP-related)
SMTP-USERNAME                   ✅ (SMTP-related)
```

**MISSING secrets for Azure Communication Services:**
```
AZURE-EMAIL-CONNECTION-STRING   ❌ (DOES NOT EXIST)
AZURE-EMAIL-SENDER-ADDRESS      ❌ (DOES NOT EXIST)
```

---

## Impact Analysis

### 1. Functionality Impact
- **Email verification**: BROKEN
- **Password reset**: BROKEN
- **Event notifications**: BROKEN
- **User registration**: PARTIALLY BROKEN (account created but verification email fails)

### 2. User Experience Impact
- Users cannot verify email addresses
- Users cannot reset passwords
- Event organizers cannot receive notifications
- Business workflow disrupted

### 3. Technical Debt
- Configuration inconsistency between files and deployment
- No validation that required secrets exist before deployment
- Duplicate configuration (both SMTP and Azure in same files)

---

## Timeline

1. **Initial Development**: Application configured to use Azure Communication Services in JSON files
2. **Deployment Setup**: GitHub Actions workflow configured with SMTP settings (likely copied from template)
3. **Email.cs Fix**: Regex pattern fixed (2025-12-17)
4. **Template Migration**: Email templates migrated successfully
5. **Production Issue**: Emails still failing because deployment uses wrong provider
6. **RCA Triggered**: Investigation revealed workflow override issue (2025-12-19)

---

## Why This Wasn't Caught Earlier

1. **Configuration Override Not Obvious**: Environment variables silently override JSON files
2. **No Validation**: Workflow doesn't validate that all required secrets exist
3. **Mixed Configuration**: Both SMTP and Azure settings present in JSON files (fallback pattern)
4. **Testing Gap**: Local testing uses `appsettings.Development.json` which may differ

---

## Solution Requirements

### Must-Have
1. Create Azure Communication Services secrets in Key Vault
2. Update workflow to use Azure provider instead of SMTP
3. Validate secret existence before deployment
4. Apply to both staging and production workflows

### Should-Have
1. Remove SMTP configuration from production files (single provider pattern)
2. Add smoke test for email functionality in deployment
3. Document email provider configuration in ADR

### Nice-to-Have
1. Add configuration validation at application startup
2. Alert on missing required configuration
3. Fallback mechanism with proper logging

---

## Questions for Infrastructure Team

### Critical Questions
1. **Do Azure Communication Services resources exist?**
   - If yes: Provide connection string and verified sender address
   - If no: Need to provision Azure Communication Services resource first

2. **Key Vault Secret Names:**
   - What should we name the secrets?
   - Suggested: `AZURE-EMAIL-CONNECTION-STRING` and `AZURE-EMAIL-SENDER-ADDRESS`
   - Or different naming convention?

3. **Email Domain Verification:**
   - Is the sender domain verified in Azure Communication Services?
   - What sender address should we use? (e.g., noreply@lankaconnect.com)

### Configuration Questions
4. **SMTP Fallback:**
   - Should we keep SMTP settings as fallback?
   - Or remove them entirely from production configuration?

5. **Production vs Staging:**
   - Same Azure Communication Services resource for both?
   - Or separate resources with different sender addresses?

6. **deploy-production.yml:**
   - Does it exist?
   - Does it have the same issue?
   - Need to update it too?

---

## Next Steps (Pending Answers)

### Immediate (Once Secrets Available)
1. Add secrets to Key Vault
2. Update `deploy-staging.yml` workflow
3. Update `deploy-production.yml` workflow (if exists)
4. Test deployment in staging
5. Verify email functionality works

### Short-term
1. Clean up configuration files (remove SMTP from production files)
2. Add configuration validation
3. Document in Architecture Decision Record

### Long-term
1. Implement deployment validation that checks required secrets exist
2. Add smoke tests for email functionality
3. Consider infrastructure-as-code for Azure Communication Services

---

## Risk Assessment

**If Not Fixed:**
- HIGH: Email functionality remains broken in production
- MEDIUM: User onboarding broken (cannot verify emails)
- MEDIUM: Password reset unavailable
- LOW: But workaround exists (manual verification in database)

**If Fixed Incorrectly:**
- MEDIUM: Could break email in both staging and production
- LOW: Easy to rollback via workflow change
- LOW: No data loss risk (configuration only)

---

## Appendix A: Configuration Files Comparison

### Local Development (Working)
- Uses `appsettings.Development.json`
- May use different email provider or local SMTP
- Not affected by workflow override

### Staging (Broken)
- Uses `appsettings.Staging.json` → Sets Provider=Azure
- Workflow overrides → Sets Provider=Smtp
- Result: SMTP used, but secrets incomplete

### Production (Assumed Broken)
- Uses `appsettings.Production.json` → Sets Provider=Azure
- Workflow likely overrides → Sets Provider=Smtp
- Result: Same issue as staging

---

## Appendix B: Proposed Workflow Fix

### Current (WRONG):
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

### Proposed (CORRECT):
```yaml
EmailSettings__Provider=Azure \
EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
EmailSettings__SenderName="LankaConnect Staging" \
EmailSettings__TemplateBasePath=Templates/Email \
```

### Alternative (Minimal Override):
```yaml
# Only override what's needed, let appsettings.Staging.json provide defaults
EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \
EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \
```

**Recommendation**: Use "Proposed (CORRECT)" for explicit configuration.

---

## Sign-off

**Analysis Completed By**: System Architect Agent
**Date**: 2025-12-19
**Status**: Awaiting infrastructure team input on Azure Communication Services

**Approval Required From**:
- [ ] Infrastructure Team (Azure Communication Services provisioning)
- [ ] DevOps Team (Key Vault secret creation)
- [ ] Development Team (Workflow update review)
- [ ] QA Team (Post-deployment testing)
