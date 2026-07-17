# Agent Channel: UoWRetire

**Agent role:** Wave 8.5.h — Retire `IMultiContextUnitOfWork.CommitAsync(DbContext[])` per Tech Lead D-01.
**Priority:** P1 (unblocks Phase B cross-module writes per architect Q4.a)
**Est time:** 2 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

Per Tech Lead D-01: RETIRE `IMultiContextUnitOfWork.CommitAsync(DbContext[])`. Do not fix the shared-connection Npgsql pooling issue. Callers migrate to per-context direct-SaveChanges + integration events / saga.

Architect Consult #25 Q6 blanket-approved single-context direct-SaveChanges as the pattern going forward. Multi-context UoW method's shared-connection fix is 1-2 days; retire is ~2 hours.

## Deliverable

### Part 1 — Caller audit

1. Grep every caller of the multi-context CommitAsync overload:
   ```bash
   grep -rn "IMultiContextUnitOfWork\|_unitOfWork\.CommitAsync(new DbContext\[\]\|_unitOfWork\.CommitAsync(new\[\]" src/ --include="*.cs"
   ```
2. Also grep the `Save(DbContext[])` variant if any exists.
3. Enumerate every hit: file path + method + which contexts are passed in the array.
4. Post enumeration to channel BEFORE modifying any caller.

### Part 2 — Per-caller retire

For each caller:

**Case A — All array entries are the SAME context**: refactor to single direct-SaveChanges on that context. Trivial.

**Case B — Different contexts written atomically together**: this is the real Wave 8.5.h case. Options:
- **B.1** If the writes are logically SEPARABLE (e.g. write EventCreated to LankaEventsDbContext + then EventCreatedIntegrationEvent to outbox in same context): refactor to per-context SaveChanges + integration event. Integration event triggers downstream projection in the OTHER context.
- **B.2** If writes are TRULY atomic (rare): FLAG blocker for Tech Lead — needs saga infrastructure decision.

### Part 3 — Interface + impl deletion

Once ALL callers are refactored:
1. Delete `IMultiContextUnitOfWork.CommitAsync(DbContext[])` method from `IMultiContextUnitOfWork` interface (find via grep).
2. Delete the impl method from `UnitOfWork` class (should be in BuildingBlocks.Infrastructure or LankaConnect.Infrastructure).
3. Update the interface XML doc: "Retired 2026-07-16 per Tech Lead D-01. Use per-context SaveChanges + integration events for cross-context propagation. See docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md."
4. Add ArchTest rule: `ForbidsUseOfIMultiContextUnitOfWork_CommitAsync_DbContextArray` — post-retire enforcement so no Phase B agent re-adds it.

### Part 4 — Commit + push

- 1 commit per caller-refactor batch (or 1 combined if <10 callers) + 1 commit for interface deletion + ArchTest rule.
- Commit body: `Wave 8.5.h — retire IMultiContextUnitOfWork.CommitAsync(DbContext[]) per Tech Lead D-01`
- `T-triggers: T3 (handler behavior), T6 (DI/interface change)`
- `S-class: S2 (mutator smoke via Wave 9)`
- Push to `develop`.

## Constraints

- **DO NOT** touch handlers being migrated by HandlerMigration-A/B/C for their Wave 8.5.g direct-SaveChanges work — they own those. Your work is orthogonal (interface + impl deletion + any legit cross-context callers).
- **COORDINATE via channel** if you find a handler that HandlerMigration-A/B/C should own OR a multi-context caller they might otherwise trip over.
- If Part 2 Case B.2 fires (truly-atomic cross-context write with no saga path), FLAG blocker + terminate; Tech Lead escalates to founder.

## Communication protocol

- Post caller enumeration first.
- Post per-caller refactor summary.
- Post interface + impl deletion commit SHA.
- Post `STATUS: COMPLETE` at bottom.

## Log

*(Agent writes progress below this line.)*

### 2026-07-17 — Wave-2 Agent-UoWRetire spawn

**Post-HandlerMigration-A/B/C state:** LankaEvents handlers already migrated to direct-SaveChanges (commits `451248b4`, `c50b434d`, `5727cf43`, `9b3c1b8a`, `1c927152`, `3c4ed694`, `04418850`, `5e71f09e`, `bb6f7d35`). None of them touched Forms, Identity, or the multi-context array-form callers. Scope is clean.

### Part 1 — Caller enumeration (live invocations of `_unitOfWork.CommitAsync(new DbContext[] {...}, ct)`)

**Grep pattern used:** `CommitAsync\s*\(\s*new\s+DbContext\[\]|CommitAsync\s*\(\s*new\[\]`

**Case A — Single-context callers (16 files, 16 sites):**

Forms (13 handlers, all pass `new DbContext[] { _formsContext }`):
1. `src/Modules/Forms/Forms.Application/Commands/AddFormQuestion/AddFormQuestionCommandHandler.cs:111`
2. `src/Modules/Forms/Forms.Application/Commands/CloseEventForm/CloseEventFormCommandHandler.cs:84`
3. `src/Modules/Forms/Forms.Application/Commands/CreateEventForm/CreateEventFormCommandHandler.cs:135`
4. `src/Modules/Forms/Forms.Application/Commands/DeleteEventForm/DeleteEventFormCommandHandler.cs:82`
5. `src/Modules/Forms/Forms.Application/Commands/DeleteFormQuestion/DeleteFormQuestionCommandHandler.cs:85`
6. `src/Modules/Forms/Forms.Application/Commands/DeleteFormResponse/DeleteFormResponseCommandHandler.cs:163`
7. `src/Modules/Forms/Forms.Application/Commands/PublishEventForm/PublishEventFormCommandHandler.cs:89`
8. `src/Modules/Forms/Forms.Application/Commands/ReopenEventForm/ReopenEventFormCommandHandler.cs:84`
9. `src/Modules/Forms/Forms.Application/Commands/ReorderFormQuestions/ReorderFormQuestionsCommandHandler.cs:84`
10. `src/Modules/Forms/Forms.Application/Commands/SubmitFormResponse/SubmitFormResponseCommandHandler.cs:247`
11. `src/Modules/Forms/Forms.Application/Commands/UpdateEventForm/UpdateEventFormCommandHandler.cs:93`
12. `src/Modules/Forms/Forms.Application/Commands/UpdateFormQuestion/UpdateFormQuestionCommandHandler.cs:116`
13. `src/Modules/Forms/Forms.Application/Commands/UpdateFormResponse/UpdateFormResponseCommandHandler.cs:289`

Identity (3 handlers, pass `new DbContext[] { _notificationsContext }`):
14. `src/Modules/Identity/Identity.Application/Commands/Users/ApproveRoleUpgrade/ApproveRoleUpgradeCommandHandler.cs:186`
15. `src/Modules/Identity/Identity.Application/Commands/Users/AdminUpgradeUser/AdminUpgradeUserCommandHandler.cs:191`
16. `src/Modules/Identity/Identity.Application/Commands/Users/RejectRoleUpgrade/RejectRoleUpgradeCommandHandler.cs:129`

**No `Save(DbContext[])` variant exists** — grep for that pattern returned no live callsites (only the `SaveChangesAsync` calls unrelated to this API).

### Case-analysis

**Case A retire (all 13 Forms handlers):** Forms writes only touch FormsDbContext. The multi-context UoW's implicit AppDbContext save was a no-op. Refactor is 1-line: swap `await _unitOfWork.CommitAsync(new DbContext[] { _formsContext }, ct)` → `await _formsContext.SaveChangesAsync(ct)`. Wave 8.5.f interceptor is wired on FormsDbContext (per D-08) so domain events dispatch correctly.

**Case B → treated as Case B.1 (3 Identity handlers):** Writes span:
- User (IdentityDbContext) — **currently NOT saved** by the multi-context call, because the array only contains `_notificationsContext` and the internal `_context.CommitAsync` fires on AppDbContext. This is a live split-brain bug matching the `RegisterUserHandler` sprint-Day-9 fix (commit `c20a39de`) and `CreateEventCommandHandler` sprint-Day-7 fix.
- AdminAuditLog (AppDbContext) — saved via internal `_context.CommitAsync`
- Notification (NotificationsDbContext) — saved as explicit module ctx

Retire = 3 sequential direct SaveChanges (IdentityDbContext then AppDbContext then NotificationsDbContext), each with its own interceptor coverage. Fixes the User-write drop as a side effect. **NOT a saga escalation** — atomicity across the 3 contexts was already broken pre-retire; direct-SaveChanges is the accepted pattern per Consult #25 Q6.

Cross-module comment/docstring/csproj references (not live callers) — leave the docstrings for now; interface-deletion step will trigger a compile-clean sweep that scrubs any remaining referenced type-names.

Beginning refactor.

