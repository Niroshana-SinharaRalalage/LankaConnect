# Testing Discipline Ruling — Founder Mandate + Architect Framework

**Date**: 2026-06-08
**Trigger**: founder mandate during Wave 4.9 execution after discovering ~40 commits in Waves 0-4 shipped with read-only smoke and no proactive test-writing.
**Status**: ACTIVE. Encoded in CLAUDE.md §13. Forcing functions land alongside Gap G0.

---

## The Founder Mandate (verbatim, 2026-06-07)

> "Refactoring is not just making code changes and structural changes, it is all about writing unit test, fixing and updating existing unit tests and test via APIs (smoke testing). You will complete the refactoring and report as done, but then the whole system won't work if we go this way.
>
> So from Wave 0 to Wave 8, we should always:
> 1. Fix existing unit test and write new unit test if needed
> 2. After changing each testable unit API testing must be conducted.
> 3. If those are not in the current plan and TODO list, immediately consult system-architect and update them asap. And then proceed.
> 4. If you have missed testing and unit testing for wave 0 to wave 4, you better go back and fill all those testing gaps."

---

## The new definition of "done"

> "Done" no longer means "compiles + reads green on staging".
>
> **Done means: the testable unit has a unit test exercising the new behavior AND a staging API call that actually invokes that code path in runtime.**

---

## SECTION A — Forward-Looking Discipline

### A.1 — Unit Test Mandatory Triggers (T1 — T8)

A new or modified unit test is **MANDATORY** when any of the following are true for a single commit. Triggers stack — a commit that fires two of them needs tests covering both.

| Trigger | Test that MUST exist before commit |
|---|---|
| **T1** New public method on a domain entity / aggregate / value object | `Method_Scenario_ExpectedBehavior` test in matching `*Tests/Domain/`. Coverage: happy path + one error/guard path. |
| **T2** New or changed mutator touching `IAuditable` / domain events / state-machine transitions | Test asserts: (a) field changed, (b) `UpdatedAt` set within last 5 sec, (c) any domain event raised with expected payload. |
| **T3** New or changed Command/Query handler | Application-layer test with mocked deps; assertions over response object AND `IDispatcher`/`IRepository` call. |
| **T4** New or changed EF Core configuration (`ToTable`, `HasColumnName`, `Property`, `Ignore`, `ValueComparer`, `HasConversion`) | Infrastructure-layer test that round-trips an entity through in-memory or sqlite provider; asserts column written + read back. **Catches the Phase 6A.123 NULL-default, Phase 6A.129 JSONB silent-revert, today's `42703 CreatedBy does not exist` class.** |
| **T5** New or changed REST endpoint signature (route, verb, request body, response DTO) | API integration test in `LankaConnect.IntegrationTests` using `WebApplicationFactory`. Assertions: status + response shape + ≥1 DB side-effect. |
| **T6** New or changed DI registration / DbContext registration / interceptor registration | "Container builds and resolves the seed services" test in `LankaConnect.Infrastructure.Tests`. Per `[[feedback-di-test-failures-are-real]]`. |
| **T7** Namespace move / type relocation (Wave 2/3/4 pivots) | NO new test required; existing tests referencing the moved type MUST compile + pass, and the commit posts `dotnet test` evidence in its message. |
| **T8** EF Core migration add (any class file in `Data/Migrations/`) | Migration-correctness test in `LankaConnect.Infrastructure.Tests/Database/` asserting snapshot LOC delta matches migration intent (additive-only / destructive-with-header / etc.). |

**Counter-triggers (test NOT required):**
- C1. Pure namespace alias / `using` directive change
- C2. Comment / doc-string change
- C3. `.gitignore` / `.editorconfig` / build-script change
- C4. csproj reference move (compiler validates; build IS the test)

**The "I'll write the test next commit" pattern is FORBIDDEN.**

### A.2 — Smoke Coverage Matrix (S1 — S6)

A passing `dotnet test` + a 200 on a read endpoint **is not** API smoke. Smoke must exercise the **WRITE path** that the commit's code is in.

| Class | Commit shape | Required smoke before STAGING-VERIFIED |
|---|---|---|
| **S1** Read-only refactor (namespace move, getter rename) | One GET list + one GET detail; assert 200 + non-empty payload. |
| **S2** Mutator refactor (mutator body changed) | (a) CREATE via POST, (b) GET re-fetch, (c) assert `createdAt` ≤60s old AND `updatedAt == createdAt` on fresh create, (d) PATCH/PUT, (e) GET re-fetch, (f) assert `updatedAt > createdAt`. **This catches the silent `CreatedAt = MinValue` bug on day one.** |
| **S3** EF config / `Ignore` / mapping change | (a) POST against affected resource, (b) assert 201, (c) GET, (d) assert all expected fields, (e) inspect container logs — any `42703`/`22P02`/`NpgsqlException` in last 60s = smoke FAIL. |
| **S4** New endpoint | Full lifecycle: POST → GET → PATCH → DELETE (or status transition) with per-step assertions, plus log inspection per S3. |
| **S5** Schema migration (Phase 1.x, 2.x of Wave 4.9) | (a) `\d <table>` probe pre-deploy, (b) deploy, (c) `\d` probe post-deploy assert change, (d) run S2 or S3 smoke against an affected resource. |
| **S6** Module-DbContext touch (Notifications/Media/Forms pivot) | Negative-evidence smoke per `[[feedback-email-smoke]]`: trigger module's write path, probe `\d <schema>.<table>` to confirm the row was written via the new context. |

### A.3 — Pre-Commit Checklist (encode in CLAUDE.md §13.1)

```
PRE-COMMIT CHECKLIST
  [ ] 1. dotnet build LankaConnect.sln          → 0 Error(s)
  [ ] 2. dotnet test                            → 0 failed (baseline must hold)
  [ ] 3. T-trigger audit:
        - Identify T-triggers fired by this commit
        - Confirm each fired trigger has a test ADDED or MODIFIED in the staged diff
        - List T-numbers + matching test file paths in the commit message body
  [ ] 4. S-class plan:
        - If any S-class applies, list the scripts/smoke/*.ps1 that will run post-deploy
        - (Smoke executes post-push; the COMMIT MESSAGE names the smoke that will run.)
  [ ] 5. Architect-consult flag:
        - If T-triggers extend beyond current MASTER_TODO scope,
          stop + consult system-architect + update plan BEFORE committing.
```

### A.4 — Pre-Deploy Verification (encode in CLAUDE.md §13.2)

```
PRE-DEPLOY VERIFICATION
  [ ] 1. deploy-staging.yml run shows: build OK, all 4 DbContexts apply OK, container started OK
  [ ] 2. scripts/smoke/Invoke-Login.ps1          → 200 + bearer obtained
  [ ] 3. For each S-class listed in the commit message:
        - Execute the smoke script
        - Capture stdout + assert exit code 0
        - Paste 3-line summary into status report
  [ ] 4. scripts/smoke/Smoke-LogSilence.ps1 against the changed endpoint
        → No 42703 / 22P02 / NpgsqlException in last 60s
  [ ] 5. For S5/S6 (migration / module-context): scripts/smoke/Smoke-Probe.ps1
        → Assert table or schema in expected post-migration shape
  [ ] 6. Status report includes: (a) deploy URL, (b) smoke output,
        (c) log-silence assertion, (d) probe output if applicable
```

### A.5 — Forcing Functions

1. **`scripts/hooks/pre-push.ps1`** — rejects pushes whose commit messages lack `T-triggers:` or `S-class:` lines (unless subject starts with `docs:`, `chore:`, `revert:`, or `Merge`/`Revert`).
2. **GitHub Actions PR-validation gate** — grep step that fails the PR check when any commit touching `src/` lacks T/S annotations.
3. **Test-debt budget** (§D.1 below) — pre-push hook hard-blocks if budget breached.

---

## SECTION B — Retroactive Gap-Fill (Waves 0-4)

Risk-ranked, not Wave-ordered.

| Order | Gap | Risk | Time | Cumulative |
|---|---|---|---|---|
| **G0** | Build 4 smoke scripts in `scripts/smoke/` (foundation) | — | 90m | 1.5h |
| **G1** | IAuditable mutator round-trip + Ignore-correctness (70 entities) | A 🔴 | 270m | 6.0h |
| **G6** | Probe `notifications/media/forms` operational tables exist on staging | B 🟡 | 20m | 6.3h |
| **G2** | W4.2 Media write-path round-trip | A 🔴 | 90m | 7.8h |
| **G3** | W4.3 Forms write-path round-trip | A 🔴 | 90m | 9.3h |
| **G4** | W4.0b Notifications write-path round-trip | B 🟡 | 60m | 10.3h |
| **G5** | W4.7 `ICulturalCalendar` DI resolution | B 🟡 | 30m | 10.8h |
| **G7** | Wave 2 cultural type API soak | C 🟢 | 45m | 11.5h |
| **G8** | Wave 1 BB/SK surface tests | C 🟢 | 30m | 12.0h |
| **G10** | Wave 0 docs only | n/a | 0 | 12.0h |

**G0 + G1 + G6 (~6.3h) closes the bulk of the risk.**

---

## SECTION C — Wave 4.9 Phase Template

Every remaining phase (Wave 4.9 Phase 1.1-1.10, Phase 2 Media/Forms, Phase 3 redo) follows the same 5-section structure. Phase 1.1 (Identity group) is the canonical worked example.

### Phase 1.1 — Identity group: IAuditable AddColumn for 8 identity-schema tables

**Tables**: `identity.users`, `user_profiles`, `user_addresses`, `user_preferences`, `refresh_tokens`, `password_reset_tokens`, `email_verifications`, `user_roles`.

**Migration**: `Phase1_1_IdentityIAuditableColumns.cs` — 32 `AddColumn` (4 × 8) wrapped in `IF NOT EXISTS`.

#### 1.1.A — Unit Tests (mandatory per A.1 T4 + T6)

ADD in `tests/LankaConnect.Infrastructure.Tests/Configuration/Identity/`:
- `<Entity>EntityConfiguration_Maps_CreatedBy_To_Created_By_Column` × 8 entities
- `<Entity>EntityConfiguration_Maps_UpdatedBy_To_Updated_By_Column` × 8 entities
- `<Entity>EntityConfiguration_Does_Not_Ignore_CreatedBy_After_Phase1_1` × 8 entities
- **Total: 24 new unit tests.**

MODIFY `AppDbContext_OnModelCreating_Ignores_*` to exclude the 8 identity entities from the Ignore assertion list.

#### 1.1.B — Staging API Smoke Matrix (mandatory per A.2 S5)

```powershell
# Pre-deploy probe
pwsh scripts/smoke/Smoke-Probe.ps1 -Schema identity -Table users
# Expect: columns NO created_by/updated_by yet

# Post-deploy probe (after migration applied)
pwsh scripts/smoke/Smoke-Probe.ps1 -Schema identity -Table users
# Expect: columns include created_by, updated_by

# Functional mutator smoke
pwsh scripts/smoke/Invoke-Login.ps1
pwsh scripts/smoke/Smoke-Mutator.ps1 -Resource user -Mode UpdateLocation
pwsh scripts/smoke/Smoke-LogSilence.ps1 -Endpoint /api/users/me
```

**Cross-surface matrix** per `[[feedback-cross-surface-matrix-smoke]]`:

| Surface | Mode | Auth | Expected |
|---|---|---|---|
| Web | Update profile | authed | 200, `updated_by` set |
| API | Update profile | authed | 200, `updated_by` set |
| Web | Update profile | unauthed | 401 (no DB write) |
| Admin | Upgrade to organizer | admin | 200, `updated_by` = admin user id |

**Operator UAT gate** per `[[feedback-operator-uat-gate]]`: founder browses to `/profile`, edits location, saves, reloads, sees updated timestamp. Status flips to STAGING-VERIFIED only after this confirms.

#### 1.1.C — Pre-Deploy Checklist
1. `dotnet build` 0 errors
2. `dotnet test` 0 failed; +24 new tests passing
3. Migration has additive-only DDL; CI lint passes (no `SCHEMA-DESTRUCTIVE-APPROVED` needed)
4. `git diff` shows 8 entities removed from Ignore list
5. Commit message includes `T-triggers:` and `S-class:` lines

#### 1.1.D — Post-Deploy Verification
- Pre-deploy probe captured
- `__EFMigrationsHistory` shows `Phase1_1_IdentityIAuditableColumns`
- Post-deploy probe shows 4 new columns on all 8 tables
- Functional + cross-surface smoke pass
- Operator UAT confirmed
- Status report includes all of the above

#### 1.1.E — Rollback
Per `docs/operations/migration-rollback.md` Phase 0 work. `git revert` migration + `dotnet ef database update <prior>` on staging.

**Wall-clock estimate**: ~2h 15min (up from original "~45min"; testing was previously underweighted).

---

## SECTION D — Forcing Functions

### D.1 — Test-Debt Budget

**Definition**: an "untested commit" = commit on `develop` where T1-T8 fired but no matching test was added in the same commit per A.1.

**Budget**: max **2 untested commits** in any rolling 24-hour window per branch.

**Mechanic**: `scripts/hooks/pre-push.ps1` scans last 24h commits via `git log`. For each, greps for `T-triggers:` line. Missing or empty = "untested". If incoming push brings count to **>2**, hook BLOCKS push. Escape: `git push --no-verify` (logged to `docs/audit/test-debt-overrides.log`).

**Why 2 not 0**: zero blocks exploratory spikes + reverts. Two allows "spike, then test" rhythm but forces test-write within a working day.

### D.2 — Per-Wave MASTER_TODO Discipline

Every behavior-touching wave gets a `docs/MASTER_TODO_WAVE_<N>.md` with **4 mandatory checkboxes per Phase**:

```markdown
## Phase 1.1 — Identity IAuditable

- [ ] Migration written + applied + probe-verified
- [ ] Unit tests: T-triggers = T4, T6; tests added in commit X
- [ ] API smoke: S-class = S5; smokes executed = G6 probe + S2 mutator + S3 log-silence
- [ ] Operator UAT: founder confirmed in browser on YYYY-MM-DD HH:MM UTC
- [ ] STAGING-VERIFIED flip at: <UTC>
```

**Status flips to STAGING-VERIFIED only when ALL FOUR boxes are ticked with concrete evidence.**

### D.3 — Weekly Test-Debt Audit

`scripts/audit/Test-Debt-Report.ps1` runs Sunday EOD, scans last 7 days of commits across branches, lists any with missing T/S annotations, posts one-paragraph summary to founder.

### D.4 — Architect-Consult Trigger

Founder mandate point 3 codified: if a commit's T-triggers list any test class/method NOT in current wave's MASTER_TODO, **stop**, draft architect consult, wait for ruling before committing.

---

## Execution Order (per architect)

1. **NOW**: Append CLAUDE.md §13 (this discipline as law) — done in same commit as this doc
2. **NEXT**: G0 — build the 4 smoke scripts (`Invoke-Login`, `Smoke-Mutator`, `Smoke-LogSilence`, `Smoke-Probe`)
3. **THEN**: §A.5 forcing functions (pre-push hook + PR-validation T/S annotation gate)
4. **THEN**: G1 — IAuditable mutator round-trip across 70-entity surface (~6h work)
5. **THEN**: G6 → G2 → G3 → G4 → G5 → G7 → G8 (module write paths + lower-priority gaps)
6. **ONLY THEN**: Wave 4.9 Phase 1.1 under the §C template, gated by `docs/MASTER_TODO_WAVE_4_9.md` 4-checkbox per phase
