# Traceability Matrix

> **Every task in the platform maps through this hierarchy:**
>
> ```
> Task → Sub-slice → Wave → Phase → Platform Vision
> ```
>
> **No task, feature, bug fix, refactor, or architectural initiative may exist without a parent reference in this matrix.** If you cannot trace your proposed work back through every level, stop and consult the System Architect — the work is either out of scope or the plan needs an update first.

---

## How to use this matrix

### When you start a new TODO item

1. **Identify the parent Wave** in the active phase plan (e.g., Wave 5.4 in Phase A)
2. **Identify the parent Sub-slice** (e.g., Wave 5.4.b — DbContext partition for events schema)
3. **Add a row below** with: Task ID + Description + Sub-slice + Wave + Phase + Vision link + Status + Owner
4. **Commit the matrix update in the same commit** as the work-item ships, OR in the immediately preceding planning commit

### Hard rule

**New TODOs require a matrix row before commit.** Refactor commits exempt (no new tasks, just relocation/cleanup). Feature commits (new behavior, new API surface, new aggregate, new capability) require a matrix entry. Bug-fix commits inherit the matrix entry of the feature they're fixing.

If you find yourself doing work without a corresponding matrix row, that's the signal that either (a) the matrix needs updating BEFORE the work continues, or (b) the work is out of scope and should be paused for architect review.

### When you ship a task

Update the row's Status column: `PLANNED` → `IN-PROGRESS` → `SHIPPED <commit>` → `STAGING-VERIFIED <date>` → `CLOSED <date>`. Mirror the same status flip in the phase plan.

### When you discover a task you forgot to add

Backfill the matrix row immediately, even retroactively. Reference the commits in the Notes column. The matrix is an inventory check; the inventory must reflect reality.

---

## Vision references (used in the Vision column)

- **V1 — Multi-product platform** — single foundation hosting 7 customer-facing products
- **V2 — Zero re-architecture per new product** — Capability/Product topology means LankaTemples uses Scheduling without taking LankaEvents as a dep
- **V3 — Monolith-first, extract-when-needed** — modular boundaries make microservice extraction a deployment decision, not a code rewrite
- **V4 — Architect-paired single source of truth** — every plan/arch/doc change pairs with the System Architect; PLATFORM_MASTER_PLAN.md is the contract
- **V5 — Testing-discipline gated production** — every commit ships with T-trigger unit tests + S-class staging smoke; no production deploy without provable per-commit coverage

Every task must serve at least one vision reference.

---

## Active matrix

> **This is an empty template + 7 example rows seeded 2026-06-29.** Populate with live data as new TODOs are created. Agents responsible for new work-items are responsible for the matrix entry.

| Task ID | Description | Sub-slice | Wave | Phase | Vision | Status | Owner |
|---|---|---|---|---|---|---|---|
| EXAMPLE-001 | (template — delete after first real row added) | Wave X.Y.a | Wave X | Phase A | V1 | PLANNED | unassigned |
| W5.3.a1 | Relocate MetroAreaRepository to Products/LankaEvents.Infrastructure | Wave 5.3.a | Wave 5 | Phase A | V2 + V3 | SHIPPED `9be09e8a` | Planning Agent |
| W5.3.a2 | Bulk-move 3 leaf Event-family repos (TicketScanLog, EventNotificationHistory, EventReminder) | Wave 5.3.a | Wave 5 | Phase A | V2 + V3 | SHIPPED `bd33290a` | Planning Agent |
| W5.3.b | Bulk-move 8 Event-finance repos (AddOnDefinition, AddOnPurchase, Collection, Donation, Sponsor, SponsorshipPackage, RegistrationPayment, RegistrationAddition) | Wave 5.3.b | Wave 5 | Phase A | V2 + V3 | SHIPPED `a820df03` | Planning Agent |
| W5.3.c1 | Relocate 4 Event child-entity repos (VenueLayout, SeatHold, SeatReservation, Ticket) | Wave 5.3.c | Wave 5 | Phase A | V2 + V3 | SHIPPED `e43481cf` | Planning Agent |
| W5.3.c2 | Relocate EventRepository + RegistrationRepository (aggregate roots, 1,560 LOC) | Wave 5.3.c | Wave 5 | Phase A | V2 + V3 + V5 | SHIPPED `0047a6dd` | Planning Agent + Architect pairing |
| W4.9.6.a | Per-controller smoke for EventsController — gates W5.3 STAGING-VERIFIED flip | Wave 4.9.6.a | Wave 4.9 | Phase A | V5 | PLANNED | Planning Agent |
| W4.9.6.b-k | Per-controller smoke for remaining 41 controllers (~330 endpoints total) | Wave 4.9.6.b through k | Wave 4.9 | Phase A | V5 | PLANNED | Planning Agent |

---

## Example: filling out a new row

If a founder request lands tomorrow asking "add a hidden-fields setting for newsletter previews", the matrix entry might look like:

```
| NL-PREVIEW-1 | Add hidden-fields toggle to NewsletterPreviewSettings | Wave 5.4.c (TBD) | Wave 5 | Phase A | V1 + V4 | PLANNED | Planning Agent |
```

The matrix forces the agent to (a) identify which Wave/Sub-slice it belongs to BEFORE starting, (b) confirm the Wave exists in the phase plan, (c) confirm the work serves a Vision reference. If any of those fail, the answer is "consult the System Architect to update the plan FIRST" — not "just do it and add a matrix row later".

---

## Matrix maintenance

- **Updated** by the agent shipping the work (planning or implementation)
- **Reviewed** during the wave-close ceremony (when a Wave flips to STAGING-VERIFIED, audit the matrix entries for that wave)
- **Audited** at phase-close (when Phase A wraps, every shipped commit must have a corresponding matrix row OR be explicitly listed in PROGRESS_TRACKER as out-of-scope justified)

If the matrix gets large enough that scanning by hand is painful, split by Phase (e.g., `TRACEABILITY_MATRIX_PHASE_A.md`) — but founder must approve the split + the central matrix remains the entry point listing all per-phase sub-matrices.

---

*Authored 2026-06-29 by Planning Agent in pairing with the System Architect. Founder-mandated as part of the platform plan hierarchy.*
