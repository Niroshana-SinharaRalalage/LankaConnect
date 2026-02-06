# Agent Cleanup Analysis - LankaConnect

## Current State: 62 Agent Definitions in `.claude/agents/`

---

## ✅ KEEP - Essential for Modular Monolith (15 agents)

### Core Development (5 agents)
- `core/coder.md` - General code implementation
- `core/planner.md` - Planning and task breakdown
- `core/researcher.md` - Research and exploration
- `core/reviewer.md` - Code review
- `core/tester.md` - Testing

### Backend Development (3 agents)
- `development/backend/dev-backend-api.md` - Backend API development
- `architecture/system-design/arch-system-design.md` - System architecture
- `specialized/mobile/mobile-dev.md` - If mobile app planned

### Testing & Quality (3 agents)
- `testing/unit/test-unit.md` - Unit testing
- `testing/validation/prod-validator.md` - Production validation
- `analysis/code-review/analyze-code-quality.md` - Code quality analysis

### Documentation (2 agents)
- `documentation/api-docs/doc-api-specs.md` - API documentation
- `base-template-generator.md` - Template generation

### DevOps (2 agents)
- `devops/ci-cd/ops-cicd-github.md` - CI/CD pipeline
- `optimization/perf-analyzer.md` - Performance analysis

---

## ❌ REMOVE - Not Needed for Small Team (47 agents)

### Consensus/Distributed Systems (7 agents) - **OVERKILL**
```
❌ consensus/byzantine-coordinator.md - Byzantine fault tolerance (complex distributed systems)
❌ consensus/crdt-synchronizer.md - Conflict-free replicated data types
❌ consensus/gossip-coordinator.md - Gossip protocols
❌ consensus/performance-benchmarker.md - Consensus benchmarking
❌ consensus/quorum-manager.md - Quorum management
❌ consensus/raft-manager.md - Raft consensus algorithm
❌ consensus/security-manager.md - Distributed security
```
**Reason:** You're building a modular monolith, not a distributed consensus system. These are for blockchain/distributed databases.

### Swarm Coordination (20+ agents) - **TOO COMPLEX**
```
❌ swarm/adaptive-coordinator.md
❌ swarm/hierarchical-coordinator.md
❌ swarm/mesh-coordinator.md
❌ swarm/collective-intelligence-coordinator.md
❌ swarm/swarm-memory-manager.md
❌ swarm/smart-agent.md
❌ swarm/swarm-init.md
❌ github/code-review-swarm.md
❌ github/github-modes.md
❌ github/issue-tracker.md
❌ github/multi-repo-swarm.md
❌ github/pr-manager.md
❌ github/project-board-sync.md
❌ github/release-manager.md
❌ github/release-swarm.md
❌ github/repo-architect.md
❌ github/swarm-issue.md
❌ github/swarm-pr.md
❌ github/sync-coordinator.md
❌ github/workflow-automation.md
```
**Reason:** Designed for large-scale multi-repo GitHub management. You have 2-3 developers in single repo.

### SPARC Methodology (5+ agents) - **NICE-TO-HAVE**
```
⚠️ sparc/sparc-coord.md - SPARC orchestrator
⚠️ sparc/sparc-coder.md - SPARC-based coding
⚠️ sparc/specification.md - Specification phase
⚠️ sparc/pseudocode.md - Pseudocode phase
⚠️ sparc/architecture.md - Architecture phase
⚠️ sparc/refinement.md - Refinement phase
```
**Reason:** SPARC is a development methodology (Specification, Pseudocode, Architecture, Refinement, Completion).
**KEEP IF:** You want structured development workflow. **REMOVE IF:** Overkill for small team.

### Data/ML (2+ agents) - **NOT APPLICABLE**
```
❌ data/ml/data-ml-model.md - Machine learning models
❌ data/ml/data-ml-pipeline.md - ML pipelines
```
**Reason:** No ML requirements in your marketplace/forum/business profile features.

### Migration/Legacy (3+ agents) - **NOT APPLICABLE YET**
```
❌ optimization/migration-planner.md - Migration planning
❌ specialized/migration-specialist.md - Legacy migration
❌ specialized/legacy-modernizer.md - Legacy modernization
```
**Reason:** You're building new features, not migrating legacy systems.

### Template/Boilerplate (5+ agents) - **REDUNDANT**
```
⚠️ templates/automation-smart-agent.md
⚠️ templates/agent-template.md
⚠️ templates/specialized-agent-template.md
❌ templates/boilerplate-generator.md
❌ templates/code-scaffolder.md
```
**Reason:** Redundant with `base-template-generator.md`. **KEEP:** Only base-template-generator.

---

## 🟡 MAYBE KEEP - Evaluate Based on Team Size

### GitHub Integration (If using GitHub heavily)
```
⚠️ github/pr-manager.md - PR management (useful if lots of PRs)
⚠️ github/code-review-swarm.md - Automated code review (nice-to-have)
```
**Decision:** KEEP if 3+ developers making frequent PRs. REMOVE if solo/2 devs.

### SPARC Methodology (If structured process needed)
```
⚠️ sparc/* - All SPARC agents
```
**Decision:** KEEP if you want formalized SDLC. REMOVE if agile/ad-hoc development.

### Mobile Development
```
⚠️ specialized/mobile/mobile-dev.md
⚠️ specialized/mobile/mobile-testing.md
```
**Decision:** KEEP only if building mobile app. REMOVE if web-only.

---

## 📊 Cleanup Summary

| Category | Total | KEEP | REMOVE | MAYBE |
|----------|-------|------|--------|-------|
| Core Development | 5 | 5 ✅ | 0 | 0 |
| Backend/Architecture | 3 | 3 ✅ | 0 | 0 |
| Testing/Quality | 3 | 3 ✅ | 0 | 0 |
| Documentation | 2 | 2 ✅ | 0 | 0 |
| DevOps | 2 | 2 ✅ | 0 | 0 |
| Consensus/Distributed | 7 | 0 | 7 ❌ | 0 |
| Swarm/GitHub | 20 | 0 | 15 ❌ | 5 🟡 |
| SPARC Methodology | 6 | 0 | 0 | 6 🟡 |
| Data/ML | 2 | 0 | 2 ❌ | 0 |
| Migration/Legacy | 3 | 0 | 3 ❌ | 0 |
| Templates | 5 | 1 | 4 ❌ | 0 |
| Other | 4 | 0 | 4 ❌ | 0 |
| **TOTAL** | **62** | **15** ✅ | **35** ❌ | **11** 🟡 |

---

## 🎯 Recommended Agent Set for Modular Monolith (Small Team)

### Minimal Set (15 agents):
```
.claude/agents/
├── core/
│   ├── coder.md ✅
│   ├── planner.md ✅
│   ├── researcher.md ✅
│   ├── reviewer.md ✅
│   └── tester.md ✅
├── development/
│   └── backend/
│       └── dev-backend-api.md ✅
├── architecture/
│   └── system-design/
│       └── arch-system-design.md ✅
├── testing/
│   ├── unit/
│   │   └── test-unit.md ✅
│   └── validation/
│       └── prod-validator.md ✅
├── analysis/
│   └── code-review/
│       └── analyze-code-quality.md ✅
├── documentation/
│   └── api-docs/
│       └── doc-api-specs.md ✅
├── devops/
│   └── ci-cd/
│       └── ops-cicd-github.md ✅
├── optimization/
│   └── perf-analyzer.md ✅
└── base-template-generator.md ✅
```

### Archive Rest:
```
.claude/agents/archive/ (NEW)
├── consensus/ (7 agents - for distributed systems)
├── swarm/ (20 agents - for large teams/multi-repo)
├── sparc/ (6 agents - for SPARC methodology)
├── data-ml/ (2 agents - for ML projects)
├── migration/ (3 agents - for legacy migrations)
└── templates/ (4 agents - redundant templates)
```

---

## 🚀 Cleanup Script

```bash
# Create archive directory
mkdir -p .claude/agents/archive

# Move unwanted agent categories
mv .claude/agents/consensus .claude/agents/archive/
mv .claude/agents/swarm .claude/agents/archive/
mv .claude/agents/data .claude/agents/archive/
mv .claude/agents/specialized/migration* .claude/agents/archive/
mv .claude/agents/specialized/legacy* .claude/agents/archive/

# Move GitHub swarm agents (keep basic pr-manager)
mkdir -p .claude/agents/archive/github-swarm
mv .claude/agents/github/*-swarm.md .claude/agents/archive/github-swarm/
mv .claude/agents/github/multi-repo-swarm.md .claude/agents/archive/github-swarm/
mv .claude/agents/github/release-swarm.md .claude/agents/archive/github-swarm/
mv .claude/agents/github/sync-coordinator.md .claude/agents/archive/github-swarm/
mv .claude/agents/github/workflow-automation.md .claude/agents/archive/github-swarm/

# Move redundant templates (keep base-template-generator.md)
mkdir -p .claude/agents/archive/templates
mv .claude/agents/templates/* .claude/agents/archive/templates/
# Copy back base-template-generator if it was in templates
cp .claude/agents/archive/templates/base-template-generator.md .claude/agents/ 2>/dev/null || true

# Optional: Move SPARC agents (if not using SPARC methodology)
# mv .claude/agents/sparc .claude/agents/archive/

echo "Agent cleanup complete!"
echo "Reduced from 62 agents to 15 essential agents"
echo "Archived 47 agents to .claude/agents/archive/"
```

---

## 📝 Agent Usage Guide for Modular Monolith

### For Module Development:
```
Use: backend-dev (dev-backend-api.md)
For: Building Marketplace, Business Profile, Forum modules
```

### For Code Quality:
```
Use: reviewer (reviewer.md) + code-analyzer (analyze-code-quality.md)
For: Code reviews and quality checks
```

### For Testing:
```
Use: tester (tester.md) + unit-tester (test-unit.md)
For: Comprehensive test coverage
```

### For Architecture:
```
Use: system-architect (arch-system-design.md)
For: Module boundary design, database schema design
```

### For Planning:
```
Use: planner (planner.md)
For: Breaking down large features into tasks
```

### For Documentation:
```
Use: api-docs (doc-api-specs.md)
For: Swagger/OpenAPI documentation
```

---

## ✅ After Cleanup Benefits

**Before:** 62 agents (overwhelming, most unused)
**After:** 15 agents (focused, actually needed)

**Benefits:**
- ✅ Easier to understand which agent to use
- ✅ Faster agent selection
- ✅ Less confusion
- ✅ Cleaner repository
- ✅ Archived agents can be restored if needed later

---

## 🔄 Can Restore If Needed

If you later need:
- **Distributed systems:** Restore `archive/consensus/`
- **Large team coordination:** Restore `archive/swarm/`
- **SPARC methodology:** Restore `archive/sparc/`
- **ML features:** Restore `archive/data-ml/`

Just move back from archive to active directory.

---

**RECOMMENDATION:** Execute cleanup script to reduce from 62 → 15 agents before starting modular monolith implementation.
