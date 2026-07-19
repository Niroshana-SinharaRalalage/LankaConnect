# Agent Channel: BuildRestore

**Agent role:** Wave 8.5.e — Restore full-solution build in `deploy-staging.yml` per Consult #18 Day 10 debt.
**Priority:** P3 (~3h scope; independent)
**Est time:** 3 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

Sprint bible §Day 6 landed Consult #18 workflow-scope transitional at commit `d51496b6`: `.github/workflows/deploy-staging.yml` step 5 narrowed to `dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release --no-restore` + `continue-on-error: true` on Run unit tests.

Restore full-solution build once test-project compile errors (72 files as of Day 6 EOD; some resolved during Day 7-10) are down to zero.

TRACEABILITY_MATRIX row: SPRINT-D10.1.

## Deliverable

### Part 1 — Test-project compile-error audit

Run `dotnet build LankaConnect.sln -c Release --no-restore 2>&1 | tee /tmp/build.log` and grep error count:
```bash
grep -c "error" /tmp/build.log
```

Report per-project error count.

### Part 2 — Fix compile errors

For each compile error:
- **Missing type reference** — add ProjectReference or `using` statement
- **Renamed type** (Consult #12/13/17 aftermath) — update reference
- **Deleted type** (Business aggregate per Consult #12 Option D) — delete test method or update assertion
- **Namespace move** (Consult #15 PASS C aftermath) — update `using` statement

Group fixes by root-cause pattern. Batch commit ~20 files per commit.

### Part 3 — Restore workflow

Once `dotnet build LankaConnect.sln -c Release` → 0 errors:
1. Edit `.github/workflows/deploy-staging.yml` step 5:
   - Change target: `src/LankaConnect.API/LankaConnect.API.csproj` → `LankaConnect.sln`
   - Remove `--no-restore` flag (full-restore now safe)
2. Remove `continue-on-error: true` from the "Run unit tests" step
3. Verify: push to develop → observe `deploy-staging.yml` run → step 5 + step 6 both green

### Part 4 — Amend if Agent-ApiRename ran before you

If ApiRename shipped its rename before you: target becomes `LankaConnect.sln` (unchanged) but any lingering path assumptions in the workflow need audit.

### Commit

- 1 commit per fix batch + 1 commit for workflow restore
- Body: `Wave 8.5.e — <part-summary>`
- `T-triggers: T5 (test refactor) + T6 (workflow config change)`
- `S-class: S1 (deploy verify — full-solution build green on staging)`
- Push to `develop`.

## Constraints

- **DO NOT** touch production code to fix test-project errors — fix in the test project only.
- **DO NOT** disable tests to make them compile.
- If a test genuinely cannot be repaired (references deleted aggregate + test intent obsolete): DELETE the test file with reason.
- **COORDINATE** with ResidualFails (Wave 2) — they own Wave 9 smoke green; your work is unit-test green.

## Communication protocol

- Post per-project error counts first.
- Post fix commit SHAs per batch.
- Post workflow-restore commit SHA + first successful deploy-staging.yml run URL.
- Post `STATUS: COMPLETE` at bottom.

## Log

*(Agent writes progress below this line.)*

---

### 2026-07-18 — Batch 1 shipped

**Commit:** `a2eacbd8` — Wave 8.5.e — test-project foundation compile-fix batch 1

Fixed 3 test projects (12+5+5=22 compile errors → 0):
- `LankaConnect.Domain.Tests` — 12 errors → 0
  - `LankaConnect.Domain.Tests.csproj` — added ProjectReferences to `LankaEvents.Domain` + `SharedKernel.Money`
  - `Common/IAuditableAggregateRoundTripTests.cs` — aliased UserEmail to LankaEvents.Domain.ValueObjects.Email (User.Create takes that flavor)
  - `Common/LegacyBaseEntityCtorTests.cs` — added `[SetsRequiredMembers]` on TestEntity ctors chaining LegacyBaseEntity ctors (Consult #13 Q1)
- `LankaConnect.TestUtilities` — foundation
  - `GlobalUsings.cs` — removed stale BuildingBlocks.Domain.Shared / Domain.Common / Domain.Business globals
  - `Builders/EmailTestDataBuilder.cs` — UserEmail alias → Communications.Domain.ValueObjects.UserEmail
- `Notifications.Domain.Tests` — 5 errors → 0
  - `NotificationBehaviorTests.cs` — `Result.Error` is string; assert `.Should().Contain("Notification.<Code>")` instead of non-existent `.Error.Code`
- `Forms.Application.Tests` — 5 errors → 0
  - `Commands/DeleteFormResponseCommandHandlerTests.cs` — retire IMultiContextUnitOfWork.CommitAsync(DbContext[]); handler now takes `FormsDbContext` directly per Consult #25 direct-SaveChanges; test mocks FormsDbContext via Moq (jsonb model not InMemory-compatible)

### 2026-07-18 — Batch 2 status

**Batch 2 outcome:** `dotnet build LankaConnect.sln -c Release --no-restore` → **4 errors, ALL in `src/`, ZERO in `tests/`**.

Errors are in-flight LegacyPromotionsSplit LankaEvents work (folder split from `Contracts.LegacyPromotions` → `Contracts.Repositories/Services/DTOs/Shims`):

```
src/Modules/Payments/Payments.Application/Services/RefundReconciliationService.cs:42,50
  — fully-qualified `LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions.IRefundRequestRepository`
    needs to become `.Contracts.Repositories.IRefundRequestRepository`
src/Products/LankaEvents/LankaEvents.Infrastructure/Services/RegistrationEmailService.cs:275,279
  — same fully-qualified LegacyPromotions.<Type> to Contracts.<subfolder>.<Type>
```

These 4 refs sit inside the LegacyPromotionsSplit agent's working set (they've already shipped Communications `2aed1ded` + Media `ba25bc4e`, LankaEvents still uncommitted in working tree).

**Test-project scope is compile-clean.** All test projects under `tests/` build against the current working-tree src state (with all parallel-agent WIP applied). No test-project errors remain in my scope.

**Workflow restore BLOCKED on LegacyPromotionsSplit's LankaEvents commit.** Per brief constraint "DO NOT touch production code to fix test-project errors — fix in the test project only" + "COORDINATE with parallel agents", I am not clobbering LegacyPromotionsSplit's uncommitted work. Once they commit LankaEvents rewrite (4 refs), I restore `.github/workflows/deploy-staging.yml` step 5 to `LankaConnect.sln` + drop `continue-on-error: true`.

**Uncommitted working-tree state observed** (not my work, coordinating with parallel agents):
- 129 test-file changes (BuildingBlocks.Domain.Shared.ValueObjects.Email → Modules.Communications.Domain.ValueObjects.Email rename; also BOM/em-dash mojibake from editor round-trip — cosmetic only, comments only)
- 70 src-file changes (LegacyPromotionsSplit LankaEvents split + LayerInversion + Gap-Closure-Geo WIP)
- 33 untracked files (SharedKernel.Geo VOs + coordination channel logs + architecture consults)

**STATUS: PARTIAL** — Test-project compile-fix complete (Batch 1 shipped `a2eacbd8`; Batch 2 confirmed zero test-project errors remain). Workflow restore blocked on LegacyPromotionsSplit LankaEvents commit landing (4 residual src/ CS0234 refs).

Next agent picking this up: verify `dotnet build LankaConnect.sln -c Release --no-restore 2>&1 | grep -c "error CS"` returns 0, then restore workflow per Part 3 of brief.
