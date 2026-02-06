# 4 Full-Stack Agent Workflow - Complete Guide

**Date:** 2026-01-24
**System:** Hybrid Modular Monolith with 4 Full-Stack Agents

---

## 🎯 Quick Summary

You will work interactively with **4 Full-Stack Agents** in parallel, each owning a complete module (Database → API → UI).

```
Your Workflow:
├── Window 1: Senior Engineer 1 (Events Module)      ← Backend + Frontend
├── Window 2: Senior Engineer 2 (Marketplace Module) ← Backend + Frontend
├── Window 3: Senior Engineer 3 (Business Module)    ← Backend + Frontend
└── Window 4: Senior Engineer 4 (Forum Module)       ← Backend + Frontend
```

**Each engineer ships complete features** - no waiting for APIs, no coordination overhead, no frontend bottleneck.

---

## 📋 Phase 0: Foundation (You Do This - 1-2 Days)

**Before starting any agent work, complete this foundation:**

### Step 1: Create Shared UI Component Library

```bash
# Create shared components directory
mkdir -p web/src/components/ui

# Create design tokens file
# web/src/lib/design-tokens.ts
export const colors = {
  primary: {
    blue: '#1E40AF',
    darkBlue: '#1E3A8A'
  },
  semantic: {
    success: '#10B981',
    error: '#EF4444',
    warning: '#F59E0B',
    info: '#3B82F6'
  },
  neutral: {
    gray50: '#F9FAFB',
    gray900: '#111827'
  }
};

export const typography = {
  fontFamily: {
    primary: "'Inter', sans-serif"
  },
  text: {
    xs: '0.75rem',    // 12px
    sm: '0.875rem',   // 14px
    base: '1rem',     // 16px
    lg: '1.125rem',   // 18px
    xl: '1.25rem',    // 20px
    '2xl': '1.5rem',  // 24px
    '4xl': '2.25rem'  // 36px
  }
};

export const spacing = {
  1: '0.25rem',  // 4px
  2: '0.5rem',   // 8px
  3: '0.75rem',  // 12px
  4: '1rem',     // 16px
  6: '1.5rem',   // 24px
  8: '2rem'      // 32px
};
```

### Step 2: Create Reusable Components

**From UI_STYLE_GUIDE.md, create these components:**

```tsx
// web/src/components/ui/Button.tsx
export function Button({ variant, size, children, ...props }) {
  // Implementation from UI_STYLE_GUIDE.md
}

// web/src/components/ui/Input.tsx
export function Input({ label, error, ...props }) {
  // Implementation from UI_STYLE_GUIDE.md
}

// web/src/components/ui/Card.tsx
export function Card({ children }) { /* ... */ }
export function CardHeader({ children }) { /* ... */ }
export function CardTitle({ children }) { /* ... */ }
export function CardContent({ children }) { /* ... */ }

// web/src/components/ui/Modal.tsx
export function Modal({ isOpen, onClose, children }) {
  // Implementation
}

// web/src/components/ui/Alert.tsx
export function Alert({ variant, children }) {
  // Implementation
}
```

### Step 3: Update CLAUDE.md with UI Consistency Rule

Add this section to [CLAUDE.md](../CLAUDE.md):

```markdown
## 🚨 SECTION 10: UI CONSISTENCY (MANDATORY FOR ALL AGENTS)

**Before ANY UI work:**
1. ✅ Read UI_STYLE_GUIDE.md completely
2. ✅ Import components from `web/src/components/ui/`
3. ✅ Use design tokens from `web/src/lib/design-tokens.ts`
4. ✅ NO custom components without approval

**UI Rule Violations = Immediate Refactor**

**Example - CORRECT:**
```tsx
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { colors, spacing } from '@/lib/design-tokens';

<Card>
  <div style={{ padding: spacing[4], color: colors.primary.blue }}>
    <Button variant="primary">Click Me</Button>
  </div>
</Card>
```

**Example - WRONG (Never do this):**
```tsx
// ❌ Custom styling
<button className="my-custom-button">Click Me</button>

// ❌ Hardcoded colors
<div style={{ color: '#1E40AF', padding: '16px' }}>

// ❌ Custom component without approval
<MyCustomCard>...</MyCustomCard>
```
```

### Step 4: Verify Foundation Complete

- [ ] All components in `web/src/components/ui/` created
- [ ] Design tokens in `web/src/lib/design-tokens.ts` created
- [ ] UI_STYLE_GUIDE.md finalized
- [ ] CLAUDE.md updated with Section 10 (UI Consistency)
- [ ] Test that components render correctly

---

## 🚀 Phase 1: Launch 4 Full-Stack Agents (Parallel)

### Opening Your 4 Windows

**In 4 separate Claude Code windows/terminals:**

```
Window 1: /senior-engineer-1
Window 2: /senior-engineer-2
Window 3: /senior-engineer-3
Window 4: /senior-engineer-4
```

### Initial Instructions for Each Agent

**Window 1 (Events):**
```
/senior-engineer-1 You're responsible for the Events module (Backend + Frontend).

Read your agent file: docs/agents/senior-engineer-1.md
Read CLAUDE.md sections 1, 2, 9, 10
Read UI_STYLE_GUIDE.md

Your first epic: 14.A (Events Module Restructure - Backend)

Create a detailed implementation plan using the template at:
docs/epics/EPIC_PLAN_TEMPLATE.md

Save as: docs/epics/14A-events-restructure-plan.md

Once plan is ready, start TDD workflow:
1. Write tests first
2. Refactor existing Events code into Clean Architecture
3. 90%+ test coverage
4. Deploy to staging

Let me know when your plan is ready.
```

**Window 2 (Marketplace):**
```
/senior-engineer-2 You're responsible for the Marketplace module (Backend + Frontend).

Read your agent file: docs/agents/senior-engineer-2.md
Read CLAUDE.md sections 1, 2, 9, 10
Read UI_STYLE_GUIDE.md

Your first epic: 10.C (Product Catalog - Backend + UI)

Create implementation plan:
docs/epics/10C-product-catalog-plan.md

Build backend first (TDD), then frontend (using shared UI components).

Let me know when your plan is ready.
```

**Window 3 (Business Profile):**
```
/senior-engineer-3 You're responsible for the Business Profile module (Backend + Frontend).

Read your agent file: docs/agents/senior-engineer-3.md
Read CLAUDE.md sections 1, 2, 9, 10
Read UI_STYLE_GUIDE.md

Your first epic: 11.A (Business Domain Model - Backend + UI)

Create implementation plan:
docs/epics/11A-business-domain-model-plan.md

Build backend first (TDD), then frontend (using shared UI components).

Let me know when your plan is ready.
```

**Window 4 (Forum):**
```
/senior-engineer-4 You're responsible for the Forum module (Backend + Frontend).

Read your agent file: docs/agents/senior-engineer-4.md
Read CLAUDE.md sections 1, 2, 9, 10
Read UI_STYLE_GUIDE.md

Your first epic: 12.A (Forum Domain Model - Backend + UI)

Create implementation plan:
docs/epics/12A-forum-domain-model-plan.md

Build backend first (TDD), then frontend (using shared UI components).

Let me know when your plan is ready.
```

---

## 💬 How to Communicate with Agents

### Regular Check-ins (Every 30 Minutes or 20-30 Messages)

**Paste this in each window:**
```
Status update:
- What have you completed?
- What are you working on now?
- Any blockers?
- Next steps?
```

### When Agent Loses Focus

**Paste the REFOCUS prompt:**
```
REFOCUS CHECKPOINT:

1. ✅ Re-read CLAUDE.md Sections 1, 2, 9, 10
2. ✅ Re-read UI_STYLE_GUIDE.md (if doing UI work)
3. ✅ Check your agent file (docs/agents/senior-engineer-[N].md)
4. ✅ Check your epic plan (docs/epics/[EPIC_ID]-plan.md)
5. ✅ Check todo list - what task are you on?
6. ✅ Recall: You're building [MODULE] end-to-end (Backend + Frontend)

DO NOT proceed until you've completed this checklist.
```

### When Epic Completes

**Verify completion:**
```
Before marking Epic [ID] complete, verify:

Backend:
- [ ] Domain models + tests (90%+ coverage)
- [ ] API endpoints working
- [ ] Database migrations applied
- [ ] Deployed to staging
- [ ] API tested with curl

Frontend:
- [ ] UI uses shared components from UI_STYLE_GUIDE.md
- [ ] Responsive design (desktop, tablet, mobile)
- [ ] API integration working
- [ ] Tested in browser

Documentation:
- [ ] Updated PROGRESS_TRACKER.md
- [ ] Updated STREAMLINED_ACTION_PLAN.md
- [ ] Updated TASK_SYNCHRONIZATION_STRATEGY.md
- [ ] Created epic summary document
- [ ] Updated Master Requirements (epic status → Complete)
- [ ] Build succeeds (0 errors)

Confirm all checked, then proceed to next epic.
```

---

## 🔄 Agent Workflow Per Epic

Each agent follows this pattern for every epic:

### Week 1: Backend Development
```
1. Read CLAUDE.md, agent file, epic plan
2. Create todo list (7-10 tasks)
3. TDD: Write failing tests
4. TDD: Implement domain models
5. TDD: Build application layer (commands, queries, handlers)
6. TDD: Create API controllers
7. Database migrations
8. Deploy to staging
9. Test API with curl
10. Mark backend tasks complete
```

### Week 2: Frontend Development
```
1. Re-read UI_STYLE_GUIDE.md
2. Import shared components
3. Build pages using Card, Button, Input, etc.
4. Implement API integration (call backend endpoints)
5. State management (Zustand stores)
6. Forms with validation
7. Error handling and loading states
8. Deploy to staging
9. Test in browser (desktop, tablet, mobile)
10. Mark frontend tasks complete
```

### Week 3: Integration & Deployment
```
1. End-to-end testing
2. Cross-browser testing
3. Performance testing
4. Fix any bugs
5. Update all documentation
6. Deploy to production
7. Mark epic complete
```

---

## 📊 Monitoring Progress

### Daily Progress Check (All 4 Windows)

Ask each agent:
```
Daily standup:
1. What did you complete yesterday?
2. What are you working on today?
3. Any blockers?
```

### Weekly Review (Every Friday)

**Check completed epics:**
```bash
# Review commits from all 4 agents
git log --oneline --graph --all --since="1 week ago"

# Check staging deployments
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health

# Review UI consistency
# Open staging frontend in browser
# Verify all modules look consistent
```

**If UI inconsistencies found:**
- Screenshot the inconsistency
- Tell affected agent: "Your UI doesn't match UI_STYLE_GUIDE.md. Fix X, Y, Z."
- Agent refactors to use correct components

---

## 🚨 Common Issues & Solutions

### Issue 1: Agent Forgets UI Guidelines

**Solution:**
```
REFOCUS: Re-read UI_STYLE_GUIDE.md completely.
You MUST use components from web/src/components/ui/.
NO custom styling.
Show me your code - I'll verify it follows the guide.
```

### Issue 2: Agent Loses Track of Epic

**Solution:**
```
Check your epic plan: docs/epics/[EPIC_ID]-plan.md
Check your agent file: docs/agents/senior-engineer-[N].md
What task number are you on? (e.g., "Task 5 of 10")
Resume where you left off.
```

### Issue 3: Build Errors

**Solution:**
```
STOP. Before proceeding:
1. Run: dotnet build
2. Fix ALL errors (0 errors required)
3. Run: npm run build (for frontend)
4. Fix ALL TypeScript errors
5. Only then continue development

TDD Rule: Code MUST compile and tests MUST pass.
```

### Issue 4: Agent Wants to Skip Tests

**Solution:**
```
NO. TDD is MANDATORY.
Read CLAUDE.md Section 2 again.

You MUST:
1. Write failing test first (RED)
2. Write code to pass test (GREEN)
3. Refactor (REFACTOR)
4. Repeat for next feature

90%+ coverage required. No exceptions.
```

---

## ✅ Success Metrics

**After 4 Weeks of Parallel Development:**

- [ ] **Events Module Complete**
  - Backend refactored to Clean Architecture
  - UI enhanced and consistent
  - Tests: 90%+ coverage
  - Deployed to production

- [ ] **Marketplace Module Complete**
  - Product catalog, cart, checkout, orders working
  - Stripe integration functional
  - UI matches design system
  - Deployed to production

- [ ] **Business Profile Module Complete**
  - Business directory, approval workflow working
  - Reviews and ratings functional
  - UI consistent with design system
  - Deployed to production

- [ ] **Forum Module Complete**
  - Forums, topics, posts, moderation working
  - Real-time updates via SignalR
  - UI consistent with design system
  - Deployed to production

- [ ] **UI Consistency Verified**
  - All 4 modules use same components
  - Same colors, fonts, spacing
  - Responsive on all devices
  - Accessible (WCAG 2.1 AA)

- [ ] **Documentation Complete**
  - All epics marked complete in Master Requirements
  - PROGRESS_TRACKER.md updated
  - STREAMLINED_ACTION_PLAN.md updated
  - TASK_SYNCHRONIZATION_STRATEGY.md updated
  - Epic summaries created

---

## 📖 Reference Documents

**For You (Orchestrator):**
- [4_FULL_STACK_AGENT_WORKFLOW.md](./4_FULL_STACK_AGENT_WORKFLOW.md) - This guide
- [PARALLEL_AGENT_COORDINATION.md](./PARALLEL_AGENT_COORDINATION.md) - Original coordination strategy
- [Master Requirements Specification.md](./Master%20Requirements%20Specification.md) - Epic tracking

**For All Agents:**
- [CLAUDE.md](../CLAUDE.md) - Common mandatory rules
- [UI_STYLE_GUIDE.md](./UI_STYLE_GUIDE.md) - Design system (CRITICAL!)
- [REFOCUS_PROMPT.md](./REFOCUS_PROMPT.md) - When agent loses focus

**For Each Agent:**
- [senior-engineer-1.md](./agents/senior-engineer-1.md) - Events module owner
- [senior-engineer-2.md](./agents/senior-engineer-2.md) - Marketplace module owner
- [senior-engineer-3.md](./agents/senior-engineer-3.md) - Business module owner
- [senior-engineer-4.md](./agents/senior-engineer-4.md) - Forum module owner

---

## 🎯 Next Steps

1. ✅ **Phase 0 Complete?** - Check foundation checklist above
2. ✅ **Open 4 Windows** - Launch 4 Claude Code sessions
3. ✅ **Assign First Epics** - Give each agent their starting epic
4. ✅ **Monitor Daily** - Check progress, refocus when needed
5. ✅ **Review Weekly** - Verify UI consistency, test integration
6. ✅ **Ship to Production** - Deploy complete features continuously

---

**Your system is ready! Time to build. 🚀**
