# Architect Ruling — Multi-DbContext Implementation Comparison (Seventh Consult, 2026-07-04)

**Date**: 2026-07-04 (evening, ~13 hours into the day's DbContext incident chain)
**Participants**: Founder (Niroshana — direction-setter; explicit re-examination mandate), Executing Agent (Claude Opus 4.7), System Architect (Claude Opus 4.7 persona)
**Status**: BINDING — supersedes the sixth consult (`2026-07-04-dbcontext-direction-reversal-ruling.md`, "Option Gamma"). Retains ADR-005 (outbox-everything) with re-scoped implementation shape.
**Supersedes**: sixth consult Option Gamma verdict, `[[architect-dbcontext-plurality-ceiling-at-six]]` memory
**Preserves**: sixth consult §7 (retrospective conditions), §8 (Rule 5k codification), §4 (hotfix commits merge as-is)

---

## 0. Founder mandate

> "If we go for multi DB contexts, what context will be used for the shared modules or components?
> I like the multi DB context but, I am not convinced with the way your a planning and using it.
> If you implemented multi DB contexts, I don't know why you are recommending to revert it. I would like to see the implementation, challenges, benefits of Single DB context and multi DB context in the modular monolithic approach side by side comparison. Can you please pair with system-architect on above please?"

Founder DOES like multi-DbContext principle. Does NOT like execution. Wants coherent analysis, not another compromise.

---

## 1. Acknowledgment of oscillation (owned)

Executing agent's position has been unstable across seven consults today. Every turn optimized for what was easiest to explain THAT TURN rather than authoring a coherent architectural position. What the oscillation obscured: **no coherent multi-DbContext ownership model was ever written down.** Blueprint §D5 argued FOR multi-DbContext without saying which entities live where. Wave 6.5's scope required handler migration but no rule enforced it. When smoke failed, the recommendation was to revert rather than complete the migration.

The rest of this ruling is what should have shipped in the Blueprint §D5 authoring pass in June.

---

## 2. Q1 — Shared components ownership matrix

Every persisted type falls into exactly one of three categories:

### 2.1 Category VO — Value objects and typed IDs (SharedKernel; no DbContext)

Compiled-primitive types embedded via Owned-types / value converters. No DbContext ownership.

| Type | Assembly |
|---|---|
| `Money`, `Currency` | `SharedKernel.Money` |
| Cultural enums | `SharedKernel.Cultural` |
| `UserId` (typed ID) | `SharedKernel.Identity` |
| `GeoLocation`, `Address` | `SharedKernel.Geo` |
| `LocaleCode` | `SharedKernel.Locale` |

### 2.2 Category PLAT — Platform-cross-cutting entities (AppDbContext permanently)

Read/written by every module. If moved to a module context, breaks EF FK inference across the platform. **AppDbContext is their permanent owner.**

| Entity | Physical schema | Rationale |
|---|---|---|
| `User` + Identity children | `identity` | Every module's rows carry UserId FK. Cannot move without cross-context FK to Ignored principal. |
| `ReferenceValue`, `StateTaxRate` | `reference` | Read by every module. |
| `AdminAuditLog`, `SupportTicket` | `platform` | Cross-module. |
| `Badge`, `EventBadge` | `badges` | Junction crossing multiple contexts — AppDbContext-side is the only place both principals are mapped. |
| Stripe primitives | `payments` | Payment-adjacent, not extracted. |
| Communications entities (`EmailMessage`, `EmailTemplate`, `Newsletter`, `WhatsApp*`) | `communications` | Communications module not extracted (post-freeze, requires Blueprint amendment). |

### 2.3 Category MOD — Module-owned aggregates

Internal to a bounded context. Belong to module DbContext.

| Context | Aggregates | Status |
|---|---|---|
| NotificationsDbContext | `Notification` | Extracted Wave 3.5. Correct. |
| MediaDbContext | `PhotoAlbum`, `Photo`, `Video` | Extracted Wave 4.2. Handlers use `IMultiContextUnitOfWork`. Correct. |
| FormsDbContext | `Form`, `FormResponse` | Extracted Wave 4.3. Handlers use `IMultiContextUnitOfWork`. Correct. |
| LankaEventsDbContext | ~24 Event-family aggregates | Extracted Wave 6.5.e. **Handlers do NOT use `IMultiContextUnitOfWork`. Write-loss.** |

### 2.4 Deliberate NON-extractions (permanent)

**Identity** — `User` is FK'd from every module. Extracting creates `Ignore<User>()` ceremony in every module context. Stays in AppDbContext PERMANENTLY. If microservice extraction lands, Identity becomes a separate SERVICE not a separate context.

**Payments** — No PaymentsDbContext exists (sixth consult was factually wrong to list one). Payment intent / refund workflow tightly coupled to Registration + User. Cross-context transaction surface > isolation gain. Stays in AppDbContext PERMANENTLY.

**Corrected DbContext count: 5 today (not 6).** AppDbContext + LankaEventsDbContext + NotificationsDbContext + MediaDbContext + FormsDbContext.

### 2.5 Cross-context reads (Blueprint §7.8 codified)

Modules read cross-context data via `.Contracts` query interfaces, never by cross-mapping. LankaEvents needs User name → `IIdentityQueries` (declared in `Identity.Contracts`, implemented against AppDbContext). LankaEventsDbContext calls `Ignore<User>()` + FK columns stay scalar.

---

## 3. Q2 — Side-by-side comparison

| Dimension | Single-DbContext (AppDbContext-only) | Multi-DbContext (proper) |
|---|---|---|
| DbContext files | 1 (~820 lines) | 5 (AppDbContext ~500 + 4 module contexts ~200 each) |
| EF migrations | 1 folder | 5 folders; migration ordering matters when cross-schema FKs cross contexts |
| Model snapshots | 1 file | 5 files; drift risk × 5 |
| UnitOfWork API | `IUnitOfWork.CommitAsync(ct)` | `IMultiContextUnitOfWork.CommitAsync(DbContext[], ct)` + legacy shape |
| Handler change per extraction | 0 | ~105 handlers for LankaEvents Wave 6.5 |
| Cross-module read | Direct navigation | `.Contracts` query interface |
| Cross-module write | Single `SaveChanges` (atomic) | Multi-context via `UseTransaction` — atomic done right, write-loss done wrong |
| Deploy pipeline | 1 migration bundle | N migration applies |
| Add-a-module refactor | Low (AppDbContext grows) | High one-time cost; each module amortizes |
| Microservice extraction | 6-8 weeks per module | 1-2 weeks per module |
| Failure blast radius | Bug in AppDbContext hits every module | Bug in module DbContext contained |
| Existing prod data loss | 0 from topology | 0 shipped (write-loss bug on hotfix branch, NOT on develop) |
| Sessions consumed today | 0 (hypothetical) | ~15 (5 hotfixes + parallel-agent runs + 7 architect consults) |

**Honest summary**: Multi-DbContext done right is one full-fidelity DDD modular monolith pattern with real cost. LankaConnect burned ~15 sessions today because handler migration was documented but not enforced. Direction is not wrong; execution has a specific fixable gap.

Single-DbContext sacrifices microservice-extraction readiness (LankaConnect has not committed to it) for uniformity, simpler UoW, one migration pipeline. Works. Grows AppDbContext toward review-hotspot.

---

## 4. Q3 — Was Gamma the right call?

**No.** Gamma was a compromise driven by sunk cost.

Two things Gamma got wrong:
1. **Factually wrong count**. Listed Payments + Identity as existing contexts. They don't exist. Neither should.
2. **Deferred the real question**. Answered "should we extract more" (no) but never answered "is the current implementation correct" (no, it's write-losing).

Gamma's tactical utility: let five hotfix commits merge without another rewrite. That's preserved. What Gamma got wrong was framing the go-forward.

---

## 5. Q4 — What's wrong with the current implementation (four specific gaps)

**Gap 1 — Handler migration not enforced.** `IMultiContextUnitOfWork` exists. `LankaEventsDbContext` exists. But no rule fires when a repo cuts over while handlers still call `IUnitOfWork.CommitAsync(ct)`. **Evidence**: `grep IMultiContextUnitOfWork` in Products/LankaEvents/Application → 9 files (PhotoAlbum only). `grep _unitOfWork.CommitAsync(cancellationToken)` → 126 sites across 105 handlers. Write-loss surface.

**Gap 2 — Dual mapping has no exit gate.** Wave 6.5.f.6 cleanup planned but no rule enforces the exit.

**Gap 3 — Cross-context junctions need special ownership.** `EventBadge` (Event + Badge) and `EventEmailGroupLink` (Event + EmailGroup) must live in AppDbContext (only place both principals mapped). LankaEventsDbContext gets `Ignore<>()` + inferred FK defaults to Cascade (hotfix2c).

**Gap 4 — Wave 6.5.f.4 (payment cluster) never landed.** Baseline JSON should be at 4 remaining, not 20 nor 0.

---

## 6. Correct implementation path

### 6.1 Component 1 — Multi-context handler pattern

Every module-mutating command handler follows this template:

```csharp
public class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMultiContextUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _eventsContext;
    private readonly IIntegrationEventOutbox<LankaEventsDbContext> _outbox;

    public async Task<Result> Handle(UpdateEventCommand request, CancellationToken ct)
    {
        var evt = await _eventRepository.GetByIdAsync(request.EventId, ct);
        evt.Update(...);
        await _outbox.EnqueueAsync(new EventUpdatedIntegrationEventV1(...));
        await _unitOfWork.CommitAsync(new DbContext[] { _eventsContext }, ct);
        return Result.Success();
    }
}
```

### 6.2 Component 2 — Rule 5j.4 (new)

> **Rule 5j.4 — Handler migration audit on repo cutover**: When a repository's constructor changes DbContext type, every command handler that injects it MUST use `IMultiContextUnitOfWork` and inject the module DbContext explicitly. Enforced by architecture test + pre-push T-9 trigger. Mechanical, no introspection needed.

### 6.3 Component 3 — Complete Wave 6.5.f handler migration

Ships as new sub-slice Wave 6.5.f.7. ~105 LankaEvents handlers migrated to `IMultiContextUnitOfWork`. 3-4 sessions parallelizable per command folder cluster (Events, Registrations, Sponsors, Donations, Collections, AddOns, Sponsorships, SignUpItems, Tickets, VenueLayouts, SeatHolds/SeatReservations, RefundRequests). Rule 5j.4 test enforces gate.

### 6.4 Component 4 — Wave 6.5.f.6 dual-mapping cleanup

Post-6.5.f.7. Remove 24 `ApplyConfiguration` calls from AppDbContext.OnModelCreating for LankaEvents-family entities. Half-session.

### 6.5 Component 5 — Cross-context junction ownership (Blueprint §7.8 addition)

> Junction entities referencing principals in TWO different DbContexts MUST: be mapped in AppDbContext-side (both principals available); be `Ignore()`'d in the module context that owns one principal; FK stays scalar Guid in module context; AppDbContext-side declares BOTH FKs explicitly with `.OnDelete()` — never rely on `RelationshipDiscoveryConvention`.

Architecture test `Rule5i2_JunctionEntityExplicitOnDelete` scans configs.

---

## 7. Revised recommendation — Option Delta (replaces Gamma)

**Multi-DbContext, completed correctly.**

1. Existing 5 contexts stay (Notifications/Media/Forms/LankaEvents + AppDbContext). No revert.
2. **AppDbContext is a first-class permanent owner** of Category PLAT — not legacy.
3. Cross-context reads via `.Contracts` (Blueprint §7.8).
4. Cross-context writes via `IMultiContextUnitOfWork.CommitAsync(DbContext[])`. Enforced by Rule 5j.4.
5. Wave 6.5.f.7 handler migration completes (§6.3). 3-4 sessions.
6. Wave 6.5.f.6 dual-mapping cleanup follows. Half-session.
7. Junction-entity ownership codified (§6.5).
8. **NO Identity extraction, NO Payments extraction. Ever.** (§2.4).
9. Communications extraction allowed post-Wave 6.5 only with Blueprint amendment + founder ratification + Rule 5j.4 pre-audit clean gate.
10. Rule 5k retained (three same-family hotfixes → direction-reversal consult).

### 7.1 Scale conditions to authorize new extraction (any one)
- 3-8 concurrent contributors
- Committed microservice extraction date ≤12 months
- Data-residency requirements per module
- Postgres write throughput >2K writes/s sustained

Plus Rule 5j.4 pre-audit MUST show clean gate before cutover starts.

### 7.2 Cost comparison

| Path | Sessions | End state |
|---|---|---|
| Gamma (sixth consult) | 1 doc retrofit; NO write-loss fix | "Froze the number, didn't fix the bug" |
| Path B (revert Wave 6.5.f) | 3-4 sessions unwind | "Admit multi-DbContext was wrong" — contradicts founder mandate |
| **Delta (this ruling)** | 4-5 sessions handler migration + cleanup + rule codification | "Completing multi-DbContext correctly + mechanical enforcement forever" |

Delta = same cost as Path B, produces correct end state instead of rollback.

---

## 8. Wave 6.5 disposition

### 8.1 Prior 6 hotfix commits (hotfix1 through hotfix2e)

**Merge as-is** — sixth consult §4 preserved.

### 8.2 Write-loss bug (Wave 6.5.f.7 — new)

- **6.5.f.7.a** — Rule 5j.4 codified + architecture test + pre-push T-9 trigger. Test fails red as gate.
- **6.5.f.7.b** — Events + Registrations handler cluster (~30 handlers).
- **6.5.f.7.c** — Sponsors + Donations + Collections + AddOns (~25 handlers).
- **6.5.f.7.d** — Tickets + VenueLayouts + Seats (~20 handlers).
- **6.5.f.7.e** — SignUpItems + RefundRequests + remainder (~30 handlers).
- **6.5.f.7.f** — Complete Wave 6.5.f.4 payment cluster (4 repos + their handlers) with Rule 5j.4 enforcing.

4-5 sessions. Parallelizable per Rule 5l.

### 8.3 Wave 6.5.f.6 dual-mapping cleanup

Post-6.5.f.7. Half-session.

### 8.4 Wave 6.5.g + h

Unchanged from sixth consult. Application-layer refactors, orthogonal to topology.

### 8.5 Merge sequence

1. Wave 9 smoke green on hotfix2e
2. Land 6.5.f.7.a (Rule 5j.4 codification, test red)
3. Land 6.5.f.7.b through .f (handler migration cluster-by-cluster; test flips green)
4. Wave 9 smoke: 54 → 0
5. Merge hotfix branch (with 6.5.f.7 additions) to develop
6. Wave 6.5.f.6 cleanup on develop
7. Wave 6.5.g + h resume per 2026-07-02 plan

---

## 9. Process rules

**Rule 5j.4** (§6.2) — Handler migration audit on repo cutover. Mechanical.

**Rule 5j.5** — Cross-context junction entities require explicit `OnDelete()`. Architecture test scans configs.

**Rule 5j.6** — Dual-mapping windows require named exit ticket. Pre-push scan for `ApplyConfiguration` on entities present in module DbContext without `TODO(Wave-X.Y.Z-cleanup)`.

**Rule 5k** — Retained from sixth consult.

**Rule 5l** (new) — Parallel-worktree brief must enumerate migration-audit facts. This ruling would have caught today's bug at Wave 6.5.f.1 briefing.

---

## 10. Revisions to prior positions

- **Sixth consult**: §2 Gamma verdict superseded by Delta. §4 hotfix merge preserved. §6.1 Blueprint §D5 revision RESCINDED (D5 stays as originally authored + §D5.1 addendum this ruling). §6.7 memory `[[architect-dbcontext-plurality-ceiling-at-six]]` SUPERSEDED by `[[architect-dbcontext-multi-context-done-right]]`.
- **Blueprint §D5.1 (new)** — Category VO/PLAT/MOD ownership matrix per §2.
- **ADR-005 addendum revised** — "Multi-DbContext with per-module outbox for module aggregates; AppDbContext.outbox for platform events; handler migration enforced by Rule 5j.4."
- **Wave 4.0b/4.2/4.3 editor's notes** — reworded from "pattern retired" to "pattern is platform standard for MOD aggregates per §D5.1; no successors absent scale conditions."

---

## 11. Acceptance criteria

1. This ruling written to `docs/architect-consults/2026-07-04-multi-dbcontext-implementation-comparison-ruling.md`
2. Blueprint §D5.1 addendum authored
3. ADR-005 addendum revised
4. Rule 5j.4 codified + architecture test authored
5. Rule 5j.5 codified + architecture test
6. Rule 5j.6 codified + pre-push scan
7. Rule 5l codified
8. Wave 6.5.f.7 sub-slices added to master plan
9. Sixth consult ruling receives superseded header pointing here
10. Memory `[[architect-dbcontext-plurality-ceiling-at-six]]` retired; `[[architect-dbcontext-multi-context-done-right]]` created
11. Retrospective doc amendment section
12. Hotfix branch receives Wave 6.5.f.7 BEFORE develop merge; smoke 54→0
13. Wave 6.5.f.6 cleanup after 6.5.f.7 lands
14. NO extraction of Identity or Payments — ever, without founder-ratified Blueprint amendment

---

## 12. What NOT to do

- Do NOT revert Wave 6.5.e or Wave 6.5.f.1/2/3/5
- Do NOT extract Identity or Payments into own DbContext (permanent)
- Do NOT skip Rule 5j.4 architecture-test authoring
- Do NOT parallelize Wave 6.5.f.7 without embedding Rule 5j.4 audit in each worktree brief (Rule 5l)
- Do NOT rely on documentation alone — every rule has a mechanical gate
- Do NOT re-open Blueprint §D5 without a scale condition triggering
- Do NOT restart Gamma's "ceiling at 6" framing

---

## 13. Ruling summary

**Option Delta.** Multi-DbContext-per-module is the retained target pattern. Current implementation has execution gaps — ~105 LankaEvents handlers not migrated to `IMultiContextUnitOfWork`. Fixable in 4-5 sessions. Closed permanently by Rule 5j.4 (mechanical enforcement).

Founder's shared-components question answered by Category VO/PLAT/MOD matrix (§2). AppDbContext is first-class permanent owner of platform-cross-cutting types. Module DbContexts own bounded-context aggregates. SharedKernel value objects embedded, not persisted separately. Identity + Payments extraction PERMANENTLY rejected.

Sixth consult Gamma superseded. Gamma froze the count but dodged the substantive question. Executing agent's oscillation across seven consults owned (§1). Four new rules (5j.4/5j.5/5j.6/5l) codified — position enforced by tooling, not agent discipline.

Wave 6.5 five prior hotfix commits merge as-is. Wave 6.5.f.7 (new) completes handler migration + payment cluster. Wave 6.5.f.6 dual-mapping cleanup follows. Wave 6.5.g + h proceed per 2026-07-02 plan. Delta cost = 4-5 sessions, matches Path B revert cost with correct end state.

Founder is factually right: multi-DbContext done right is correct for LankaConnect at any scale where module boundaries matter. What was wrong was the implementation. This ruling fixes the implementation without unwinding the pattern.
