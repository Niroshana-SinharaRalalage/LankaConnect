# Workflow Implementation Guide: Smart Triggers

## Current Workflow Files (What You Already Have)

```
.github/workflows/
├── deploy-staging.yml              # Backend to staging
├── deploy-ui-staging.yml           # Frontend to staging
├── deploy-production.yml           # Backend to production
└── deploy-ui-production.yml        # Frontend to production
```

**Total: 4 files** (already exists!)

---

## What We Need to Add (Minimal Changes)

```
.github/workflows/
├── deploy-staging.yml              # ← UPDATE: Add branch parameter
├── deploy-ui-staging.yml           # ← UPDATE: Add branch parameter
├── deploy-production.yml           # ← KEEP: No changes
├── deploy-ui-production.yml        # ← KEEP: No changes
└── deploy-production-with-approval.yml  # ← NEW: Approval workflow
```

**Total: 5 files** (only 1 new file + 2 small updates!)

---

## How It Works: Smart Triggers

### Scenario 1: Normal Development (develop → staging)

**No changes needed! Already works!**

```yaml
# deploy-staging.yml (current)
on:
  push:
    branches:
      - develop    # ← Auto-deploys when develop updates

# When you push to develop:
git push origin develop
# ✅ Auto-deploys to staging (existing behavior)
```

### Scenario 2: Test Main Branch in Staging (manual)

**Update existing file to accept branch parameter:**

```yaml
# deploy-staging.yml (UPDATED)
on:
  push:
    branches:
      - develop    # ← Still auto-deploys develop
  workflow_dispatch:  # ← ADD: Manual trigger
    inputs:
      branch:
        description: 'Branch to deploy (for testing main)'
        required: false
        default: 'develop'
        type: string

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          ref: ${{ inputs.branch || 'develop' }}  # ← Use input or default to develop

      # ... rest of deployment steps (same as before)
```

**How to use:**

```bash
# Option A: Automatic (default behavior)
git push origin develop
# ✅ Auto-deploys develop to staging

# Option B: Manual (test main in staging)
# Go to GitHub Actions → deploy-staging.yml → "Run workflow"
# Select branch: main
# Click "Run workflow"
# ✅ Deploys main to staging for testing
```

### Scenario 3: Production Deployment (main → approval → production)

**Use new approval workflow:**

```yaml
# deploy-production-with-approval.yml (NEW)
on:
  push:
    branches:
      - main    # ← Triggers when main updates

jobs:
  validate-staging:
    # ... checks staging health

  request-approval:
    environment: production-approval  # ← Manual approval here
    # ... waits for your approval

  deploy-backend:
    uses: ./.github/workflows/deploy-production.yml  # ← Reuses existing file!
    secrets: inherit

  deploy-frontend:
    uses: ./.github/workflows/deploy-ui-production.yml  # ← Reuses existing file!
    secrets: inherit
```

**How to use:**

```bash
# Merge to main
git checkout main
git merge develop
git push origin main

# GitHub Actions automatically:
# 1. Validates staging
# 2. Shows "Approve" button
# 3. You click "Approve"
# 4. Deploys to production
```

### Scenario 4: Hotfix (hotfix → staging → production → develop)

**Same workflows, different branch:**

```bash
# Step 1: Create hotfix from main
git checkout main
git pull origin main
git checkout -b hotfix/critical-bug

# Step 2: Fix the bug
# ... code changes ...
git commit -m "fix: critical bug"
git push origin hotfix/critical-bug

# Step 3: Deploy hotfix to staging for testing
# Go to GitHub Actions → deploy-staging.yml → "Run workflow"
# Branch: hotfix/critical-bug
# ✅ Deploys to staging

# Step 4: Test in staging
# ... verify fix works ...

# Step 5: Merge to main
git checkout main
git merge hotfix/critical-bug
git push origin main
# ✅ Triggers approval workflow → production

# Step 6: Sync back to develop
git checkout develop
git merge hotfix/critical-bug
git push origin develop
# ✅ Keeps develop in sync
```

---

## Complete Workflow Files (Final State)

### File 1: deploy-staging.yml (UPDATED - Add 5 lines)

```yaml
name: Deploy to Azure Staging

on:
  push:
    branches:
      - develop
  workflow_dispatch:  # ← ADD THIS
    inputs:           # ← ADD THIS
      branch:         # ← ADD THIS
        description: 'Branch to deploy (default: develop)'  # ← ADD THIS
        default: 'develop'  # ← ADD THIS
        type: string        # ← ADD THIS

env:
  AZURE_CONTAINER_REGISTRY: lankaconnectstaging
  CONTAINER_APP_NAME: lankaconnect-api-staging
  RESOURCE_GROUP: lankaconnect-staging
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          ref: ${{ inputs.branch || github.ref }}  # ← UPDATE THIS LINE

      # ... rest of file stays the same (no changes)
```

**Changes:** Added 6 lines total (workflow_dispatch section + ref parameter)

### File 2: deploy-ui-staging.yml (UPDATED - Add 5 lines)

```yaml
name: Deploy UI to Azure Staging

on:
  push:
    branches:
      - develop
    paths:
      - 'web/**'
      - '.github/workflows/deploy-ui-staging.yml'
  workflow_dispatch:  # ← ADD THIS
    inputs:           # ← ADD THIS
      branch:         # ← ADD THIS
        description: 'Branch to deploy (default: develop)'  # ← ADD THIS
        default: 'develop'  # ← ADD THIS
        type: string        # ← ADD THIS

env:
  AZURE_CONTAINER_REGISTRY: lankaconnectstaging
  CONTAINER_APP_NAME: lankaconnect-ui-staging
  RESOURCE_GROUP: lankaconnect-staging
  IMAGE_NAME: lankaconnect-ui
  NODE_VERSION: '20'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./web

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          ref: ${{ inputs.branch || github.ref }}  # ← UPDATE THIS LINE

      # ... rest of file stays the same (no changes)
```

**Changes:** Added 6 lines total (workflow_dispatch section + ref parameter)

### File 3: deploy-production-with-approval.yml (NEW)

Already created in previous response! No changes needed.

### Files 4 & 5: deploy-production.yml, deploy-ui-production.yml (NO CHANGES)

Keep as-is! They're reused by the approval workflow.

---

## Visual Flow Diagram

### How All Workflows Connect

```
┌─────────────────────────────────────────────────────────────┐
│  Git Branches → Workflow Triggers                           │
└─────────────────────────────────────────────────────────────┘

develop branch:
  ├─ Push to develop
  │   └─→ deploy-staging.yml (auto)
  │       └─→ Deploys to staging
  │
  └─ Merge to main (via PR)
      └─→ main branch (see below)

main branch:
  ├─ Push to main
  │   └─→ deploy-production-with-approval.yml (auto)
  │       ├─→ Validates staging
  │       ├─→ Requests approval (manual)
  │       └─→ Calls deploy-production.yml + deploy-ui-production.yml
  │           └─→ Deploys to production
  │
  └─ Manual trigger (for testing in staging)
      └─→ deploy-staging.yml (manual)
          └─→ Deploys main to staging

hotfix/* branch:
  ├─ Manual trigger (for testing)
  │   └─→ deploy-staging.yml (manual)
  │       └─→ Deploys hotfix to staging
  │
  └─ Merge to main
      └─→ deploy-production-with-approval.yml (auto)
          └─→ Deploys to production
```

---

## Complete Scenarios with Exact Commands

### Scenario 1: Normal Feature Development (90% of cases)

```bash
# ===================================================================
# Developer workflow:
# ===================================================================

# 1. Create feature
git checkout develop
git pull origin develop
git checkout -b feature/new-feature

# 2. Develop and commit
# ... code changes ...
git add .
git commit -m "feat: add new feature"

# 3. Merge to develop
git checkout develop
git merge feature/new-feature
git push origin develop

# ✅ AUTO-DEPLOY: deploy-staging.yml triggers automatically
# ✅ Test at: https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io

# 4. When ready for production
# Go to GitHub → Create PR: develop → main
# Review, approve, merge

# 5. Push to main
git checkout main
git pull origin main
git push origin main

# ✅ AUTO-TRIGGER: deploy-production-with-approval.yml
# ✅ GitHub shows "Approve deployment" button
# ✅ Click approve → production deploys
```

**Workflows used:**
- `deploy-staging.yml` (auto-triggered by develop push)
- `deploy-production-with-approval.yml` (auto-triggered by main push)

**No manual workflow triggers needed!**

### Scenario 2: Critical Release (Test main in staging first)

```bash
# ===================================================================
# After merging develop → main (conflicts resolved)
# ===================================================================

# 1. Main branch updated
git checkout main
git pull origin main

# 2. Manually deploy main to staging for testing
# Go to GitHub Actions:
# https://github.com/YOUR_ORG/LankaConnect/actions/workflows/deploy-staging.yml

# Click "Run workflow"
# Branch: main  ← SELECT main instead of develop
# Click "Run workflow"

# ✅ MANUAL-TRIGGER: deploy-staging.yml deploys main to staging
# ✅ Test at: https://staging.lankaconnect.app

# 3. Test thoroughly (30 minutes)
# - Test login
# - Test events
# - Test payments
# - Check logs

# 4. If all tests pass, approve production
# Go to GitHub Actions:
# https://github.com/YOUR_ORG/LankaConnect/actions

# Find "Deploy to Production (With Approval)" workflow
# Click "Review deployments"
# Click "Approve and deploy"

# ✅ AUTO-DEPLOY: Production deployment proceeds
```

**Workflows used:**
- `deploy-staging.yml` (manually triggered with branch=main)
- `deploy-production-with-approval.yml` (auto-triggered by main push)

**Manual trigger only for testing main in staging!**

### Scenario 3: Hotfix (Emergency Production Fix)

```bash
# ===================================================================
# Emergency fix needed in production
# ===================================================================

# 1. Create hotfix branch from main
git checkout main
git pull origin main
git checkout -b hotfix/fix-payment-bug

# 2. Fix the bug
# ... code changes ...
git add .
git commit -m "fix: payment processing bug"
git push origin hotfix/fix-payment-bug

# 3. Test hotfix in staging
# Go to GitHub Actions → deploy-staging.yml → "Run workflow"
# Branch: hotfix/fix-payment-bug
# Click "Run workflow"

# ✅ MANUAL-TRIGGER: Deploys hotfix to staging
# ✅ Test at: https://staging.lankaconnect.app

# 4. Verify fix works (quick test - 10 minutes)
# - Test payment flow
# - Verify bug is fixed

# 5. Merge to main for production
git checkout main
git merge hotfix/fix-payment-bug
git push origin main

# ✅ AUTO-TRIGGER: deploy-production-with-approval.yml
# ✅ Click "Approve" (fast-track for hotfix)
# ✅ Production deploys

# 6. Sync back to develop (important!)
git checkout develop
git merge hotfix/fix-payment-bug
git push origin develop

# ✅ Keeps develop in sync with production fix
```

**Workflows used:**
- `deploy-staging.yml` (manually triggered with branch=hotfix/*)
- `deploy-production-with-approval.yml` (auto-triggered by main push)

---

## Summary: Workflow Triggers

### Automatic Triggers (No Action Needed)

| Branch | Push Event | Workflow Triggered | Result |
|--------|------------|-------------------|---------|
| `develop` | Push | `deploy-staging.yml` | Auto-deploy to staging |
| `main` | Push | `deploy-production-with-approval.yml` | Request approval → production |

### Manual Triggers (Use GitHub UI)

| Action | Where | What Happens |
|--------|-------|--------------|
| Test main in staging | Actions → `deploy-staging.yml` → Run workflow → Branch: main | Deploys main to staging |
| Test hotfix in staging | Actions → `deploy-staging.yml` → Run workflow → Branch: hotfix/* | Deploys hotfix to staging |
| Test feature in staging | Actions → `deploy-staging.yml` → Run workflow → Branch: feature/* | Deploys feature to staging |

---

## Implementation Steps (What You Need to Do)

### Step 1: Update Existing Workflows (5 minutes)

```bash
# Edit these two files:
# 1. .github/workflows/deploy-staging.yml
# 2. .github/workflows/deploy-ui-staging.yml

# Add workflow_dispatch section (see code above)
# Update checkout step to use inputs.branch
```

### Step 2: Create Approval Workflow (Already Done)

```bash
# File already created:
# .github/workflows/deploy-production-with-approval.yml
```

### Step 3: Create GitHub Environment (5 minutes)

```bash
# Go to GitHub:
# Settings → Environments → New environment
# Name: production-approval
# Add yourself as required reviewer
# Save
```

### Step 4: Test the Workflows (15 minutes)

```bash
# Test 1: Normal develop → staging (should work already)
git push origin develop

# Test 2: Manual trigger (test main in staging)
# GitHub Actions → deploy-staging.yml → Run workflow → Branch: main

# Test 3: Approval workflow (test production deployment)
git checkout main
git merge develop
git push origin main
# Then approve in GitHub UI
```

---

## No Separate YML Files Needed! ✅

**You asked:** "Are we gonna have separate ymls like develop-staging, main-staging..etc?"

**Answer:** **NO!** ✅

Instead:
- ✅ **Reuse existing workflows** with smart triggers
- ✅ **One staging workflow** handles all branches (develop, main, hotfix, feature)
- ✅ **One production workflow** handles all production deployments
- ✅ **Total files:** 5 (4 existing + 1 new)

**Not needed:**
- ❌ develop-staging.yml
- ❌ main-staging.yml
- ❌ hotfix-staging.yml
- ❌ feature-staging.yml

---

## Quick Reference Card

### When to Use Which Workflow

| Scenario | Branch | Trigger | Workflow | Action |
|----------|--------|---------|----------|--------|
| **Normal dev** | develop | Auto (push) | deploy-staging.yml | None (auto) |
| **Test main** | main | Manual | deploy-staging.yml | Run workflow → branch: main |
| **Test hotfix** | hotfix/* | Manual | deploy-staging.yml | Run workflow → branch: hotfix/* |
| **Production** | main | Auto (push) | deploy-production-with-approval.yml | Click "Approve" |

### GitHub Actions UI Shortcuts

```
Manual Deploy to Staging:
https://github.com/YOUR_ORG/LankaConnect/actions/workflows/deploy-staging.yml
→ Click "Run workflow"
→ Select branch
→ Click "Run workflow"

Approve Production:
https://github.com/YOUR_ORG/LankaConnect/actions
→ Find "Deploy to Production (With Approval)"
→ Click workflow run
→ Click "Review deployments"
→ Check "production-approval"
→ Click "Approve and deploy"
```

---

## Benefits of This Approach

✅ **Minimal files** - 5 total (4 existing + 1 new)
✅ **Reusable** - Same workflows for all scenarios
✅ **Flexible** - Can test any branch in staging
✅ **Safe** - Approval required for production
✅ **Simple** - Easy to understand and maintain
✅ **Fast** - Auto-deploys for normal development
✅ **Controlled** - Manual testing when needed

---

**Status:** Implementation plan complete ✅
**Files to change:** 2 (add workflow_dispatch)
**Files to create:** 1 (approval workflow)
**Complexity:** Low (simple parameter passing)
**Maintenance:** Easy (DRY principle - no duplication)