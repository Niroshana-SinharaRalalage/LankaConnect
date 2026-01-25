# Parallel Agent Coordination Strategy

**For Monitoring and Instructing 4+ Agents Working Simultaneously**

**Last Updated:** 2026-01-24

---

## 🎯 Purpose

This document explains how to:
1. **Monitor** progress of 4 parallel agents
2. **Instruct** agents during execution
3. **Coordinate** code changes and deployments
4. **Sync** branches and push to staging

---

## 👥 THE 4 AGENT TEAMS

### Team 1: Events Module Refactor
**Branch:** `feature/events-module-refactor`
**Worktree:** `../lc-events`
**Scope:** Restructure existing Events code into modular architecture

### Team 2: Marketplace Module
**Branch:** `feature/marketplace-module`
**Worktree:** `../lc-marketplace`
**Scope:** Build complete marketplace (products, cart, orders, Stripe, shipping)

### Team 3: Business Profile & Forum Modules
**Branch:** `feature/business-forum-modules`
**Worktree:** `../lc-business-forum`
**Scope:** Build business profiles with approval + forum with content moderation

### Team 4: Frontend Features
**Branch:** `feature/frontend-features`
**Worktree:** `../lc-frontend`
**Scope:** Build all UI pages for Marketplace, Business, Forum

---

## 📊 MONITORING AGENT PROGRESS

### Method 1: Real-Time Conversation Monitoring
Each agent runs in its own conversation context:

```
Main Conversation (You)
├── Agent 1 Output (Events Module)
│   ├── "Creating Events.Domain project..."
│   ├── "Refactoring EventsController..."
│   └── "Tests passing: 45/45"
│
├── Agent 2 Output (Marketplace)
│   ├── "Creating Marketplace.Domain..."
│   ├── "Implementing Product aggregate..."
│   └── "Stripe integration complete"
│
├── Agent 3 Output (Business/Forum)
│   └── "Building BusinessProfile domain..."
│
└── Agent 4 Output (Frontend)
    └── "Creating Marketplace pages..."
```

**How to check:**
- Agents report progress inline
- Check conversation history for each agent's outputs
- Look for completion messages or error reports

### Method 2: Git Commit History
```bash
# See all commits from all agents
git log --oneline --graph --all

# See commits by branch
git log --oneline feature/marketplace-module
git log --oneline feature/events-module-refactor

# See what each agent changed
git show feature/marketplace-module:src/LankaConnect.Marketplace/
```

### Method 3: Memory Coordination
Agents store progress in shared memory:

```bash
# Check what agents have stored
npx claude-flow@alpha memory search "swarm/*"

# Example outputs:
# swarm/marketplace/schema-created: true
# swarm/marketplace/product-aggregate: complete
# swarm/events/refactor-complete: false
# swarm/frontend/pages-created: 8/17
```

### Method 4: GitHub Actions Status
Check deployment status in real-time:

```bash
# Check CI/CD status
gh run list --branch feature/marketplace-module

# View running workflows
gh run view [run-id] --log
```

### Method 5: File System Monitoring
Each agent works in isolated worktree:

```bash
# Check Events agent's progress
cd ../lc-events && git status && git log --oneline -5

# Check Marketplace agent's progress
cd ../lc-marketplace && git status && git log --oneline -5

# Check Business/Forum agent's progress
cd ../lc-business-forum && git status && git log --oneline -5

# Check Frontend agent's progress
cd ../lc-frontend && git status && git log --oneline -5
```

### Method 6: Tracking Document Updates
Agents update tracking docs as they complete tasks:

```bash
# Check PROGRESS_TRACKER.md for agent updates
grep -A 10 "Session.*Marketplace" docs/PROGRESS_TRACKER.md

# Check STREAMLINED_ACTION_PLAN.md for completed items
grep -A 5 "Marketplace" docs/STREAMLINED_ACTION_PLAN.md
```

---

## 🎯 INSTRUCTING AGENTS

### BEFORE Spawning Agents

Include ALL instructions in the initial Task prompt:

```javascript
Task(
  "Marketplace Developer",
  "Build complete Marketplace module following these requirements:

   ARCHITECTURE:
   - Follow Clean Architecture pattern from CLAUDE.md Part 9
   - Use Domain/Application/Infrastructure/API layers
   - Database schema: 'marketplace'

   FEATURES:
   - Product catalog with categories, search, filters
   - Shopping cart with session persistence
   - Checkout with Stripe integration
   - Order management with status tracking
   - Inventory management with low-stock warnings
   - Admin product management
   - Promotion system (20% off, free shipping, etc.)
   - Shipping label generation (USPS, UPS, FedEx)

   TESTING:
   - Write tests FIRST (TDD)
   - 90%+ coverage required
   - Domain tests, application tests, integration tests

   DEPLOYMENT:
   - Deploy to Azure staging after implementation
   - Test API endpoints with curl
   - Verify migrations applied successfully

   COORDINATION:
   - Store product schema in memory for frontend team
   - Check memory for shared authentication patterns
   - Update PROGRESS_TRACKER.md when complete

   CONSTRAINTS:
   - Do NOT reference other modules directly
   - Use LankaConnect.Shared for common code
   - Follow UI_STYLE_GUIDE.md for any UI components
  ",
  "backend-dev"
)
```

### DURING Execution (Foreground Agents)

**You CANNOT interrupt foreground agents mid-execution.**

Agents run to completion, then report results. If you need to change course:
1. Let agent finish current task
2. Review output
3. Spawn new agent with updated instructions

### AFTER Completion

Review agent output and provide feedback:

```markdown
# Review Agent 2 (Marketplace) Output

✅ What went well:
- Clean Architecture properly implemented
- 95% test coverage achieved
- Stripe integration working

❌ Issues found:
- Missing shipping label generation
- Inventory warnings not implemented
- Admin UI missing

Next steps:
- Spawn follow-up agent to implement missing features
- Or adjust scope and mark as complete with known limitations
```

Then respawn agent if needed:

```javascript
Task(
  "Marketplace Developer (Follow-up)",
  "Complete remaining Marketplace features:
   - Implement shipping label generation (USPS, UPS, FedEx)
   - Add inventory low-stock warnings (<10 items)
   - Build admin product management UI

   Context: Previous agent completed core marketplace features.
   Check memory for existing Product schema.
  ",
  "backend-dev"
)
```

---

## 🔄 CODE SYNCHRONIZATION & DEPLOYMENT

### Phase 1: Initial Setup (Before Spawning Agents)

```bash
# 1. Create feature branches
git checkout -b feature/events-module-refactor
git push -u origin feature/events-module-refactor

git checkout develop
git checkout -b feature/marketplace-module
git push -u origin feature/marketplace-module

git checkout develop
git checkout -b feature/business-forum-modules
git push -u origin feature/business-forum-modules

git checkout develop
git checkout -b feature/frontend-features
git push -u origin feature/frontend-features

# 2. Create git worktrees for isolation
git worktree add ../lc-events feature/events-module-refactor
git worktree add ../lc-marketplace feature/marketplace-module
git worktree add ../lc-business-forum feature/business-forum-modules
git worktree add ../lc-frontend feature/frontend-features

# 3. Verify worktrees created
git worktree list
```

### Phase 2: During Development (Agents Auto-Commit)

Each agent commits to their own branch independently:

```bash
# Agent 1 (Events Module)
cd ../lc-events
git add .
git commit -m "refactor(events): Restructure into module pattern"
git push origin feature/events-module-refactor

# Agent 2 (Marketplace)
cd ../lc-marketplace
git add .
git commit -m "feat(marketplace): Add product catalog and shopping cart"
git push origin feature/marketplace-module

# Agent 3 (Business/Forum)
cd ../lc-business-forum
git add .
git commit -m "feat(business): Add business profile approval workflow"
git push origin feature/business-forum-modules

# Agent 4 (Frontend)
cd ../lc-frontend
git add .
git commit -m "feat(ui): Add marketplace product catalog pages"
git push origin feature/frontend-features
```

**GitHub Actions automatically:**
- Runs tests
- Deploys backend to Azure staging (if backend changes)
- Deploys frontend to Azure staging (if frontend changes)

### Phase 3: Continuous Deployment Monitoring

Each push triggers deployment:

```bash
# Monitor backend deployment
gh run watch --repo lankaconnect

# Check staging API
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health

# Check staging frontend
open https://[frontend-staging-url]
```

### Phase 4: Integration (After Agents Complete)

Once all 4 agents complete, integrate their work:

```bash
# 1. Merge to develop (one at a time, test after each merge)
cd c:/Work/LankaConnect  # Back to main repo
git checkout develop

# Merge Events module
git pull origin develop
git merge feature/events-module-refactor
git push origin develop
# Wait for deployment, verify in staging

# Merge Marketplace module
git pull origin develop
git merge feature/marketplace-module
git push origin develop
# Wait for deployment, verify in staging

# Merge Business/Forum modules
git pull origin develop
git merge feature/business-forum-modules
git push origin develop
# Wait for deployment, verify in staging

# Merge Frontend features
git pull origin develop
git merge feature/frontend-features
git push origin develop
# Wait for deployment, verify in staging

# 2. Final integration testing
# - Test all modules work together
# - Test end-to-end user journeys
# - Verify no regressions

# 3. Clean up worktrees
git worktree remove ../lc-events
git worktree remove ../lc-marketplace
git worktree remove ../lc-business-forum
git worktree remove ../lc-frontend

# 4. Delete feature branches (optional, keep for reference)
git branch -d feature/events-module-refactor
git push origin --delete feature/events-module-refactor
```

---

## 🚨 CONFLICT RESOLUTION

### Preventing Conflicts (Best Practices)

1. **Clear Module Boundaries**
   - Events agent only touches `src/LankaConnect.Events/`
   - Marketplace agent only touches `src/LankaConnect.Marketplace/`
   - No overlapping file changes

2. **Frontend Coordination**
   - Frontend agent creates NEW pages/components
   - Doesn't modify existing pages
   - If modification needed, document and handle sequentially

3. **Shared Code Changes**
   - If multiple agents need to modify `LankaConnect.Shared`:
     - One agent makes change first
     - Other agents pull and rebase
     - Or defer shared changes to post-integration phase

### Handling Conflicts (If They Occur)

```bash
# When merging to develop, if conflicts occur:
git checkout develop
git merge feature/marketplace-module

# Conflict detected
Auto-merging src/LankaConnect.Shared/Common/BaseEntity.cs
CONFLICT (content): Merge conflict in src/LankaConnect.Shared/Common/BaseEntity.cs

# Resolve conflict
code src/LankaConnect.Shared/Common/BaseEntity.cs
# Manually resolve conflict markers

# Complete merge
git add src/LankaConnect.Shared/Common/BaseEntity.cs
git commit -m "Merge feature/marketplace-module (resolved BaseEntity conflict)"
git push origin develop
```

---

## 📋 DEPLOYMENT VERIFICATION CHECKLIST

After EACH merge to develop and deployment:

### Backend Verification
- [ ] **API Health Check**
  ```bash
  curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health
  ```

- [ ] **Test New Endpoints**
  ```bash
  # Get token
  TOKEN=$(curl -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
    -H 'Content-Type: application/json' \
    -d '{"email":"niroshhh@gmail.com","password":"12!@qwASzx","rememberMe":true,"ipAddress":"string"}' \
    | jq -r '.accessToken')

  # Test marketplace endpoint
  curl -X GET 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/marketplace/products' \
    -H "Authorization: Bearer $TOKEN"
  ```

- [ ] **Check Container Logs**
  ```bash
  az containerapp logs show --name lankaconnect-api-staging --follow
  # Look for errors, migration success
  ```

- [ ] **Verify Database Migrations**
  ```sql
  -- Connect to Azure PostgreSQL
  SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;

  -- Verify new tables exist
  SELECT table_name FROM information_schema.tables
  WHERE table_schema = 'marketplace';
  ```

### Frontend Verification
- [ ] **Page Loads**
  ```bash
  # Open in browser
  open https://[frontend-staging-url]/marketplace
  ```

- [ ] **No Console Errors**
  - Open browser DevTools (F12)
  - Check Console tab for errors
  - Check Network tab for failed requests

- [ ] **Functionality Works**
  - Click through new features
  - Test forms, buttons, navigation
  - Verify data displays correctly

### Integration Verification
- [ ] **End-to-End User Journey**
  - Complete full workflow (e.g., browse products → add to cart → checkout)
  - Test across modules (e.g., create event → view in marketplace)
  - Verify authentication works across all modules

---

## 🎯 QUICK REFERENCE COMMANDS

### Check Agent Progress
```bash
# Git commits
git log --oneline --graph --all

# Memory
npx claude-flow@alpha memory search "swarm/*"

# GitHub Actions
gh run list

# Tracking docs
grep -A 5 "Marketplace" docs/PROGRESS_TRACKER.md
```

### Monitor Deployments
```bash
# Backend
az containerapp logs show --name lankaconnect-api-staging --follow

# Frontend
# (check GitHub Actions for frontend deployment)

# API health
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health
```

### Sync and Merge
```bash
# Pull latest
git checkout develop && git pull origin develop

# Merge feature branch
git merge feature/[branch-name]

# Resolve conflicts (if any)
git add [conflicted-files]
git commit -m "Merge feature/[branch-name]"

# Push to trigger deployment
git push origin develop
```

---

## ✅ SUCCESS METRICS

Parallel agent execution is successful when:

- ✅ **All 4 agents complete without errors**
- ✅ **No merge conflicts** (or conflicts resolved smoothly)
- ✅ **All deployments succeed** to Azure staging
- ✅ **All tests pass** (90%+ coverage)
- ✅ **Integration testing** passes end-to-end
- ✅ **Documentation updated** (all 3 PRIMARY docs)
- ✅ **Zero production incidents** from parallel development

---

**Questions? Refer to [CLAUDE.md](../CLAUDE.md) for detailed rules.**
**Last Updated:** 2026-01-24
