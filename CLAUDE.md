# Claude Code Configuration - LankaConnect Development

**CRITICAL: This file is read at the start of EVERY conversation and by EVERY spawned agent.**
**ALL rules here are MANDATORY and MUST be followed without exception.**

---

# PART A: LANKACONNECT PROJECT RULES (MANDATORY)

## 🚨 SECTION 1: SENIOR ENGINEER MINDSET (ALWAYS ACTIVE)

**Role:** Think and act as a **Senior Software Engineer** at all times.

### Core Principles:
1. ✅ **Handle issues systematically** - No shortcuts, no quick patches
2. ✅ **Apply durable fixes** - Build for maintainability, not just immediate resolution
3. ✅ **Question everything** - If unsure about design/scope/impact, consult architect or ask user
4. ✅ **Reuse before create** - Search codebase for similar implementations before writing new code
5. ✅ **Never break existing functionality** - Especially UI components (very fragile)

---

## 🚨 SECTION 2: TEST-DRIVEN DEVELOPMENT (TDD) - MANDATORY

**ABSOLUTE REQUIREMENT: Write tests FIRST, then implementation.**

### TDD Process (Red-Green-Refactor):
1. **RED**: Write failing test for new feature
2. **GREEN**: Write minimal code to make test pass
3. **REFACTOR**: Clean up code while keeping tests green

### TDD Rules:
- ✅ **Zero tolerance for compilation errors** - Fix ALL errors before proceeding
- ✅ **Small, testable steps** - Iterate incrementally
- ✅ **90% test coverage minimum** - Measure with `dotnet test /p:CollectCoverage=true`
- ✅ **Tests must pass before commit** - Run `dotnet test` before every git commit

---

## 🚨 SECTION 3: UI/UX BEST PRACTICES (MANDATORY)

**CRITICAL: LankaConnect has established UI patterns. NEVER deviate without user approval.**

### UI Consistency Rules:
1. ✅ **Follow existing component patterns** - Check `/web/src/presentation/components/` before creating new components
2. ✅ **Use design system** - Refer to [UI_STYLE_GUIDE.md](./docs/UI_STYLE_GUIDE.md) for colors, spacing, typography
3. ✅ **Accessibility first** - All inputs must have labels, all interactive elements must be keyboard-accessible
4. ✅ **Mobile-first responsive design** - Test on mobile breakpoints (320px, 768px, 1024px)
5. ✅ **Loading states** - All async operations must show loading indicators
6. ✅ **Error boundaries** - All pages must have error boundaries for graceful failure

### UI Change Protocol:
**Before changing ANY UI component:**
1. Read the component file to understand current behavior
2. Search for ALL usages: `grep -r "ComponentName" web/src/`
3. Test changes in ALL contexts where component is used
4. Add unit tests for new props/behavior
5. Get user approval if changing visual appearance

---

## 🚨 SECTION 4: OBSERVABILITY & ERROR HANDLING (MANDATORY)

**CRITICAL: All code must be traceable and debuggable in production.**

### Logging Requirements:
```csharp
// ✅ CORRECT: Structured logging with context
_logger.LogInformation(
    "Creating order {OrderId} for user {UserId} with {ItemCount} items",
    orderId, userId, items.Count);

try
{
    var order = await _orderRepository.CreateAsync(orderData);
    _logger.LogInformation("Order {OrderId} created successfully", order.Id);
    return order;
}
catch (Exception ex)
{
    _logger.LogError(ex,
        "Failed to create order for user {UserId}. Items: {ItemCount}",
        userId, items.Count);
    throw;
}
```

### Try-Catch Requirements:
- ✅ **Wrap ALL external calls** - Database, HTTP, file I/O must have try-catch
- ✅ **Log before rethrowing** - Always log exception with context before `throw;`
- ✅ **Never swallow exceptions** - Empty catch blocks are FORBIDDEN
- ✅ **Use specific exceptions** - Catch specific types when possible

---

## 🚨 SECTION 5: DATABASE MIGRATIONS (EF CORE - MANDATORY)

**CRITICAL: ALL database changes MUST use EF Core migrations.**

### Migration Workflow:
```bash
# 1. Create migration (ALWAYS name descriptively)
dotnet ef migrations add AddMarketplaceProductsTable --project src/LankaConnect.Infrastructure

# 2. Review generated migration code
# 3. Test migration locally
dotnet ef database update --project src/LankaConnect.Infrastructure

# 4. Verify migration succeeded
# 5. Commit migration files
git add src/LankaConnect.Infrastructure/Data/Migrations/
git commit -m "Add marketplace products table migration"
```

### Migration Rules:
- ✅ **Never edit existing migrations** - Create new migration if changes needed
- ✅ **Always test Down() method** - Ensure rollback works
- ✅ **Use schema names** - All tables must specify schema: `modelBuilder.ToTable("products", "marketplace");`
- ✅ **Check for conflicts** - Pull latest from develop before creating migration

---

## 🚨 SECTION 6: AZURE STAGING DEPLOYMENT (MANDATORY)

**CRITICAL: LankaConnect deploys to Azure staging after EVERY change.**

### Deployment Workflow:

#### Backend Changes:
```bash
# 1-3: Make changes, write tests, run tests locally
dotnet test

# 4-5: Commit and push
git add . && git commit -m "feat(marketplace): Add product catalog API"
git push origin feature/marketplace-module

# 6: GitHub Actions runs deploy-staging.yml automatically

# 7: Test deployed API
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"12!@qwASzx","rememberMe":true,"ipAddress":"string"}'
```

### Post-Deployment Verification (MANDATORY):
- [ ] API endpoint returns expected response
- [ ] Database migrations applied successfully
- [ ] No errors in container logs
- [ ] Frontend page loads without errors

---

## 🚨 SECTION 7: DOCUMENTATION SYNCHRONIZATION (MANDATORY)

**CRITICAL: Update tracking docs BEFORE marking task complete.**

### After EVERY implementation, update ALL 3 documents:
1. **PROGRESS_TRACKER.md** - Add new entry with date, changes made
2. **STREAMLINED_ACTION_PLAN.md** - Update action item status
3. **TASK_SYNCHRONIZATION_STRATEGY.md** - Update phase overview

---

## 🚨 SECTION 8: STATUS REPORTING (MANDATORY - BE HONEST)

**CRITICAL: Never claim success without verification.**

### Status Checklist (Complete ALL before reporting success):
- [ ] Code committed and pushed
- [ ] Tests passing (90%+ coverage)
- [ ] Deployment succeeded
- [ ] Database migrations applied
- [ ] API tested with curl
- [ ] UI tested in browser
- [ ] Logs checked (no errors)
- [ ] Documentation updated

---

## 🚨 SECTION 9: MODULE DEVELOPMENT STANDARDS (MANDATORY)

**CRITICAL: All modules MUST follow Clean Architecture + DDD patterns.**

### Module Structure (Exact Pattern):
```
src/LankaConnect.[ModuleName]/
├── [ModuleName].Domain/        # Aggregates, Entities, ValueObjects
├── [ModuleName].Application/   # Commands, Queries, Handlers
├── [ModuleName].Infrastructure/ # Data, Repositories, Migrations
├── [ModuleName].API/           # Controllers, DTOs, Filters
└── [ModuleName].Tests/         # Domain, Application, Infra, API tests
```

### Module Boundaries (STRICT):
- ✅ **Module can reference:** `LankaConnect.Shared` only
- ❌ **Module CANNOT reference:** Other modules directly
- ❌ **No cross-module database queries**
- ❌ **No shared entities**

---

## 🚨 SECTION 10: UI STYLE GUIDE COMPLIANCE (MANDATORY)

**CRITICAL: Refer to [UI_STYLE_GUIDE.md](./docs/UI_STYLE_GUIDE.md) for ALL UI work.**

Before Building ANY UI Component:
1. Check if similar component exists
2. Read UI_STYLE_GUIDE.md for design tokens
3. Use existing components when possible
4. Get user approval if deviating from style guide

---

## 🚨 SECTION 11: GIT WORKFLOW FOR PARALLEL DEVELOPMENT

### Git Worktree Setup (Prevents Conflicts):
```bash
# Agent 1: Events module
git worktree add ../lc-events feature/events-module

# Agent 2: Marketplace module
git worktree add ../lc-marketplace feature/marketplace-module
```

### Before Merging to Develop:
1. Pull latest develop
2. Rebase feature branch
3. Resolve conflicts (if any)
4. Run ALL tests
5. Push and create PR

---

## 🚨 SECTION 12: PRE-COMPLETION CHECKLIST

**MANDATORY: Complete ALL items before reporting task complete.**

- [ ] Code follows Clean Architecture + DDD
- [ ] Tests written FIRST (TDD), 90%+ coverage
- [ ] All tests passing locally
- [ ] Code committed with descriptive message
- [ ] Deployed to Azure staging successfully
- [ ] API tested / UI tested
- [ ] Azure logs checked (no errors)
- [ ] All 3 PRIMARY docs updated
- [ ] Status report includes verification

---

# PART B: CLAUDE FLOW & SPARC METHODOLOGY

## 🚨 CRITICAL: CONCURRENT EXECUTION & FILE MANAGEMENT

**ABSOLUTE RULES**:
1. ALL operations MUST be concurrent/parallel in a single message
2. **NEVER save working files, text/mds and tests to the root folder**
3. ALWAYS organize files in appropriate subdirectories
4. **USE CLAUDE CODE'S TASK TOOL** for spawning agents concurrently, not just MCP

### ⚡ GOLDEN RULE: "1 MESSAGE = ALL RELATED OPERATIONS"

**MANDATORY PATTERNS:**
- **TodoWrite**: ALWAYS batch ALL todos in ONE call (5-10+ todos minimum)
- **File operations**: ALWAYS batch ALL reads/writes/edits in ONE message
- **Bash commands**: ALWAYS batch ALL terminal operations in ONE message
- **Memory operations**: ALWAYS batch ALL memory store/retrieve in ONE message

### 🎯 CRITICAL: Claude Code Task Tool for Agent Execution

**Claude Code's Task tool is the PRIMARY way to spawn agents:**
```javascript
// ✅ CORRECT: Use Claude Code's Task tool for parallel agent execution
[Single Message]:
  Task("Research agent", "Analyze requirements and patterns...", "researcher")
  Task("Coder agent", "Implement core features...", "coder")
  Task("Tester agent", "Create comprehensive tests...", "tester")
  Task("Reviewer agent", "Review code quality...", "reviewer")
```

**MCP tools are ONLY for coordination setup:**
- `mcp__claude-flow__swarm_init` - Initialize coordination topology
- `mcp__claude-flow__agent_spawn` - Define agent types for coordination
- `mcp__claude-flow__task_orchestrate` - Orchestrate high-level workflows

### 📁 File Organization Rules

**NEVER save to root folder. Use these directories:**
- `/src` - Source code files
- `/tests` - Test files
- `/docs` - Documentation and markdown files
- `/config` - Configuration files
- `/scripts` - Utility scripts

---

## 🚨 CRITICAL: REQUIREMENT DOCUMENTATION PROTOCOL (Phase 6A Prevention System)

**PROBLEM**: Phase 6A revealed requirements discussed in conversation but NEVER documented in PRIMARY tracking docs, causing missed implementations.

**SOLUTION**: Three-part prevention system to ensure requirement gaps are caught early.

### Part 1: Conversation History Review (ALWAYS DO FIRST)

**Before implementing ANY feature**:
1. ✅ Read conversation history looking for undocumented planning
2. ✅ Check if requirements were discussed but never written to tracking docs
3. ✅ Verify all user intent is captured in PRIMARY docs

**Red Flags to Look For**:
- "I discussed this before..." = Requirement in conversation history only
- "We talked about..." = Not documented in PRIMARY docs

### Part 2: Phase Number Management (CRITICAL)

**Before assigning ANY new phase number**:
1. ✅ Check [PHASE_6A_MASTER_INDEX.md](./docs/PHASE_6A_MASTER_INDEX.md) for next available number
2. ✅ Verify number not used in tracking docs
3. ✅ **Record assignment in master index BEFORE implementation starts**

### Part 3: Documentation Synchronization (BEFORE COMPLETION)

**PRIMARY Tracking Documents** (MUST STAY IN SYNC):
1. [PROGRESS_TRACKER.md](./docs/PROGRESS_TRACKER.md) - Current session status
2. [STREAMLINED_ACTION_PLAN.md](./docs/STREAMLINED_ACTION_PLAN.md) - Action items
3. [TASK_SYNCHRONIZATION_STRATEGY.md](./docs/TASK_SYNCHRONIZATION_STRATEGY.md) - Phase overview

---

## Project Overview

This project is an **AI-powered listing application** built using:
- **Clean Architecture**: Domain-centered design with dependency inversion
- **Domain-Driven Design (DDD)**: Rich domain models with aggregates, value objects, domain services
- **Test-Driven Development (TDD)**: Red-Green-Refactor with 90% test coverage
- **SPARC Methodology**: Systematic development workflow with Claude-Flow orchestration

### Architectural Layers
```
src/
├── domain/          # Business logic, entities, value objects
├── application/     # Use cases, application services
├── infrastructure/  # Data access, external services
└── presentation/    # Controllers, UI, API endpoints
```

---

## SPARC Workflow Phases

1. **Specification** - Requirements analysis
2. **Pseudocode** - Algorithm design
3. **Architecture** - System design
4. **Refinement** - TDD implementation
5. **Completion** - Integration

---

## 🚀 Available Agents

### Core Development (Essential)
`coder`, `reviewer`, `tester`, `planner`, `researcher`, `backend-dev`, `system-architect`, `code-analyzer`, `api-docs`, `cicd-engineer`, `perf-analyzer`

### SPARC Methodology
`sparc-coord`, `sparc-coder`, `specification`, `pseudocode`, `architecture`, `refinement`

---

## 🎯 Claude Code vs MCP Tools

### Claude Code Handles ALL EXECUTION:
- **Task tool**: Spawn and run agents concurrently
- File operations (Read, Write, Edit, Glob, Grep)
- Code generation and programming
- Bash commands and system operations
- TodoWrite and task management
- Git operations, testing, debugging

### MCP Tools ONLY COORDINATE:
- Swarm initialization (topology setup)
- Agent type definitions (coordination patterns)
- Task orchestration (high-level planning)
- Memory management, performance tracking

**KEY**: MCP coordinates the strategy, Claude Code's Task tool executes with real agents.

---

## 📋 Agent Coordination Protocol

### Every Agent Spawned via Task Tool MUST:

**1️⃣ BEFORE Work:**
```bash
npx claude-flow@alpha hooks pre-task --description "[task]"
npx claude-flow@alpha hooks session-restore --session-id "swarm-[id]"
```

**2️⃣ DURING Work:**
```bash
npx claude-flow@alpha hooks post-edit --file "[file]" --memory-key "swarm/[agent]/[step]"
npx claude-flow@alpha hooks notify --message "[what was done]"
```

**3️⃣ AFTER Work:**
```bash
npx claude-flow@alpha hooks post-task --task-id "[task]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

---

## 🎯 Concurrent Execution Examples

### ✅ CORRECT WORKFLOW: MCP Coordinates, Claude Code Executes

```javascript
// Single Message - Parallel Agent Execution
[Parallel Execution]:
  Task("Research agent", "Analyze API requirements. Check memory for prior decisions.", "researcher")
  Task("Coder agent", "Implement REST endpoints. Coordinate via hooks.", "coder")
  Task("Database agent", "Design database schema. Store decisions in memory.", "code-analyzer")
  Task("Tester agent", "Create comprehensive test suite with 90% coverage.", "tester")
  Task("Reviewer agent", "Review code quality and security. Document findings.", "reviewer")

  // Batch ALL todos in ONE call
  TodoWrite { todos: [
    {id: "1", content: "Research API patterns", status: "in_progress"},
    {id: "2", content: "Design database schema", status: "in_progress"},
    {id: "3", content: "Implement authentication", status: "pending"},
    {id: "4", content: "Build REST endpoints", status: "pending"},
    {id: "5", content: "Write unit tests", status: "pending"},
    {id: "6", content: "Integration tests", status: "pending"}
  ]}

  // Parallel file operations
  Write "app/src/server.ts"
  Write "app/tests/server.test.ts"
  Write "app/docs/API.md"
```

### ❌ WRONG (Multiple Messages):
```javascript
Message 1: Task("agent 1")
Message 2: TodoWrite { todos: [single todo] }
Message 3: Write "file.js"
// This breaks parallel coordination!
```

---

## Performance Benefits

- **84.8% SWE-Bench solve rate**
- **32.3% token reduction**
- **2.8-4.4x speed improvement**
- **27+ neural models**

---

## 🎯 PROJECT-SPECIFIC INFORMATION

### Tech Stack:
- **Backend**: .NET 8, C#, Clean Architecture, DDD, EF Core 8, PostgreSQL
- **Frontend**: Next.js 16, React 19, TypeScript, Zustand, TailwindCSS
- **Database**: PostgreSQL with schema separation (events, marketplace, business, forum)
- **Deployment**: Azure Container Apps (staging + production)
- **CI/CD**: GitHub Actions (deploy-staging.yml, deploy-ui-staging.yml)

### Azure Staging URLs:
- **Backend API**: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
- **Test Credentials**: Email: `niroshhh@gmail.com`, Password: `12!@qwASzx`

---

## 📚 REFERENCE DOCUMENTS

All agents MUST read these documents before starting work:

1. **[UI_STYLE_GUIDE.md](./docs/UI_STYLE_GUIDE.md)** - UI consistency
2. **[PARALLEL_AGENT_COORDINATION.md](./docs/PARALLEL_AGENT_COORDINATION.md)** - Coordination strategy
3. **[REVISED_MODULAR_MONOLITH_STRATEGY.md](./docs/REVISED_MODULAR_MONOLITH_STRATEGY.md)** - Architecture
4. **[PROGRESS_TRACKER.md](./docs/PROGRESS_TRACKER.md)** - Current status
5. **[STREAMLINED_ACTION_PLAN.md](./docs/STREAMLINED_ACTION_PLAN.md)** - Action items

---

## ❌ COMMON MISTAKES TO AVOID

1. ❌ **Skipping tests** - Tests are MANDATORY
2. ❌ **Committing without testing** - Always run tests first
3. ❌ **Breaking existing UI** - Test ALL usages
4. ❌ **Empty try-catch blocks** - Always log exceptions
5. ❌ **Cross-module dependencies** - Modules must be independent
6. ❌ **Skipping deployment verification** - Always test deployed API
7. ❌ **Claiming success without proof** - Verify EVERY checklist item

---

## ✅ SUCCESS CRITERIA

A task is ONLY complete when:
- ✅ Code follows all patterns defined in this file
- ✅ Tests written first (TDD) and passing (90%+ coverage)
- ✅ Deployed to Azure staging successfully
- ✅ Verified working in staging environment
- ✅ All documentation updated
- ✅ Checklist completed with evidence

---

**Remember: This file is LAW. Follow it without exception. If something is unclear, ASK the user.**

**Last Updated**: 2026-01-24
