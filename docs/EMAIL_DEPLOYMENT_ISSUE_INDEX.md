# Email Deployment Configuration Issue - Documentation Index

**Issue ID**: EMAIL-DEPLOY-001
**Date Identified**: 2025-12-19
**Status**: RCA Complete, Fix Ready, Awaiting Infrastructure Input
**Priority**: HIGH
**Estimated Fix Time**: 1 hour (once secrets available)

---

## Quick Links

**Start Here:**
- [Summary (This page)](#executive-summary)
- [What You Need to Know in 2 Minutes](#what-you-need-to-know-in-2-minutes)

**For Different Roles:**
- [For Infrastructure Team](#for-infrastructure-team)
- [For DevOps Team](#for-devops-team)
- [For Development Team](#for-development-team)
- [For Management](#for-management)

**Documentation:**
- [All Documents](#documentation-files)
- [Visual Diagrams](#visual-diagrams)
- [Scripts and Tools](#scripts-and-tools)

---

## What You Need to Know in 2 Minutes

### The Problem
Emails aren't working in staging/production because:
1. Configuration files say: "Use Azure Communication Services"
2. Deployment workflow overrides: "Use SMTP instead"
3. SMTP credentials are incomplete/invalid
4. Result: All emails fail

### The Fix
1. Get Azure Communication Services connection string
2. Add to Key Vault as secrets
3. Update workflow to use Azure instead of SMTP
4. Deploy and test
5. Done in 1 hour

### What's Blocking
We need from infrastructure team:
- Azure Communication Services connection string
- Azure Communication Services sender address
- Or confirmation that we need to provision these resources

### Impact
**Broken:**
- User email verification
- Password reset
- Event notifications

**Working:**
- Everything else (auth, events, payments, etc.)

---

## Executive Summary

### Root Cause
GitHub Actions workflow (`.github/workflows/deploy-staging.yml` line 144) sets environment variable `EmailSettings__Provider=Smtp`, which has higher priority than `appsettings.Staging.json` configuration that specifies `"Provider": "Azure"`. This causes the application to attempt using SMTP with incomplete credentials instead of Azure Communication Services.

### Impact
- **Severity**: HIGH - Core email functionality broken
- **Scope**: Staging and Production environments (Local development unaffected)
- **User Impact**: Cannot verify emails, reset passwords, or receive notifications
- **Business Impact**: User onboarding blocked, support tickets likely increasing

### Solution Status
- **RCA**: ✅ Complete
- **Fix Plan**: ✅ Documented
- **Scripts**: ✅ Ready
- **Testing Plan**: ✅ Defined
- **Rollback Plan**: ✅ Documented
- **Blocking Issue**: ⏸️ Awaiting Azure Communication Services secrets

### Timeline
- **Issue Identified**: 2025-12-19
- **RCA Completed**: 2025-12-19 (same day)
- **Fix Ready**: 2025-12-19 (same day)
- **Deployment**: Pending infrastructure input
- **Estimated Resolution**: 1 hour after secrets provided

---

## For Infrastructure Team

### What We Need From You

**Critical Information:**

1. **Does Azure Communication Services resource exist?**
   ```bash
   az resource list --resource-type "Microsoft.Communication/CommunicationServices"
   ```

2. **If yes, please provide:**
   - Resource name: ?
   - Resource group: ?
   - Connection string: `az communication list-key --name <name> --resource-group <rg>`
   - Verified sender address: (from Azure Portal → Communication Services → Email → Domains)

3. **If no, please provision:**
   - Azure Communication Services resource
   - Email domain verification (1-2 days for DNS)
   - Verified sender address

**Estimated Time Needed From You:**
- If resource exists: 15 minutes (get connection string, provide to DevOps)
- If needs provisioning: 1-3 days (resource creation + domain verification)

**Documents for You:**
- [Proposed Fix - Prerequisites Section](./PROPOSED_FIX_EMAIL_DEPLOYMENT.md#prerequisites)
- [Quick Start - If Secrets Don't Exist](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md#if-secrets-dont-exist)

---

## For DevOps Team

### What We Need From You

**Once infrastructure provides secrets:**

1. **Add secrets to Key Vault:**
   ```bash
   az keyvault secret set \
     --vault-name lankaconnect-staging-kv \
     --name AZURE-EMAIL-CONNECTION-STRING \
     --value "<from infrastructure team>"

   az keyvault secret set \
     --vault-name lankaconnect-staging-kv \
     --name AZURE-EMAIL-SENDER-ADDRESS \
     --value "<from infrastructure team>"
   ```

2. **Verify secrets added:**
   ```bash
   ./docs/workflow-fixes/verify-secrets.sh staging
   ```

3. **Coordinate deployment** with development team

**Estimated Time Needed From You:**
- 15 minutes (add secrets, verify, coordinate)

**Documents for You:**
- [Quick Start Guide](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md)
- [Verify Secrets Script](./workflow-fixes/verify-secrets.sh)

---

## For Development Team

### What We Need From You

**Once DevOps adds secrets:**

1. **Verify secrets exist:**
   ```bash
   chmod +x ./docs/workflow-fixes/verify-secrets.sh
   ./docs/workflow-fixes/verify-secrets.sh staging
   ```

2. **Apply fix:**
   ```bash
   chmod +x ./docs/workflow-fixes/apply-fixes.sh
   ./docs/workflow-fixes/apply-fixes.sh --dry-run --environment staging
   ./docs/workflow-fixes/apply-fixes.sh --environment staging
   ```

3. **Review and deploy:**
   ```bash
   git diff .github/workflows/deploy-staging.yml
   git add .github/workflows/deploy-staging.yml
   git commit -m "fix: Use Azure Communication Services for email"
   git push origin develop
   ```

4. **Monitor and test:**
   ```bash
   gh run watch
   # After deployment succeeds, test email functionality
   ```

**Estimated Time Needed From You:**
- 30 minutes (apply fix, test, verify)

**Documents for You:**
- [Quick Start Guide](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md)
- [Proposed Fix - Implementation Steps](./PROPOSED_FIX_EMAIL_DEPLOYMENT.md#implementation-steps)
- [Apply Fix Script](./workflow-fixes/apply-fixes.sh)

---

## For Management

### Business Impact

**Current State:**
- Email functionality broken in production
- User onboarding impacted (cannot verify emails)
- Password reset unavailable
- Support workaround: Manual verification possible but not scalable

**Fix Timeline:**
- Analysis: ✅ Complete (same day as identification)
- Fix: ⏸️ Ready, waiting on Azure credentials
- Deployment: 1 hour after credentials provided
- Total: Could be resolved today if credentials available

**Risk Assessment:**
- **Fix Risk**: LOW (configuration-only, easy rollback)
- **Current Impact**: HIGH (core functionality broken)
- **Fix Complexity**: LOW (well-documented, tested approach)

**Cost:**
- Azure Communication Services: Free tier (500 emails/month) or $0.25 per 1000 emails
- Engineering time: ~2 hours total across all teams
- No additional infrastructure costs

**Recommendation:**
- Proceed with fix as soon as Azure credentials available
- Low risk, high value fix
- Enables resumption of normal business operations

**Documents for You:**
- [Executive Summary (This section)](#executive-summary)
- [Fix Summary](./EMAIL_FIX_SUMMARY.md)
- [RCA - Impact Analysis Section](./RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md#impact-analysis)

---

## Documentation Files

### Strategic Documents

1. **[EMAIL_FIX_SUMMARY.md](./EMAIL_FIX_SUMMARY.md)**
   - High-level overview
   - Current state and what's needed
   - Timeline and resource requirements
   - **Best for**: Management, stakeholders

2. **[RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md](./RCA_EMAIL_DEPLOYMENT_CONFIGURATION.md)**
   - Complete root cause analysis
   - Configuration hierarchy explanation
   - Timeline of events
   - Questions for infrastructure team
   - **Best for**: Technical leads, post-mortem review

3. **[PROPOSED_FIX_EMAIL_DEPLOYMENT.md](./PROPOSED_FIX_EMAIL_DEPLOYMENT.md)**
   - Detailed implementation plan
   - Prerequisites checklist
   - Step-by-step instructions
   - Testing strategy
   - Rollback procedures
   - **Best for**: Implementation team, technical reference

### Tactical Documents

4. **[EMAIL_DEPLOYMENT_FIX_QUICKSTART.md](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md)**
   - Quick 5-minute fix (if secrets ready)
   - Common commands
   - Troubleshooting guide
   - **Best for**: DevOps, developers implementing fix

5. **[EMAIL_DEPLOYMENT_ISSUE_INDEX.md](./EMAIL_DEPLOYMENT_ISSUE_INDEX.md)** (This file)
   - Navigation hub
   - Role-based guidance
   - Quick links to all resources
   - **Best for**: Entry point for anyone new to the issue

### Implementation Tools

6. **[workflow-fixes/README.md](./workflow-fixes/README.md)**
   - How to apply workflow fixes
   - Multiple application methods
   - Verification steps
   - **Best for**: Developers applying the fix

7. **[workflow-fixes/verify-secrets.sh](./workflow-fixes/verify-secrets.sh)**
   - Automated secret verification
   - Pre-deployment validation
   - Clear error messages with remediation steps
   - **Best for**: Pre-deployment validation

8. **[workflow-fixes/apply-fixes.sh](./workflow-fixes/apply-fixes.sh)**
   - Automated workflow patching
   - Dry-run mode for safety
   - Automatic backup creation
   - **Best for**: Automated fix application

9. **[workflow-fixes/deploy-staging.yml.patch](./workflow-fixes/deploy-staging.yml.patch)**
   - Standard patch file format
   - Exact changes needed
   - Can apply with `patch` command
   - **Best for**: Manual review, version control

### Visual Diagrams

10. **[diagrams/EMAIL_CONFIGURATION_ISSUE.md](./diagrams/EMAIL_CONFIGURATION_ISSUE.md)**
    - Configuration override flow diagram
    - Problem flow diagram
    - Solution flow diagram
    - Architecture diagrams
    - **Best for**: Visual learners, presentations

---

## Visual Diagrams

Located in `docs/diagrams/EMAIL_CONFIGURATION_ISSUE.md`:

1. **Configuration Override Flow** - Shows ASP.NET Core config priority
2. **Problem Flow Diagram** - Traces how the issue manifests
3. **Solution Flow Diagram** - Shows fix implementation path
4. **Configuration Hierarchy** - Priority levels explained
5. **Deployment Architecture** - Complete deployment flow
6. **Email Service Code Path** - Code-level flow diagram
7. **Secret Management Flow** - How secrets are referenced
8. **Fix Validation Flow** - Pre/post deployment checks
9. **Architecture Decision Record** - Why Azure over SMTP

**Best for**: Technical discussions, presentations, onboarding

---

## Scripts and Tools

### Verification Script
**File**: `docs/workflow-fixes/verify-secrets.sh`
**Purpose**: Check that required Azure Communication Services secrets exist
**Usage**:
```bash
chmod +x ./docs/workflow-fixes/verify-secrets.sh
./docs/workflow-fixes/verify-secrets.sh staging
./docs/workflow-fixes/verify-secrets.sh production
```
**Output**:
- ✅ Lists found secrets
- ❌ Shows missing secrets with commands to add them
- Exit code 0 if all secrets found, 1 if any missing

### Fix Application Script
**File**: `docs/workflow-fixes/apply-fixes.sh`
**Purpose**: Automatically apply workflow configuration fixes
**Usage**:
```bash
chmod +x ./docs/workflow-fixes/apply-fixes.sh

# Dry-run (preview changes without applying)
./docs/workflow-fixes/apply-fixes.sh --dry-run --environment staging

# Apply to staging
./docs/workflow-fixes/apply-fixes.sh --environment staging

# Apply to production
./docs/workflow-fixes/apply-fixes.sh --environment production

# Apply to both
./docs/workflow-fixes/apply-fixes.sh --environment both
```
**Features**:
- Automatic backup creation
- Dry-run mode
- Validates file exists before modifying
- Shows diff of changes
- Safe to run multiple times (idempotent)

### Manual Patch File
**File**: `docs/workflow-fixes/deploy-staging.yml.patch`
**Purpose**: Standard patch format for manual or automated application
**Usage**:
```bash
# Apply with patch command
patch .github/workflows/deploy-staging.yml < docs/workflow-fixes/deploy-staging.yml.patch

# Or review manually
cat docs/workflow-fixes/deploy-staging.yml.patch
```

---

## Implementation Workflow

### Step-by-Step (Happy Path)

```
┌────────────────────────────────────────────────────────┐
│ Phase 1: Information Gathering (15 minutes)            │
├────────────────────────────────────────────────────────┤
│ 1. Infrastructure team checks Azure Communication      │
│    Services resource exists                            │
│ 2. Infrastructure team retrieves connection string     │
│ 3. Infrastructure team retrieves sender address        │
│ 4. Infrastructure team provides to DevOps team         │
└────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│ Phase 2: Secret Management (15 minutes)                │
├────────────────────────────────────────────────────────┤
│ 1. DevOps adds secrets to staging Key Vault           │
│ 2. DevOps adds secrets to production Key Vault        │
│ 3. DevOps verifies secrets with verify-secrets.sh     │
│ 4. DevOps notifies development team                   │
└────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│ Phase 3: Fix Application (10 minutes)                  │
├────────────────────────────────────────────────────────┤
│ 1. Developer runs verify-secrets.sh (confirm ready)   │
│ 2. Developer runs apply-fixes.sh --dry-run            │
│ 3. Developer reviews changes                          │
│ 4. Developer runs apply-fixes.sh (apply for real)     │
│ 5. Developer commits and pushes to develop            │
└────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│ Phase 4: Deployment (10 minutes automatic)             │
├────────────────────────────────────────────────────────┤
│ 1. GitHub Actions triggers on push to develop         │
│ 2. Workflow builds and tests                          │
│ 3. Workflow deploys to staging with new config        │
│ 4. Health checks pass                                  │
└────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│ Phase 5: Testing & Verification (20 minutes)           │
├────────────────────────────────────────────────────────┤
│ 1. Developer checks container app environment vars    │
│ 2. Developer reviews logs for email provider init     │
│ 3. Developer tests user registration (email verify)   │
│ 4. Developer tests password reset email               │
│ 5. Developer tests event notification email           │
│ 6. QA verifies all emails received                    │
└────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│ Phase 6: Production Deployment (Optional, if ready)    │
├────────────────────────────────────────────────────────┤
│ 1. Merge develop to main/master                       │
│ 2. Production deployment triggers automatically       │
│ 3. Repeat testing and verification                    │
│ 4. Monitor production email functionality             │
└────────────────────────────────────────────────────────┘
                         │
                         ▼
                   ✅ COMPLETE
```

**Total Time**: ~1 hour (after infrastructure provides credentials)

---

## Testing Checklist

### Pre-Deployment Testing
- [ ] Secrets exist in Key Vault (run `verify-secrets.sh`)
- [ ] Workflow changes reviewed (run `apply-fixes.sh --dry-run`)
- [ ] Backup of current workflow created
- [ ] Changes committed to version control

### Post-Deployment Testing
- [ ] Deployment completed without errors
- [ ] Container app started successfully
- [ ] Environment variables show `Provider=Azure`
- [ ] Logs show "Email provider initialized: Azure"
- [ ] Health endpoint responding

### Functional Testing
- [ ] User registration sends verification email
- [ ] Verification email received (< 2 minutes)
- [ ] Email content renders correctly
- [ ] Email links work correctly
- [ ] Password reset email sends
- [ ] Password reset email received
- [ ] Event notification email sends
- [ ] Event notification email received

### Monitoring
- [ ] No errors in Application Insights
- [ ] No alerts triggered
- [ ] Email send metrics normal
- [ ] Container app metrics normal

---

## Troubleshooting Guide

### Issue: Secrets not found in Key Vault
**Symptom**: `verify-secrets.sh` reports missing secrets
**Solution**: Add secrets using commands provided by script output
**Reference**: [Quick Start - If Secrets Don't Exist](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md#if-secrets-dont-exist)

### Issue: Deployment fails after workflow change
**Symptom**: GitHub Actions workflow fails
**Solution**: Check workflow syntax, revert if needed
**Reference**: [Quick Start - Rollback](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md#rollback-if-something-goes-wrong)

### Issue: Emails still not sending after fix
**Symptom**: Email send failures in logs
**Possible Causes**:
1. Invalid connection string format
2. Sender address not verified
3. Rate limiting (free tier: 500/month)
4. Invalid recipient email
**Solution**: Check container app logs for specific error
**Reference**: [Quick Start - Troubleshooting](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md#troubleshooting)

### Issue: Container app not picking up new config
**Symptom**: Environment variables show old values
**Solution**: Verify deployment completed, check `az containerapp show`
**Reference**: [Quick Start - Verification](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md#verification-commands)

---

## FAQs

### Q: Why not just use SMTP?
**A**: Azure Communication Services provides:
- Better Azure integration
- Automatic SPF/DKIM configuration
- Better deliverability (Azure reputation)
- Free tier adequate for our volume
- No need to manage SMTP credentials
- Built-in rate limiting and spam protection

### Q: Can we keep SMTP as fallback?
**A**: Yes, but adds complexity. Current recommendation is Azure-only for simplicity. If business requires fallback, we can implement but need to:
- Update EmailService to handle fallback logic
- Keep both sets of secrets in Key Vault
- Add monitoring for fallback usage

### Q: What if Azure Communication Services isn't provisioned yet?
**A**: Infrastructure team needs to provision it first. Timeline:
- Resource creation: 5 minutes
- Domain verification: 1-2 days (DNS propagation)
- Or use Azure-provided domain: Immediate (no verification needed)

### Q: Will this affect local development?
**A**: No. Local development uses `appsettings.Development.json` which may have different email configuration. This fix only affects staging and production.

### Q: What's the rollback plan if something goes wrong?
**A**: Multiple options:
1. Revert git commit (easiest)
2. Manually update container app env vars to SMTP
3. Activate previous container app revision
See: [Quick Start - Rollback](./EMAIL_DEPLOYMENT_FIX_QUICKSTART.md#rollback-if-something-goes-wrong)

### Q: How do we prevent this from happening again?
**A**: Post-fix improvements:
1. Add configuration validation at app startup
2. Add deployment validation that checks required secrets exist
3. Document configuration in ADR
4. Add smoke tests for email in deployment workflow
5. Training on ASP.NET Core configuration hierarchy

---

## Next Steps

### Immediate Actions Required

**Infrastructure Team:**
- [ ] Check if Azure Communication Services exists
- [ ] If yes: Provide connection string and sender address
- [ ] If no: Provision resource and configure domain verification

**DevOps Team (after infrastructure provides secrets):**
- [ ] Add secrets to staging Key Vault
- [ ] Add secrets to production Key Vault
- [ ] Verify secrets with `verify-secrets.sh`
- [ ] Notify development team

**Development Team (after DevOps adds secrets):**
- [ ] Run `verify-secrets.sh` to confirm
- [ ] Apply fix with `apply-fixes.sh`
- [ ] Review changes and commit
- [ ] Deploy and test
- [ ] Verify email functionality

### Follow-up Actions (After Fix Deployed)

**Short-term:**
- [ ] Post-mortem review meeting
- [ ] Update deployment runbooks
- [ ] Add email configuration to monitoring
- [ ] Document lessons learned

**Long-term:**
- [ ] Implement configuration validation
- [ ] Add deployment validation for required secrets
- [ ] Create Architecture Decision Record
- [ ] Add smoke tests to deployment pipeline
- [ ] Training session on configuration management

---

## Contact & Escalation

### For Questions About This Issue

**Technical Questions:**
- System Architect: [Available for consultation]
- Development Team Lead: [Available for implementation questions]

**Infrastructure Questions:**
- Azure Team: [Azure Communication Services provisioning]
- Platform Team: [Key Vault and resource management]

**Process Questions:**
- DevOps Lead: [Deployment and secrets management]
- Engineering Manager: [Timeline and resource allocation]

### Escalation Path

**If blocked for > 4 hours:**
1. Escalate to Engineering Manager
2. Engineering Manager coordinates with Infrastructure/Platform leads
3. If business-critical, consider temporary SMTP rollback while awaiting Azure credentials

---

## Document Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-12-19 | Initial documentation created | System Architect |
| | | RCA completed | |
| | | Fix plan documented | |
| | | Scripts and tools created | |
| | | Visual diagrams added | |

---

## Related Issues

**Completed (Related):**
- Email.cs regex fix: ✅ Deployed 2025-12-17
- Email template migration: ✅ Complete

**Current:**
- Email provider configuration: ⏸️ THIS ISSUE

**Future (Potential):**
- Email configuration validation
- Deployment secret validation
- Email smoke tests in pipeline

---

**Current Status**: Ready to proceed once Azure Communication Services secrets are provided by infrastructure team.

**Last Updated**: 2025-12-19
**Next Review**: After fix is deployed
