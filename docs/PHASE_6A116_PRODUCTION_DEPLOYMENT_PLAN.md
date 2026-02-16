# Phase 6A.116 & 6A.117: Production Deployment Plan

**Date**: 2026-02-16
**Current Status**: ✅ Staging Complete | ❌ Production Pending
**PR**: #82 (OPEN - awaiting merge)

---

## Current Status Summary

### ✅ Staging Environment (COMPLETE)
- **Backend**: All code deployed
- **Migrations**: Both Phase6A116 & Phase6A117 applied successfully
- **Verification**: Deployment logs confirmed at 18:20:10 UTC
- **Testing**: User testing guide provided

### ❌ Production Environment (PENDING)
- **Backend**: Old code (commit d4373e12 from PR #76)
- **Migrations**: Phase6A116 & Phase6A117 NOT applied
- **Last Deployment**: 2026-02-15 18:58:26 UTC (before our fixes)
- **Status**: Awaiting PR #82 merge to main

---

## Why Production Doesn't Have the Fixes Yet

1. **PR #82 Not Merged**: Changes are on develop branch, not main
2. **Main Branch Protected**: Requires PR approval and merge
3. **Automatic Deployment**: deploy-production.yml only runs on main branch updates

---

## Production Deployment Workflow

### Step 1: Staging Verification (CURRENT STEP)
**Status**: ⏳ **USER TESTING IN PROGRESS**

**Actions Required:**
- [ ] Run comprehensive testing guide: `scripts/test_phase6a116_emails.md`
- [ ] Verify all 9 issues fixed in staging
- [ ] Test email rendering (placeholders, line breaks, URLs)
- [ ] Test anonymous user token authentication
- [ ] Check signup list commitment emails (no "feel free" text, clean layout)

**Success Criteria:**
- All 7 test scenarios pass
- Zero email rendering issues
- Token authentication works cross-browser
- Email layout clean and professional

---

### Step 2: PR Review and Approval
**Status**: ⏳ **PENDING USER APPROVAL**

**PR #82 Details:**
- **Title**: Phase 6A.116 & 6A.117: Critical Email Fixes + WWW Redirect
- **URL**: https://github.com/Niroshana-SinharaRalalage/LankaConnect/pull/82
- **State**: OPEN
- **Branch**: develop → main
- **Commits**: 7 commits including 2 migrations
- **Files Changed**: 13 files (backend, frontend, docs)

**Review Checklist:**
- [ ] Review PR description (comprehensive)
- [ ] Review code changes (11 backend files, 2 frontend files)
- [ ] Review migration SQL (Phase6A116 & Phase6A117)
- [ ] Approve PR
- [ ] Merge to main

---

### Step 3: Automatic Production Deployment
**Status**: ⏳ **WILL RUN AFTER MERGE**

**GitHub Actions Workflow**: `.github/workflows/deploy-production.yml`

**Deployment Steps (Automatic):**
1. Trigger on main branch push
2. Build .NET application
3. Run database migrations:
   ```yaml
   - name: Apply Database Migrations
     run: |
       cd src/LankaConnect.Infrastructure
       dotnet ef database update --context AppDbContext
   ```
4. Build Docker image
5. Push to Azure Container Registry
6. Deploy to lankaconnect-api-prod
7. Health check verification

**Expected Duration**: ~8-10 minutes

**Migration SQL to be Applied:**

**Phase6A116_FixEmailTemplateHtmlRendering:**
```sql
UPDATE communications.email_templates
SET html_template = REPLACE(html_template, '{{ResponseSummary}}', '{{{ResponseSummary}}}'),
    updated_at = NOW()
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
);
```

**Phase6A117_FixEmailTemplateTextAndLayout:**
```sql
-- Remove "feel free to reply" text
UPDATE communications.email_templates
SET html_template = REPLACE(
        html_template,
        '<br />If you have questions, feel free to reply to this email.',
        ''
    ),
    updated_at = NOW()
WHERE name IN (
    'template-event-registration-cancellation',
    'template-event-reminder',
    'template-signup-list-commitment-update'
)
AND html_template LIKE '%If you have questions, feel free to reply to this email.%';

-- Remove empty PICKUP/DELIVERY card
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
        html_template,
        '<!-- PICKUP/DELIVERY CARD -->[\s\S]*?</table>[\s\S]*?<!--\[if mso\]>[\s\S]*?<!\[endif\]-->[\s\S]*?</td>[\s\S]*?</tr>',
        '',
        'g'
    ),
    updated_at = NOW()
WHERE name IN (
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
)
AND html_template LIKE '%PICKUP/DELIVERY CARD%';
```

---

### Step 4: Production Verification
**Status**: ⏳ **PENDING DEPLOYMENT**

**Verification Script**: `scripts/check_production_migrations.sh`

**Steps:**
1. Wait for deployment completion (~10 min)
2. Connect to production container:
   ```bash
   az containerapp exec \
     --name lankaconnect-api-prod \
     --resource-group LankaConnect \
     --command '/bin/bash'
   ```
3. Check migration history:
   ```bash
   echo $DATABASE_URL | xargs -I {} psql {} -c "
   SELECT \"MigrationId\", \"ProductVersion\"
   FROM \"__EFMigrationsHistory\"
   WHERE \"MigrationId\" LIKE '%Phase6A11%'
   ORDER BY \"MigrationId\" DESC;
   "
   ```
4. Expected output:
   ```
   20260216181052_Phase6A117_FixEmailTemplateTextAndLayout | 8.0.19
   20260216033407_Phase6A116_FixEmailTemplateHtmlRendering | 8.0.19
   ```

**Production Testing Checklist:**
- [ ] Verify migrations applied
- [ ] Check Azure Container App logs (no errors)
- [ ] Test form response email (all placeholders replaced)
- [ ] Test HTML line breaks render correctly
- [ ] Test edit button URLs (no 404s)
- [ ] Test signup list commitment emails (clean layout)
- [ ] Test anonymous user token auth (cross-browser)

---

## Migration Verification Commands

### Check Staging (Already Applied)
```bash
# Connect to staging
az containerapp exec \
  --name lankaconnect-api-staging \
  --resource-group LankaConnect \
  --command '/bin/bash'

# Check migrations
echo $DATABASE_URL | xargs -I {} psql {} -c "
SELECT \"MigrationId\", TO_CHAR(\"ProductVersion\", '999.999') as Version,
       TO_CHAR(applied_at, 'YYYY-MM-DD HH24:MI:SS') as applied_time
FROM \"__EFMigrationsHistory\"
WHERE \"MigrationId\" LIKE '%Phase6A11%'
ORDER BY \"MigrationId\" DESC;
"
```

**Expected Staging Output:**
```
20260216181052_Phase6A117_FixEmailTemplateTextAndLayout | 8.0.19 | 2026-02-16 18:20:10
20260216033407_Phase6A116_FixEmailTemplateHtmlRendering | 8.0.19 | 2026-02-16 18:20:10
```

### Check Production (After Deployment)
Same commands, but use:
```bash
az containerapp exec \
  --name lankaconnect-api-prod \
  --resource-group LankaConnect \
  --command '/bin/bash'
```

---

## Rollback Plan (If Issues Found)

### If Production Deployment Fails

**Option 1: Revert Migrations**
```bash
# Inside production container
cd /app
dotnet ef database update 20260215XXXXXX_PreviousMigration --context AppDbContext
```

**Option 2: Revert Code Deployment**
```bash
# Re-deploy previous main commit
git revert <merge-commit-sha>
git push origin main
# GitHub Actions will deploy old version
```

### If Email Issues Found in Production

**Immediate Actions:**
1. Check Azure Container App logs for errors
2. Verify migration applied correctly
3. Test specific email template causing issues
4. Check database email_templates table

**Quick Fixes:**
- If template issue: Update via SQL directly (emergency only)
- If code issue: Create hotfix PR with targeted fix
- If migration issue: Run migration manually in container

---

## Timeline Estimate

| Step | Duration | Status |
|------|----------|--------|
| **Staging Testing** | 30 min | ⏳ In Progress |
| **PR Review & Merge** | 15 min | ⏳ Pending |
| **Auto Deployment** | 10 min | ⏳ Pending |
| **Production Verification** | 20 min | ⏳ Pending |
| **Production Testing** | 30 min | ⏳ Pending |
| **Total** | **~2 hours** | |

---

## Success Criteria

### Deployment Successful When:
- ✅ PR #82 merged to main
- ✅ GitHub Actions deploy-production.yml completes successfully
- ✅ Both migrations appear in __EFMigrationsHistory table
- ✅ Azure Container App logs show no errors
- ✅ All 9 issues verified fixed in production
- ✅ Production emails render correctly
- ✅ No new issues introduced

---

## Communication Plan

**Before Merge:**
- Notify team of upcoming production deployment
- Share testing results from staging
- Set maintenance window if needed (none required - zero downtime)

**During Deployment:**
- Monitor GitHub Actions workflow
- Watch Azure Container App logs
- Be ready to rollback if issues detected

**After Deployment:**
- Verify all migrations applied
- Run production smoke tests
- Update documentation
- Close PR #82
- Update PROGRESS_TRACKER.md with production status

---

## Risk Assessment

**Risk Level**: 🟡 **LOW-MEDIUM**

**Risks:**
1. **Email template corruption** - Mitigated by tested SQL in staging
2. **Migration failure** - Mitigated by reversible migrations
3. **Deployment failure** - Mitigated by automatic rollback in GitHub Actions
4. **New bugs introduced** - Mitigated by comprehensive staging testing

**Mitigation Strategies:**
- Migrations tested in staging (verified at 18:20:10 UTC)
- Reversible Down() methods implemented
- Comprehensive testing guide followed
- Azure deployment logs monitored
- Rollback plan ready

---

## Post-Deployment Actions

**Immediate (Within 1 hour):**
- [ ] Verify migrations in production database
- [ ] Test all email types in production
- [ ] Check Azure logs for errors
- [ ] Update PROGRESS_TRACKER.md

**Follow-up (Within 24 hours):**
- [ ] Monitor production error rates
- [ ] Check user feedback
- [ ] Verify email delivery success rates
- [ ] Close related GitHub issues

**Documentation:**
- [ ] Mark Phase 6A.116 & 6A.117 as COMPLETE in all tracking docs
- [ ] Archive RCA documents
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Add lessons learned to memory

---

## Contact Information

**If Issues Occur:**
1. Check Azure Portal logs
2. Review migration verification output
3. Consult RCA documents:
   - `docs/RCA_PHASE_6A115_POST_DEPLOYMENT_COMPREHENSIVE_ANALYSIS.md`
   - `docs/RCA_PHASE_6A116_ISSUES_10_11_12.md`
4. Review testing guide: `scripts/test_phase6a116_emails.md`

---

**Document Created**: 2026-02-16
**Last Updated**: 2026-02-16
**Status**: Ready for Production Deployment
**Next Action**: User to approve and merge PR #82
