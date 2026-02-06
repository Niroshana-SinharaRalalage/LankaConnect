# GitHub Approval Workflow Setup Guide

## Visual CI/CD Pipeline: Staging → Approval → Production

This guide shows you how to set up a **visual approval workflow** in GitHub where you:
1. Test everything in staging
2. Click "Approve" button in GitHub UI
3. Production deployment proceeds automatically

---

## Setup: GitHub Environments (5 minutes)

### Step 1: Create Production Environment with Approval

```bash
# Go to GitHub repository settings
https://github.com/YOUR_ORG/LankaConnect/settings/environments
```

**In GitHub UI:**

1. **Settings** → **Environments** → **New environment**
2. Name: `production-approval`
3. **Configure environment:**
   - ✅ **Required reviewers:** Add yourself (and team members)
   - ✅ **Wait timer:** 0 minutes (or set delay if you want)
   - ✅ **Deployment branches:** Only `main` branch
   - **Save protection rules**

### Step 2: Visual Representation

```
┌─────────────────────────────────────────────────────────────┐
│  GitHub Actions UI                                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✅ Validate Staging Environment  (completed)              │
│      ├─ Check staging UI health: ✅ Healthy                │
│      ├─ Check staging API health: ✅ Healthy               │
│      └─ Staging version: abc123                            │
│                                                             │
│  ⏸️  Request Production Deployment Approval  (waiting)     │
│      │                                                      │
│      │  ╔═══════════════════════════════════════════════╗  │
│      │  ║  🔔 Approval Required                        ║  │
│      │  ║                                               ║  │
│      │  ║  Staging validated successfully!             ║  │
│      │  ║                                               ║  │
│      │  ║  Before approving:                           ║  │
│      │  ║  • Test login in staging                     ║  │
│      │  ║  • Verify events work                        ║  │
│      │  ║  • Check payments (test mode)                ║  │
│      │  ║                                               ║  │
│      │  ║  Staging URLs:                               ║  │
│      │  ║  UI: https://staging.example.com             ║  │
│      │  ║  API: https://api-staging.example.com        ║  │
│      │  ║                                               ║  │
│      │  ║  [✅ Approve]  [❌ Reject]                   ║  │
│      │  ╚═══════════════════════════════════════════════╝  │
│      │                                                      │
│  ⏳ Deploy Backend to Production  (waiting for approval)   │
│  ⏳ Deploy Frontend to Production  (waiting for approval)  │
│  ⏳ Validate Production Deployment  (waiting)              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Step 3: How to Use the Workflow

#### Option A: Automatic (Recommended)

```bash
# When you're ready to deploy to production:
git checkout main
git merge develop
git push origin main

# GitHub Actions automatically:
# 1. Validates staging is healthy
# 2. Shows approval button in GitHub UI
# 3. Sends you notification (if configured)
# 4. Waits for your approval
```

#### Option B: Manual Trigger

```bash
# Go to GitHub Actions tab:
https://github.com/YOUR_ORG/LankaConnect/actions/workflows/deploy-production-with-approval.yml

# Click "Run workflow"
# Select:
#   - Branch: main
#   - Target environment: production
#   - Skip backend: No (default)
#   - Skip frontend: No (default)
# Click "Run workflow"
```

### Step 4: Approving Deployment (GitHub UI)

1. **Go to Actions tab** in GitHub
2. **Find the running workflow** (will show "Waiting")
3. **Click on the workflow run**
4. **You'll see:** "Review pending deployments"
5. **Click "Review deployments"**
6. **Check:** "production-approval"
7. **Add comment** (optional): "Staging tested, looks good!"
8. **Click "Approve and deploy"**

**Screenshot of what you'll see:**

```
┌─────────────────────────────────────────────────────┐
│  Review pending deployments                         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ☐ production-approval                             │
│                                                     │
│  Comment (optional):                               │
│  ┌─────────────────────────────────────────────┐  │
│  │ Staging tested, all features working!       │  │
│  └─────────────────────────────────────────────┘  │
│                                                     │
│  [Approve and deploy]  [Reject]                    │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Step 5: After Approval

```
Timeline after clicking "Approve":

0:00  ✅ Approval granted
0:01  🚀 Deploy Backend to Production (starts)
0:01  🚀 Deploy Frontend to Production (starts in parallel)
6:00  ✅ Backend deployed (blue-green complete)
4:00  ✅ Frontend deployed (blue-green complete)
7:00  🔍 Validate Production Deployment (starts)
7:30  ✅ Production health checks pass
7:31  🎉 Deployment complete!
```

---

## Complete Workflow Architecture

### Current Setup

```
Repository Branches:
├── develop (staging branch)
│   └── Auto-deploys to staging via:
│       ├── .github/workflows/deploy-staging.yml (backend)
│       └── .github/workflows/deploy-ui-staging.yml (frontend)
│
└── main (production branch)
    └── Deploys to production with approval via:
        └── .github/workflows/deploy-production-with-approval.yml
            ├── Validates staging
            ├── Requests approval (manual)
            ├── Deploys backend (reuses deploy-production.yml)
            └── Deploys frontend (reuses deploy-ui-production.yml)
```

### Workflow Files

```
.github/workflows/
├── deploy-staging.yml              ← Auto-deploy backend to staging
├── deploy-ui-staging.yml           ← Auto-deploy frontend to staging
├── deploy-production.yml           ← Backend production deployment (reusable)
├── deploy-ui-production.yml        ← Frontend production deployment (reusable)
└── deploy-production-with-approval.yml  ← NEW! Approval workflow
```

---

## Comparison: Approaches

### Approach 1: Current (Automatic) ❌

```
develop → Auto-deploy to staging
main → Auto-deploy to production (NO APPROVAL!)

Risk: Accidental production deployment
```

### Approach 2: With Approval (Recommended) ✅

```
develop → Auto-deploy to staging
main → Validate staging → Request approval → Deploy to production

Benefits:
✅ Visual approval in GitHub UI
✅ Pre-deployment validation
✅ Time to test staging thoroughly
✅ Prevents accidental deployments
✅ Audit trail (who approved when)
```

---

## Notification Setup (Optional)

### Slack Notifications

Add to workflow to get Slack notifications:

```yaml
- name: Notify Slack on approval request
  run: |
    curl -X POST -H 'Content-Type: application/json' \
      -d '{
        "text": "🚀 Production deployment approval requested",
        "attachments": [{
          "color": "warning",
          "fields": [
            {"title": "Commit", "value": "${{ github.sha }}", "short": true},
            {"title": "Branch", "value": "${{ github.ref_name }}", "short": true},
            {"title": "Triggered by", "value": "${{ github.actor }}", "short": true}
          ],
          "actions": [{
            "type": "button",
            "text": "Review Deployment",
            "url": "https://github.com/${{ github.repository }}/actions/runs/${{ github.run_id }}"
          }]
        }]
      }' \
      ${{ secrets.SLACK_WEBHOOK_URL }}
```

### Email Notifications

GitHub sends email automatically when:
- Approval is requested
- Deployment completes
- Deployment fails

**No setup needed!** Just ensure your GitHub notifications are enabled.

---

## Testing the Approval Workflow

### Test Run (Dry Run)

```bash
# 1. Make a small change in develop
echo "# Test" >> README.md
git add README.md
git commit -m "test: trigger approval workflow"

# 2. Merge to main
git checkout main
git merge develop
git push origin main

# 3. Watch GitHub Actions
# Open: https://github.com/YOUR_ORG/LankaConnect/actions

# 4. You should see:
# - "Deploy to Production (With Approval)" workflow running
# - Status: "Waiting" (orange dot)
# - "Review pending deployments" button

# 5. Click review, approve
# - Watch deployment proceed
# - Should complete in ~6-7 minutes

# 6. Verify production
curl https://lankaconnect-api-prod.eastus.azurecontainerapps.io/health
```

---

## Security & Best Practices

### Required Reviewers

**Recommended Setup:**

```
Environment: production-approval
Required reviewers: 2 (you + 1 other person)

This ensures:
✅ No solo deployments
✅ Code review before production
✅ Two-person rule for critical changes
```

### Branch Protection

**Protect main branch:**

```
Settings → Branches → Branch protection rules

Rule for: main

Required:
☑ Require pull request reviews before merging
☑ Require status checks to pass (staging deployments)
☑ Require conversation resolution before merging
☐ Require deployments to succeed before merging
☑ Do not allow bypassing the above settings
```

---

## Rollback During Approval Workflow

### If Issues Found During Approval Review

**Option 1: Reject Deployment**
```
1. Click "Reject" in approval dialog
2. Fix issues in develop
3. Test in staging
4. Merge to main again
```

**Option 2: Cancel Workflow**
```
1. Go to Actions tab
2. Click on running workflow
3. Click "Cancel workflow"
```

### If Issues Found After Deployment

Use standard rollback:
```bash
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <OLD_REVISION>=100
```

---

## Monitoring Approval Workflow

### GitHub Actions Insights

```
View deployment history:
https://github.com/YOUR_ORG/LankaConnect/deployments

Shows:
- All production deployments
- Who approved each deployment
- Deployment duration
- Success/failure rate
```

### Audit Trail

Every deployment is logged:
- Commit SHA
- Who triggered (github.actor)
- Who approved (reviewer)
- Timestamp
- Environment (staging/production)

---

## Summary: Complete Setup Checklist

- [ ] **Create GitHub Environment:** `production-approval` with required reviewers
- [ ] **Update workflow files:** Use new approval workflow
- [ ] **Configure branch protection:** Protect main branch
- [ ] **Test approval flow:** Dry run with small change
- [ ] **Configure notifications:** Slack/email (optional)
- [ ] **Document for team:** Share this guide
- [ ] **Train team members:** How to approve deployments

---

## Quick Reference

### Deploy to Production (3 Steps)

```bash
# Step 1: Test thoroughly in staging
https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io

# Step 2: Merge to main
git checkout main && git merge develop && git push origin main

# Step 3: Approve in GitHub UI
https://github.com/YOUR_ORG/LankaConnect/actions
→ Find running workflow
→ Click "Review deployments"
→ Check "production-approval"
→ Click "Approve and deploy"

# Done! Production deploys automatically after approval
```

### Common Commands

```bash
# View pending deployments
gh run list --workflow="deploy-production-with-approval.yml"

# Approve from CLI (requires gh extension)
gh workflow run deploy-production-with-approval.yml \
  --ref main \
  --field environment=production

# Check deployment status
gh run view <RUN_ID>

# Cancel deployment
gh run cancel <RUN_ID>
```

---

**Status:** Ready to use ✅
**Setup Time:** 5 minutes
**Benefits:** Visual approval, safety, audit trail