# Master Requirements - LankaConnect Project

**Purpose:** Central tracking document for all epics and feature assignments to Senior Engineers.

**Last Updated:** 2026-01-24

---

## Epic Numbering System

```
Format: [PHASE].[EPIC_NUMBER]. [Epic Name]

Example:
10.A. Shopping Cart Implementation
10.B. Stripe Checkout Integration
10.C. Order Management System
```

---

## Active Epics

### Phase 10: Marketplace Module

| Epic ID | Epic Name | Status | Assigned To | Implementation Plan | Start Date | Target Date |
|---------|-----------|--------|-------------|---------------------|------------|-------------|
| 10.A | Shopping Cart Implementation | Not Started | Senior Engineer 1 | [docs/epics/10A-shopping-cart-plan.md](./epics/10A-shopping-cart-plan.md) | TBD | TBD |
| 10.B | Stripe Checkout Integration | Not Started | Senior Engineer 1 | [docs/epics/10B-stripe-checkout-plan.md](./epics/10B-stripe-checkout-plan.md) | TBD | TBD |
| 10.C | Product Catalog System | Not Started | Senior Engineer 1 | [docs/epics/10C-product-catalog-plan.md](./epics/10C-product-catalog-plan.md) | TBD | TBD |
| 10.D | Order Management System | Not Started | Senior Engineer 1 | TBD | TBD | TBD |
| 10.E | Inventory Management | Not Started | Senior Engineer 1 | TBD | TBD | TBD |
| 10.F | Shipping Label Generation | Not Started | Senior Engineer 1 | TBD | TBD | TBD |

### Phase 11: Business Profile Module

| Epic ID | Epic Name | Status | Assigned To | Implementation Plan | Start Date | Target Date |
|---------|-----------|--------|-------------|---------------------|------------|-------------|
| 11.A | Business Profile Domain Model | Not Started | Senior Engineer 2 | TBD | TBD | TBD |
| 11.B | Business Approval Workflow | Not Started | Senior Engineer 2 | TBD | TBD | TBD |
| 11.C | Business Directory & Search | Not Started | Senior Engineer 2 | TBD | TBD | TBD |
| 11.D | Business Services Management | Not Started | Senior Engineer 2 | TBD | TBD | TBD |
| 11.E | Admin Approval Panel | Not Started | Senior Engineer 2 | TBD | TBD | TBD |

### Phase 12: Forum Module

| Epic ID | Epic Name | Status | Assigned To | Implementation Plan | Start Date | Target Date |
|---------|-----------|--------|-------------|---------------------|------------|-------------|
| 12.A | Forum Domain Model | Not Started | Senior Engineer 3 | TBD | TBD | TBD |
| 12.B | Discussion Threads & Posts | Not Started | Senior Engineer 3 | TBD | TBD | TBD |
| 12.C | Content Moderation System | Not Started | Senior Engineer 3 | TBD | TBD | TBD |
| 12.D | User Reputation System | Not Started | Senior Engineer 3 | TBD | TBD | TBD |
| 12.E | Forum Search & Filters | Not Started | Senior Engineer 3 | TBD | TBD | TBD |

### Phase 13: Frontend Features

| Epic ID | Epic Name | Status | Assigned To | Implementation Plan | Start Date | Target Date |
|---------|-----------|--------|-------------|---------------------|------------|-------------|
| 13.A | Marketplace UI Pages | Not Started | Senior Engineer 4 | TBD | TBD | TBD |
| 13.B | Business Profile UI Pages | Not Started | Senior Engineer 4 | TBD | TBD | TBD |
| 13.C | Forum UI Pages | Not Started | Senior Engineer 4 | TBD | TBD | TBD |
| 13.D | Shopping Cart UI | Not Started | Senior Engineer 4 | TBD | TBD | TBD |
| 13.E | Checkout Flow UI | Not Started | Senior Engineer 4 | TBD | TBD | TBD |

### Phase 14: Events Module Refactor

| Epic ID | Epic Name | Status | Assigned To | Implementation Plan | Start Date | Target Date |
|---------|-----------|--------|-------------|---------------------|------------|-------------|
| 14.A | Events Module Restructure | Not Started | TBD | TBD | TBD | TBD |
| 14.B | Events Test Migration | Not Started | TBD | TBD | TBD | TBD |

---

## Epic Status Values

- **Not Started** - Epic created but no work begun
- **Planning** - Implementation plan being created
- **In Progress** - Active development
- **Code Review** - Implementation complete, under review
- **Testing** - In QA/testing phase
- **Deployed to Staging** - On staging environment
- **Deployed to Production** - Live in production
- **Blocked** - Waiting on dependency or decision
- **On Hold** - Paused for business reasons
- **Complete** - Fully done, verified, documented

---

## How to Use This Document

### When Starting a New Epic

1. **Assign Epic to Engineer**
   - Update "Assigned To" column
   - Set status to "Planning"
   - Update agent's personal .MD file (see docs/agents/)

2. **Create Implementation Plan**
   - Use template: [docs/epics/EPIC_PLAN_TEMPLATE.md](./epics/EPIC_PLAN_TEMPLATE.md)
   - Save as: `docs/epics/[EPIC_ID]-[epic-name-slug]-plan.md`
   - Link plan in "Implementation Plan" column

3. **Update Start Date**
   - Set to current date when planning begins

4. **Set Target Date**
   - Estimate completion date (realistic, not optimistic)

5. **Notify Engineer**
   - Use slash command or direct message
   - Reference epic ID and plan document

### During Epic Development

1. **Update Status Regularly**
   - Change status as epic progresses
   - Keep status current (daily updates if active)

2. **Engineer References**
   - Engineer checks their personal .MD file (docs/agents/senior-engineer-[N].md)
   - Engineer reads epic plan document
   - Engineer follows CLAUDE.md common rules

3. **Track Progress**
   - Epic plan document has detailed task breakdown
   - Engineer updates epic plan with completions
   - Engineer references back to epic plan if focus drifts

### When Epic Completes

1. **Update Status to "Complete"**
2. **Update PROGRESS_TRACKER.md**
3. **Update STREAMLINED_ACTION_PLAN.md**
4. **Update TASK_SYNCHRONIZATION_STRATEGY.md**
5. **Create epic summary document**
6. **Link summary in epic plan**

---

## Engineer Assignments

### Senior Engineer 1: Marketplace Module (Backend)
- **Scope:** All Marketplace backend features
- **Epics:** 10.A - 10.F
- **Agent File:** [docs/agents/senior-engineer-1.md](./agents/senior-engineer-1.md)
- **Invoke with:** `/senior-engineer-1` (or custom slash command)

### Senior Engineer 2: Business Profile Module (Backend)
- **Scope:** All Business Profile backend features
- **Epics:** 11.A - 11.E
- **Agent File:** [docs/agents/senior-engineer-2.md](./agents/senior-engineer-2.md)
- **Invoke with:** `/senior-engineer-2`

### Senior Engineer 3: Forum Module (Backend)
- **Scope:** All Forum backend features
- **Epics:** 12.A - 12.E
- **Agent File:** [docs/agents/senior-engineer-3.md](./agents/senior-engineer-3.md)
- **Invoke with:** `/senior-engineer-3`

### Senior Engineer 4: Frontend Features (All Modules)
- **Scope:** All UI pages for Marketplace, Business, Forum
- **Epics:** 13.A - 13.E
- **Agent File:** [docs/agents/senior-engineer-4.md](./agents/senior-engineer-4.md)
- **Invoke with:** `/senior-engineer-4`

---

## Epic Dependencies

### Cross-Epic Dependencies

Document dependencies here when one epic blocks another:

```
Example:
10.A (Shopping Cart) → BLOCKS → 13.D (Shopping Cart UI)
Reason: UI needs backend API complete

11.B (Approval Workflow) → DEPENDS ON → 11.A (Domain Model)
Reason: Workflow uses domain entities
```

**Current Dependencies:**
- 13.A depends on 10.C (Marketplace UI needs Product Catalog API)
- 13.D depends on 10.A (Cart UI needs Cart API)
- 13.E depends on 10.B (Checkout UI needs Stripe integration)
- 13.B depends on 11.A + 11.C (Business UI needs backend)
- 13.C depends on 12.A + 12.B (Forum UI needs backend)

---

## Quick Reference: Epic Workflow

```
1. Assign epic to engineer in this document (MASTER_REQUIREMENTS.md)
2. Update engineer's personal .MD file (docs/agents/senior-engineer-[N].md)
3. Engineer creates detailed implementation plan (docs/epics/[EPIC_ID]-plan.md)
4. Link plan back to this document
5. Engineer starts work, referencing:
   - CLAUDE.md (common rules)
   - docs/agents/senior-engineer-[N].md (their assignments)
   - docs/epics/[EPIC_ID]-plan.md (detailed plan)
6. You communicate via slash command: /senior-engineer-[N]
7. If engineer loses focus, you say: "Check your agent file and epic plan"
8. Engineer updates epic plan with progress
9. When complete, update status to "Complete" here
10. Update all PRIMARY tracking docs
```

---

## Document Change History

| Date | Change | Changed By |
|------|--------|------------|
| 2026-01-24 | Initial creation | Claude |
| | | |

---

**Next Step:** Create your first epic implementation plan using [EPIC_PLAN_TEMPLATE.md](./epics/EPIC_PLAN_TEMPLATE.md)
