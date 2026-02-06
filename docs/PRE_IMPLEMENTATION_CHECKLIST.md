# Pre-Implementation Checklist - Modular Monolith Development

**Date:** 2026-01-24
**Decision:** Build Modular Monolith First, Extract to Microservices Later
**Timeline:** 4 weeks to production

---

## ✅ Your 8 Questions Answered

### 1. Repo & Branching Strategy for 4 Agents

**Answer:** Single repo, feature branches with git worktrees for isolation

```
lankaconnect/ (existing repo)
├── develop (protected)
├── feature/events-module-refactor       ← Agent 1 works here
├── feature/marketplace-module           ← Agent 2 works here
├── feature/business-forum-modules       ← Agent 3 works here
└── feature/frontend-new-features        ← Agent 4 works here
```

**Git Worktree Setup:**
```bash
# Each agent gets isolated filesystem (prevents merge conflicts)
git worktree add ../lc-events -b feature/events-module-refactor
git worktree add ../lc-marketplace -b feature/marketplace-module
git worktree add ../lc-business-forum -b feature/business-forum-modules
git worktree add ../lc-frontend -b feature/frontend-new-features
```

**Integration:**
- Agents work in parallel on separate branches
- Minimal conflicts (different folders: src/Events/, src/Marketplace/, etc.)
- Merge to `develop` when each module completes
- Final integration testing on `develop`

---

### 2. Initial Infrastructure Changes

**Answer:** A dedicated "Setup Agent" runs FIRST (Phase 0)

**Phase 0: Infrastructure Setup (1-2 days)**

```javascript
Task(
  "Infrastructure Engineer",
  "Setup modular monolith foundation:
   1. Create LankaConnect.Shared project:
      - Common/BaseEntity.cs, ValueObject.cs
      - Interfaces/IRepository.cs, IUnitOfWork.cs, ICommand.cs, IQuery.cs
      - Auth/JwtService.cs, CurrentUserService.cs
      - ReferenceData/ReferenceDataService.cs

   2. Create module project structures:
      - src/LankaConnect.Events/ (Domain, Application, Infrastructure, API)
      - src/LankaConnect.Marketplace/ (Domain, Application, Infrastructure, API)
      - src/LankaConnect.BusinessProfile/ (Domain, Application, Infrastructure, API)
      - src/LankaConnect.Forum/ (Domain, Application, Infrastructure, API)

   3. Update solution file:
      - Add all new projects
      - Set build dependencies

   4. Database schema creation:
      - CREATE SCHEMA marketplace;
      - CREATE SCHEMA business;
      - CREATE SCHEMA forum;
      - (events schema already exists)

   5. Update Program.cs:
      - Add module registration pattern
      - builder.Services.AddEventsModule(configuration);
      - builder.Services.AddMarketplaceModule(configuration);
      - builder.Services.AddBusinessProfileModule(configuration);
      - builder.Services.AddForumModule(configuration);

   6. Test setup:
      - Run dotnet build
      - Verify all modules compile
      - Document folder structure in README
  ",
  "code-analyzer"
)
```

**Then:** 4 agents start parallel module development

---

### 3. Frontend Changes Required

**Answer:** Yes, significant frontend work needed

**New Pages (17 total):**

**Marketplace:**
- `/marketplace` - Product catalog with filters
- `/marketplace/products/[id]` - Product detail page
- `/marketplace/cart` - Shopping cart
- `/marketplace/checkout` - Stripe checkout flow
- `/marketplace/orders` - Order history
- `/marketplace/orders/[id]` - Order details
- `/admin/marketplace/products` - Admin product management
- `/admin/marketplace/promotions` - Promotion management

**Business Profile:**
- `/business` - Business directory
- `/business/[id]` - Business profile details
- `/business/my-profile` - Create/edit own profile
- `/business/my-businesses` - List of owned businesses
- `/admin/business/approvals` - Admin approval queue

**Forum:**
- `/forum` - Forum list
- `/forum/[id]` - Forum discussion threads
- `/forum/posts/[id]` - Post detail with comments
- `/admin/forum/moderation` - Content moderation panel

**New API Repositories:**
```typescript
// web/src/infrastructure/api/repositories/
marketplace.repository.ts       - Products, cart, orders, promotions
business-profile.repository.ts  - Profiles, services, approvals
forum.repository.ts             - Forums, posts, comments, moderation
```

**Navigation Updates:**
```typescript
// Add to main nav
<Nav.Link href="/marketplace">Marketplace</Nav.Link>
<Nav.Link href="/business">Business Directory</Nav.Link>
<Nav.Link href="/forum">Forum</Nav.Link>
```

**No Changes Needed:**
- API client (reuses existing `api-client.ts`)
- Auth flow (same JWT mechanism)
- State management (Zustand still works)

---

### 4. Can Existing Tests Be Reused

**Answer:** Partially yes (Events), No for new modules

**Events Module Tests:**
✅ **Can reuse 100%** (just move to new location)

```csharp
// Before: src/LankaConnect.Application.Tests/Events/CreateEventCommandHandlerTests.cs
namespace LankaConnect.Application.Tests.Events
{
    public class CreateEventCommandHandlerTests { ... }
}

// After: src/LankaConnect.Events/Events.Application.Tests/CreateEventCommandHandlerTests.cs
namespace Events.Application.Tests
{
    public class CreateEventCommandHandlerTests { ... }  // Same test code!
}
```

**Changes Required:**
- Update namespaces: `LankaConnect.Application.Events` → `Events.Application`
- Update project references
- No logic changes needed

**New Modules (Marketplace, Business, Forum):**
❌ **No existing tests** (new features)

**Follow Same Patterns:**
- Domain tests: `Order.test.ts`, `Product.test.ts`
- Application tests: `CreateOrderCommandHandler.test.ts`
- Integration tests: `OrdersController.test.ts`
- UI tests: `PaymentForm.test.tsx`

**Test Coverage Target:** 90%+

---

### 5. How Easy to Extract Module to Another App

**Answer:** Very easy (2-3 days) because of clean boundaries

**Extraction Steps:**

**Day 1: Copy Module (4 hours)**
```bash
# In new application repo
mkdir -p src/Marketplace
cp -r /path/to/lankaconnect/src/LankaConnect.Marketplace/* src/Marketplace/
```

**Day 1: Update Dependencies (2 hours)**
```xml
<!-- New app references shared library -->
<ItemGroup>
  <PackageReference Include="LankaConnect.Shared" Version="1.0.0" />
  <!-- OR create your own shared library -->
</ItemGroup>
```

**Day 1: Configure DI (2 hours)**
```csharp
// In new app's Program.cs
builder.Services.AddMarketplaceModule(configuration);  // Same registration!
```

**Day 2: Database Setup (4 hours)**
```json
// Option A: Point to same DB (marketplace schema)
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=lankaconnect;Schema=marketplace;..."
  }
}

// Option B: Migrate to standalone DB
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=marketplace_standalone;..."
  }
}
```

**Day 2: Testing (4 hours)**
- Run module's test suite
- Integration testing
- E2E tests

**Day 3: Deployment (4 hours)**
- Build Docker container
- Deploy to Azure
- Smoke tests

**Total: 2-3 days** (because module is already self-contained)

**Why It's Easy:**
- ✅ No dependencies on other modules
- ✅ Own database schema (easy to point to different DB)
- ✅ Comprehensive tests (verify it works standalone)
- ✅ Standard DI registration pattern

---

### 6. Cleanup Root & Docs Folders

**Answer:** Yes, significant cleanup needed (see full analysis in [AGENT_CLEANUP_ANALYSIS.md](./AGENT_CLEANUP_ANALYSIS.md))

**Root Directory:** 274 files → 50 files (80% reduction)
- ❌ DELETE: 120+ build/test/output .txt files
- ❌ DELETE: 100+ test-*.json, test-*.sql, test-*.ps1 files
- ❌ DELETE: All token files (SECURITY ISSUE!)
- 📦 MOVE: Utility scripts to `/scripts/`
- ✅ KEEP: Solution file, configs, CLAUDE.md

**Docs Directory:** 451 files → 150 files (67% reduction)
- 📦 ARCHIVE: 167 Phase 6A docs (completed phases)
- 📦 ARCHIVE: Old Epic documentation
- 📦 ARCHIVE: Session summaries
- 📁 CONSOLIDATE: 35 ADR files to `/docs/adr/`
- 📁 CONSOLIDATE: Technical docs to subdirectories
- ✅ KEEP: 3 PRIMARY trackers, Master Index, Requirements

**Cleanup Script Created:** See full details in comprehensive cleanup analysis document.

---

### 7. Remove Unwanted Agents

**Answer:** Yes, reduce from 62 agents to 15 essential agents

**Current State:** 62 agent definitions (overwhelming!)

**Recommended Set:** 15 agents for small team

**KEEP (15 agents):**
```
✅ core/ - coder, planner, researcher, reviewer, tester (5)
✅ development/backend-dev (1)
✅ architecture/system-architect (1)
✅ testing/unit-tester, prod-validator (2)
✅ analysis/code-analyzer (1)
✅ documentation/api-docs (1)
✅ devops/cicd-engineer (1)
✅ optimization/perf-analyzer (1)
✅ base-template-generator (1)
```

**REMOVE (47 agents):**
```
❌ consensus/ - byzantine, raft, gossip, etc. (7 agents) - For distributed systems
❌ swarm/ - multi-repo coordination (20+ agents) - For large teams
❌ sparc/ - SPARC methodology (6 agents) - Optional, not essential
❌ data-ml/ - Machine learning (2 agents) - Not applicable
❌ migration/ - Legacy migrations (3 agents) - Not applicable
❌ templates/ - Redundant templates (4 agents)
```

**Cleanup Script:**
```bash
mkdir -p .claude/agents/archive
mv .claude/agents/{consensus,swarm,data,sparc} .claude/agents/archive/
# See full script in AGENT_CLEANUP_ANALYSIS.md
```

**Benefits:** Clearer agent selection, less confusion, faster development

---

### 8. Claude Code Parallel Execution Research

**Answer:** Comprehensive research completed (see full details above)

**Key Findings:**

**Parallel Execution Pattern:**
```javascript
// ✅ CORRECT: Single message with 4 Task calls = 4 agents run in parallel
Task("Backend", "Build API", "coder")
Task("Frontend", "Build UI", "coder")
Task("Testing", "Write tests", "tester")
Task("Review", "Code review", "reviewer")

// ❌ WRONG: 4 separate messages = sequential execution
Message 1: Task("Backend", "...", "coder")
Message 2: Task("Frontend", "...", "coder")  // Waits for backend
Message 3: Task("Testing", "...", "tester")  // Waits for frontend
```

**Agent Coordination:** Memory-based (your hooks already configured!)
```bash
# Backend stores schema
npx claude-flow@alpha hooks post-edit --file "Order.ts" --memory-key "swarm/backend/schema"

# Frontend reads schema
npx claude-flow@alpha hooks session-restore --read-key "swarm/backend/schema"
```

**Git Isolation:** Use worktrees to prevent conflicts
```bash
git worktree add ../lc-backend -b feature/backend
git worktree add ../lc-frontend -b feature/frontend
# Each agent works in separate filesystem
```

**Best Practices:**
- 4-6 agents maximum per parallel execution
- Layer by architecture (domain → app → infra → tests)
- Use memory for coordination
- Merge after each layer completes

---

## 🎯 FINAL PRE-IMPLEMENTATION CHECKLIST

Before spawning 4 agents to build modular monolith, complete these steps:

### ✅ Phase 0: Preparation (Do Now)

- [ ] **Cleanup root directory**
  - [ ] Delete 120+ build/test artifacts
  - [ ] Delete token files (security)
  - [ ] Move scripts to `/scripts/`
  - [ ] Archive to: See [CLEANUP_ANALYSIS](./CLEANUP_ANALYSIS.md)

- [ ] **Cleanup docs directory**
  - [ ] Archive 167 Phase 6A completed docs
  - [ ] Consolidate 35 ADRs to `/docs/adr/`
  - [ ] Organize technical docs into subdirectories
  - [ ] Archive: See [CLEANUP_ANALYSIS](./CLEANUP_ANALYSIS.md)

- [ ] **Cleanup agent definitions**
  - [ ] Archive 47 unwanted agents
  - [ ] Keep 15 essential agents
  - [ ] Script: See [AGENT_CLEANUP_ANALYSIS.md](./AGENT_CLEANUP_ANALYSIS.md)

- [ ] **Fix existing bugs** (you mentioned these need fixing first)
  - [ ] Document bugs to fix
  - [ ] Prioritize by severity
  - [ ] Fix before starting module development

### ✅ Phase 1: Infrastructure Setup (Spawn Setup Agent)

- [ ] **Spawn Infrastructure Agent** (runs FIRST)
  - [ ] Create `LankaConnect.Shared` project
  - [ ] Create 4 module project structures
  - [ ] Update solution file
  - [ ] Create database schemas (marketplace, business, forum)
  - [ ] Update `Program.cs` with module registration pattern
  - [ ] Verify build succeeds

### ✅ Phase 2: Parallel Module Development (Spawn 4 Agents)

- [ ] **Setup git worktrees** (prevents merge conflicts)
  - [ ] Create 4 feature branches
  - [ ] Create 4 worktrees
  - [ ] Verify isolation

- [ ] **Spawn 4 parallel agents** (single message!)
  - [ ] Agent 1: Events module refactor
  - [ ] Agent 2: Marketplace module
  - [ ] Agent 3: Business Profile + Forum modules
  - [ ] Agent 4: Frontend features

- [ ] **Monitor progress**
  - [ ] Check memory for agent coordination
  - [ ] Verify each agent completes
  - [ ] Review code as agents finish

### ✅ Phase 3: Integration & Testing

- [ ] **Merge feature branches**
  - [ ] Merge events-module-refactor
  - [ ] Merge marketplace-module
  - [ ] Merge business-forum-modules
  - [ ] Merge frontend-new-features

- [ ] **Integration testing**
  - [ ] Run full test suite (90%+ coverage)
  - [ ] E2E tests for critical journeys
  - [ ] Performance testing

### ✅ Phase 4: Deployment

- [ ] **Build Docker container** (single container with all modules)
- [ ] **Deploy to Azure Container Apps**
- [ ] **Run smoke tests**
- [ ] **GO LIVE! 🎉**

---

## 📚 Reference Documents Created

All questions answered in detail:

1. **Repo & Branching:** This document (Question 1)
2. **Infrastructure:** This document (Question 2)
3. **Frontend Changes:** This document (Question 3)
4. **Test Reusability:** This document (Question 4)
5. **Module Extraction:** This document (Question 5)
6. **Root/Docs Cleanup:** [CLEANUP_ANALYSIS.md](./CLEANUP_ANALYSIS.md) (comprehensive)
7. **Agent Cleanup:** [AGENT_CLEANUP_ANALYSIS.md](./AGENT_CLEANUP_ANALYSIS.md)
8. **Parallel Execution:** Comprehensive research completed (included above)

---

## 🚀 Ready to Proceed?

**Current Status:** ⚠️ **Blocked - Waiting for bugs to be fixed first**

**User Quote:**
> "Don't start anything until I tell. There are some bugs need to be fixed first."

**Next Steps:**
1. Fix existing bugs
2. Complete cleanup (root, docs, agents)
3. User approval to proceed
4. Spawn infrastructure agent (Phase 0)
5. Spawn 4 parallel agents (Phase 2)

---

## 💡 Questions Resolved

✅ **All 8 questions comprehensively answered**
✅ **Architecture decision: Modular Monolith First**
✅ **Timeline: 4 weeks to production**
✅ **Cost: ~50% savings vs microservices**
✅ **Extraction strategy: 2-3 days when needed**
✅ **Parallel execution: 4 agents via Task tool**
✅ **Cleanup plan: 274 → 50 root files, 451 → 150 docs**
✅ **Agent reduction: 62 → 15 essential agents**

**Status:** Ready to proceed when user approves (after bug fixes)
