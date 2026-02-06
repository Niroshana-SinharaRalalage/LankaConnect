# Safe Deployment Strategy: Test-After-Merge

## Problem Statement

**Risk:** Merging `develop → main` can introduce:
- Merge conflicts (resolved manually)
- Integration issues between features
- Incompatible changes from hotfixes
- Untested code combinations

**Current Gap:** The merged result in `main` is never tested before production deployment!

---

## Solution: Multi-Stage Testing with Staging-on-Main

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Development Flow with Safety Checks                            │
└─────────────────────────────────────────────────────────────────┘

1. Feature Development
   ├─ feature/A → develop
   ├─ feature/B → develop
   └─ develop → Deploy to STAGING-1 (auto)
       └─ Test features A + B together

2. Create Release (develop → main)
   ├─ Create PR: develop → main
   ├─ Code review + approval
   ├─ Resolve any merge conflicts
   └─ Merge to main

3. Test Merged Result (CRITICAL!)
   └─ main → Deploy to STAGING-2 (auto)
       └─ Test exact production code
       └─ All tests pass?
           ├─ YES → Approve production deployment
           └─ NO → Fix in develop, merge again

4. Production Deployment (Safe!)
   └─ main → Request approval → Deploy to PRODUCTION
       └─ Blue-green deployment with rollback
```

---

## Detailed Implementation

### Option A: Two Staging Environments (Safest) ✅

**Setup:**
```
STAGING-1 (develop branch)
  - Purpose: Test features as they're developed
  - URL: https://dev.staging.lankaconnect.app
  - Auto-deploy from: develop
  - Database: staging-dev database

STAGING-2 (main branch - pre-production)
  - Purpose: Test exact code going to production
  - URL: https://uat.lankaconnect.app (User Acceptance Testing)
  - Auto-deploy from: main
  - Database: staging-uat database (copy of production structure)

PRODUCTION (main branch - after approval)
  - Purpose: Serve real users
  - URL: https://lankaconnect.app
  - Deploy from: main (after manual approval)
  - Database: production database
```

**Cost Impact:**
```
Additional Cost for STAGING-2:
- Container Apps (2 apps): +$30-40/month
- Database (shared or separate): +$50/month (if separate)
- Total: +$80-90/month

Total staging cost: $270-310/month
Production cost: $150-180/month
TOTAL: $420-490/month

Still within budget for safety!
```

**Workflow:**
```bash
# Step 1: Develop features
git checkout -b feature/new-feature develop
# ... develop ...
git checkout develop
git merge feature/new-feature
git push origin develop
# ✅ Auto-deploys to STAGING-1
# ✅ Test at: https://dev.staging.lankaconnect.app

# Step 2: Create PR to main
# Go to GitHub, create PR: develop → main
# ✅ Code review
# ✅ Resolve conflicts if any
# ✅ Merge PR

# Step 3: Test merged result in STAGING-2
# (Automatically deploys when main updates)
# ✅ Test at: https://uat.lankaconnect.app
# ✅ Verify no integration issues
# ✅ Test exact production code

# Step 4: Approve production deployment
# Go to GitHub Actions
# ✅ Click "Approve deployment"
# ✅ Production deploys automatically
```

---

### Option B: Temporary Staging-on-Main (Cost-Effective) ✅

**Setup:**
```
STAGING (develop branch)
  - Auto-deploy from: develop
  - URL: https://staging.lankaconnect.app

STAGING-TEMP (main branch - on-demand)
  - Deploy from: main (only when needed)
  - URL: Same as staging (switch mode)
  - Cost: $0 (reuse same infrastructure)

PRODUCTION (main branch - after approval)
  - Deploy from: main (after testing)
  - URL: https://lankaconnect.app
```

**Workflow:**
```bash
# Step 1: Develop in staging (as usual)
git push origin develop
# ✅ Auto-deploys to staging

# Step 2: Create PR and merge to main
# ... create PR, review, merge ...

# Step 3: Manually deploy main to staging (test merged result)
# Trigger workflow manually:
gh workflow run deploy-staging.yml \
  --ref main \
  --field test_mode=true

# ✅ Deploys main branch to staging
# ✅ Test thoroughly
# ✅ Confirm no issues

# Step 4: Approve production deployment
# Go to GitHub Actions
# ✅ Click "Approve"
# ✅ Production deploys
```

**Pros:**
- ✅ No additional cost
- ✅ Tests exact production code
- ✅ Uses existing staging infrastructure

**Cons:**
- ⚠️ Manual step required
- ⚠️ Staging unavailable during main testing
- ⚠️ Can't test develop and main simultaneously

---

### Option C: Automated Testing in CI (Lightweight) ✅

**Setup:**
```
STAGING (develop branch)
  - Auto-deploy from: develop

INTEGRATION TESTS (main branch - in CI)
  - Run in GitHub Actions
  - No separate environment
  - Uses test database

PRODUCTION (main branch - after approval)
  - Deploy from: main
```

**Workflow:**
```yaml
# .github/workflows/validate-main-before-production.yml
name: Validate Main Branch

on:
  push:
    branches:
      - main

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Build backend
        run: dotnet build

      - name: Run integration tests
        run: dotnet test tests/LankaConnect.IntegrationTests

      - name: Build frontend
        run: cd web && npm ci && npm run build

      - name: Run E2E tests
        run: cd web && npm run test:e2e

  deployment-approval:
    needs: integration-tests
    if: success()
    # Only show approval after tests pass
    environment: production-approval
    runs-on: ubuntu-latest
    steps:
      - name: Request approval
        run: echo "All tests passed, ready for production"
```

**Pros:**
- ✅ No additional cost
- ✅ Automated testing
- ✅ Fast feedback

**Cons:**
- ⚠️ Tests run in CI, not real environment
- ⚠️ May miss environment-specific issues
- ⚠️ Not as thorough as real environment testing

---

## Recommended Solution: Hybrid Approach ✅

**Best of Both Worlds:**

```
Stage 1: Development Testing
  └─ develop → STAGING (auto-deploy)
      └─ Test features as developed

Stage 2: Integration Testing (CI)
  └─ main → Run integration tests in GitHub Actions
      └─ Catch merge conflicts and build issues

Stage 3: Pre-Production Testing (On-Demand)
  └─ main → STAGING (manual trigger if needed)
      └─ Test critical releases in real environment

Stage 4: Production Deployment
  └─ main → PRODUCTION (after approval)
      └─ Blue-green deployment with rollback
```

**Workflow:**
```bash
# ===================================================================
# Normal Flow (Non-Critical Changes)
# ===================================================================
1. develop → staging (auto)
2. Test in staging
3. develop → main (PR + merge)
4. main → Integration tests run automatically
5. Tests pass → Approve → Production

# ===================================================================
# Critical Flow (High-Risk Changes)
# ===================================================================
1. develop → staging (auto)
2. Test in staging
3. develop → main (PR + merge)
4. main → Integration tests run automatically
5. main → staging (manual deploy for extra testing)
6. Test merged result in staging
7. All tests pass → Approve → Production
```

---

## Handling Merge Conflicts Safely

### Scenario: Conflict During develop → main Merge

```bash
# ===================================================================
# Step 1: Create PR (discover conflicts early)
# ===================================================================
# In GitHub UI:
# 1. Create PR: develop → main
# 2. GitHub shows: "This branch has conflicts that must be resolved"

# ===================================================================
# Step 2: Resolve conflicts locally
# ===================================================================
git checkout main
git pull origin main
git checkout -b merge/develop-to-main
git merge develop

# Conflict appears:
# CONFLICT (content): Merge conflict in src/file.cs

# Resolve conflict manually:
# Edit file.cs, resolve conflicts
git add src/file.cs
git commit -m "fix: resolve merge conflicts from develop"

# ===================================================================
# Step 3: Test merged result BEFORE pushing to main
# ===================================================================
# Option 3a: Test locally
dotnet build
dotnet test
npm run build
npm run test

# Option 3b: Push to temporary branch and deploy to staging
git push origin merge/develop-to-main

# Manually trigger staging deployment from this branch:
gh workflow run deploy-staging.yml \
  --ref merge/develop-to-main

# Test at: https://staging.lankaconnect.app
# ✅ Verify no issues from merge

# ===================================================================
# Step 4: If tests pass, push to main
# ===================================================================
git checkout main
git merge merge/develop-to-main
git push origin main

# ===================================================================
# Step 5: Approve production deployment
# ===================================================================
# GitHub Actions shows approval request
# ✅ Click "Approve"
# ✅ Production deploys
```

---

## Safety Mechanisms

### 1. Automated Tests in CI

```yaml
# Required checks before merge to main:
- ✅ Build succeeds (backend + frontend)
- ✅ Unit tests pass (backend + frontend)
- ✅ Integration tests pass
- ✅ TypeScript type checking passes
- ✅ ESLint passes (if configured)
```

### 2. Branch Protection Rules

```
main branch protection:
☑ Require pull request before merging
☑ Require status checks to pass
  ☑ build-backend
  ☑ build-frontend
  ☑ test-unit-backend
  ☑ test-unit-frontend
☑ Require conversation resolution
☑ Require linear history (no force push)
☑ Include administrators (no bypass)
```

### 3. Deployment Approval Workflow

```yaml
# Only allow production deployment if:
1. ✅ All CI tests passed
2. ✅ Staging is healthy
3. ✅ Manual approval granted
4. ✅ No pending unresolved issues
```

### 4. Rollback Plan

```bash
# If production deployment fails or has issues:

# Quick rollback (<30 seconds):
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <OLD_REVISION>=100

# Or revert main branch:
git checkout main
git revert HEAD
git push origin main
# (Triggers new deployment with previous code)
```

---

## Complete Safe Deployment Workflow

### Visual Flow Diagram

```
┌────────────────────────────────────────────────────────────────┐
│  Safe Deployment Process                                       │
└────────────────────────────────────────────────────────────────┘

1. Feature Development
   ├─ feature/* → develop
   └─ ✅ Auto-deploy to staging
       └─ ✅ Test features

2. Create Release Candidate
   ├─ Create PR: develop → main
   ├─ ✅ Code review
   ├─ ✅ Resolve conflicts (if any)
   └─ ⏸️  DO NOT MERGE YET

3. Test Conflict Resolution (if conflicts occurred)
   ├─ Create temp branch: merge/develop-to-main
   ├─ Resolve conflicts
   ├─ ✅ Deploy to staging from temp branch
   └─ ✅ Test thoroughly

4. Merge to Main (only if Step 3 passed)
   ├─ Merge PR: develop → main
   └─ ✅ CI tests run automatically
       ├─ Build
       ├─ Unit tests
       ├─ Integration tests
       └─ Type checking

5. Pre-Production Testing (for critical changes)
   ├─ ✅ Manually deploy main to staging
   └─ ✅ Test exact production code

6. Production Approval
   ├─ ⏸️  Manual approval in GitHub
   └─ ✅ Approve deployment

7. Production Deployment
   ├─ ✅ Blue-green deployment
   ├─ ✅ Canary rollout (10% → 100%)
   └─ ✅ Health checks

8. Post-Deployment Validation
   ├─ ✅ Smoke tests
   ├─ ✅ Monitor for 24 hours
   └─ ✅ Deactivate old revision

ROLLBACK READY AT EVERY STEP!
```

---

## Handling Different Scenarios

### Scenario 1: Simple Feature (No Conflicts)

```bash
Flow: develop → staging → test → main → CI tests → approve → production
Time: ~15 minutes
Risk: Low
```

### Scenario 2: Multiple Features (No Conflicts)

```bash
Flow: develop → staging → test → main → CI tests → approve → production
Time: ~20 minutes (more testing)
Risk: Medium
```

### Scenario 3: Merge Conflicts Detected

```bash
Flow: develop → PR (conflicts!) → resolve → temp branch → staging → test → main → CI tests → approve → production
Time: ~45 minutes (includes conflict resolution + testing)
Risk: Medium-High (requires extra testing)
```

### Scenario 4: Hotfix in Production

```bash
Flow:
1. Create hotfix branch from main
2. Fix issue
3. Deploy hotfix branch to staging → test
4. Merge hotfix → main → approve → production
5. Merge hotfix → develop (sync staging with hotfix)

Time: ~30 minutes
Risk: High (urgent fix, needs immediate testing)
```

---

## Implementation: Updated Workflow Files

### File 1: Test Merged Result Before Production

```yaml
# .github/workflows/test-main-branch.yml
name: Test Main Branch Before Production

on:
  pull_request:
    branches:
      - main
  push:
    branches:
      - main

jobs:
  build-and-test:
    name: Build and Test Merged Code
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      # Backend tests
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build backend
        run: dotnet build -c Release --no-restore

      - name: Run unit tests
        run: dotnet test --no-restore --verbosity normal

      # Frontend tests
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: web/package-lock.json

      - name: Install frontend dependencies
        run: cd web && npm ci

      - name: Build frontend
        run: cd web && npm run build

      - name: Run frontend tests
        run: cd web && npm run test:unit

      - name: Type checking
        run: cd web && npx tsc --noEmit

  deployment-ready:
    name: Mark as Ready for Production
    needs: build-and-test
    runs-on: ubuntu-latest

    steps:
      - name: Deployment readiness
        run: |
          echo "✅ All tests passed!"
          echo "✅ Code is ready for production deployment"
          echo ""
          echo "Next steps:"
          echo "1. Review changes in staging"
          echo "2. Approve production deployment in GitHub Actions"
```

### File 2: Deploy Main to Staging (On-Demand)

```yaml
# .github/workflows/deploy-main-to-staging.yml
name: Deploy Main to Staging (Pre-Production Test)

on:
  workflow_dispatch:
    inputs:
      reason:
        description: 'Reason for testing main in staging'
        required: true
        type: string

jobs:
  deploy-backend:
    uses: ./.github/workflows/deploy-staging.yml
    with:
      branch: main
      test_mode: true
    secrets: inherit

  deploy-frontend:
    uses: ./.github/workflows/deploy-ui-staging.yml
    with:
      branch: main
      test_mode: true
    secrets: inherit

  notify:
    needs: [deploy-backend, deploy-frontend]
    runs-on: ubuntu-latest
    steps:
      - name: Deployment notification
        run: |
          echo "✅ Main branch deployed to staging"
          echo "Reason: ${{ inputs.reason }}"
          echo ""
          echo "Test thoroughly before approving production deployment!"
```

---

## Recommended Approach for Your Project

### **Hybrid Strategy** (Best Balance)

```
Normal Releases (90% of deployments):
  develop → staging → test → main → CI tests → approve → production

Critical Releases (10% of deployments):
  develop → staging → test → main → CI tests → deploy main to staging → test again → approve → production

Hotfixes:
  hotfix → staging → test → main → approve → production → sync to develop
```

**Cost:** $0 extra (reuses staging infrastructure)
**Safety:** High (automated tests + optional manual testing)
**Speed:** Fast for normal releases, thorough for critical ones

---

## Summary: Complete Safety Checklist

### Before Merging develop → main

- [ ] All features tested in staging
- [ ] No critical bugs reported
- [ ] Code review completed
- [ ] Conflicts resolved (if any)
- [ ] CI tests passing on develop

### After Merging to main

- [ ] CI tests pass on main
- [ ] Build succeeds (backend + frontend)
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] (Optional) Deploy main to staging for extra testing
- [ ] All smoke tests pass

### Before Approving Production

- [ ] Staging is healthy
- [ ] No pending critical issues
- [ ] Database migrations tested
- [ ] Rollback plan ready
- [ ] Monitoring configured

### After Production Deployment

- [ ] Health checks pass
- [ ] Smoke tests pass
- [ ] No errors in logs
- [ ] Monitor for 24 hours
- [ ] Document deployment

---

## Answer to Your Question

**Q:** "What if conflicts or issues happen during develop → main merge?"

**A:** Use the **Hybrid Strategy**:

1. **Normal case (no conflicts):**
   - develop → staging → test → main → CI tests → approve → production
   - Fast and safe

2. **Conflict case:**
   - develop → PR (conflicts detected!) → resolve in temp branch
   - Temp branch → staging (test merged result)
   - If tests pass → merge to main → CI tests → approve → production
   - If tests fail → fix in develop, try again

3. **Extra safety for critical changes:**
   - main → staging (manual deploy before production)
   - Test exact production code
   - Approve only if everything works

**Cost:** $0 extra
**Safety:** Very high
**Risk:** Minimal (tested at every step)

---

**Status:** Complete Safe Deployment Strategy ✅
**Recommended:** Hybrid approach with CI tests + optional staging-on-main
**Risk Mitigation:** Multiple safety checks at each step