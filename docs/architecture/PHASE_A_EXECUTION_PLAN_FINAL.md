# Phase A — Adjusted Execution Plan (FINAL)

**Date**: 2026-06-08
**Status**: ENTERPRISE-LOCKED — no further plan changes after execution starts
**Owner**: Claude (enterprise architect for the LankaConnect project)
**Founder**: Niroshana (decision authority + UAT)

---

## North Star (UNCHANGED from Plan v5)

> **We are refactoring the LankaConnect codebase from a layered monolith into a modular monolith. The system must work AS IT IS during and after the refactor, AND must be ready to receive Phase B product modules (LankaSeyla, LankaMart, etc.) without re-architecting.**

Plan v5 (8-wave plan) ratified 2026-06-04. This document does NOT change the wave plan. It encodes the testing discipline founder mandated 2026-06-07 and folds it into every wave task.

---

## 10 Architect-Locked Pre-Execution Resolutions

The system-architect's stress-test found 10 hidden assumptions. All resolved here. **No further architect re-consults required to start execution.**

| # | Blocker | Architect resolution |
|---|---|---|
| **P1** | Fat template element count | **8 elements** — adds `Originating-amendment` slot for audit trail (architect §A.2) |
| **P2** | Cross-link inventory captured | Done — `docs/audit/master-todo-crosslinks.md`. Section headings preserved verbatim during surgery |
| **P3** | Route assumptions verified | Done — `docs/audit/route-inventory-2026-06-08.md`. Caught: PhotoAlbums is `/api/events/{id}/albums` NOT `/photo-albums`; UpdateLocation needs `{id:guid}` substitution |
| **P4** | Gap classification | G5 + G7 demoted to **unit-only** (no direct HTTP surface); G1.d + G2 + G3 + G4 + G8 stay API-smokeable |
| **P5** | G1.c provider strategy | **Npgsql model-builder-only** (no real DB connection) — 10× faster than Testcontainers, tests the actual production model |
| **P6** | G2 staging fixture event-id | Smoke **bootstraps its own** event via POST `/api/events`, then creates album against it; no external fixture |
| **D.1** | Phase 1.1 hotfix removal pattern | **Per-type exclusion list** via `_phase1AuditableExclusions` HashSet in AppDbContext; each Phase 1.N adds its 8-15 types |
| **D.2** | snake_case column-name ArchTest | Authored in Phase 1.1 same commit; red-then-green via Phase 1.1 ship; permanent guard after |
| **C.4** | Commit-body self-attestation | **DROPPED**. 4-layer existing-tests-must-pass mechanism becomes 3-layer (pre-push Tier A/B + CI full-suite + auto-file Issue on failure) |
| **E.3** | Calendar approach | **APPROACH 3** (parallel structural + testing tracks, ~35wk Phase A remaining) with automatic fallback to Approach 2 (sequential, 46wk) if testing-track falls 2 waves behind structural. No silent slip. |

---

## The Adjusted Execution Plan (steps in fixed order)

### Stage 0 — Master TODO Surgery (this commit + next 1-2 commits)

**Objective**: encode the testing discipline into every Wave 0-8 task in the master TODO doc without rewriting shipped tasks or breaking cross-links.

**Concrete steps**:

1. Add `## Quick Index (Auto-Maintained)` section near top of `docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` — flat list of every wave + task with status, ~150 lines, single-grep navigation
2. Add `## Fat-Task Template (Canonical)` section under the v5 amendment — 8-element template with field definitions (Intent / T-triggers / Unit tests / S-class smoke / Smoke-Mutator route additions / Pre-deploy + Post-deploy checkpoints / Existing-tests-must-pass invariant / Originating-amendment / Rollback)
3. Add `## When to Fat vs One-Liner` rule section — T1-T8 triggers → fat; C1-C4 counter-triggers → one-liner with `[counter-trigger: Cn]` annotation
4. Wave 0-3 SHIPPED tasks: **annotate in-place** with `[counter-trigger: Cn]` for one-liners + `**Retroactive gap-fill**: see G[N]` cross-link for fat-eligible ones. **Do not rewrite shipped task descriptions** (audit trail invariant).
5. Active waves (W4 partial through W4.9, in-flight phases): apply **full fat-template treatment** to each remaining task per the §3 inventory.
6. Future waves (W5, W6, W6.5, W7, W8): apply **stub fat-template treatment** — Intent + T-triggers + S-class + existing-tests-must-pass lines only; details filled in as each wave activates (premature file-path commitment is itself an anti-pattern per architect §A.4).
7. Add `## Calendar Impact (Approach 3 selected)` section — names Approach 3 with the 1-wave testing-lag tolerance + 2-wave auto-fallback trigger + weekly UAT batch convention
8. Freeze historical sections with `<!-- HISTORICAL-FROZEN: 2026-06-08 -->` HTML comment

**Acceptance**:
- `dotnet build` 0 errors (no code touched)
- Anchor preservation set (per P2 audit) — every section heading byte-identical
- Quick Index covers all 8 waves + G0-G8 retro track

**Estimated wall-clock**: 90 min (single doc rewrite, no execution risk)

### Stage 1 — Test-discipline mechanism (3-layer, in parallel with G-sequence)

**Objective**: make existing-tests-must-pass enforceable, not aspirational.

**Concrete steps** (can ship in any order; each is its own commit):

1. **Layer 1 — Tier A/B pre-push test run**: extend `scripts/hooks/pre-push.ps1` with project-scoped `dotnet test` (Tier A ≤30s for intra-module changes; Tier B full local run for core-touching commits). Tier classification by changed-paths.
2. **Layer 2 — CI full-suite gate**: add `full-test-suite-must-be-green` job to `.github/workflows/pr-validation.yml`. Runs on every push to develop + every PR to main. `concurrency.cancel-in-progress: true` so latest push wins. Parallel to deploy-staging, not blocking it.
3. **Layer 3 — Auto-file GH Issue on test failure**: develop-push test job that auto-files an Issue with labels `auto-filed`, `develop`, `test-failure` on red; 24h dedup window. Surfaces breaks to founder via existing email notifications without Slack.

**Acceptance**:
- A deliberately-broken commit on a throwaway branch fails pre-push Tier A in <30s
- A test-suite-red push to develop opens an auto-filed issue within 5 min
- A test-suite-red PR to main blocks merge

**Estimated wall-clock**: 90 min total across 3 commits

### Stage 2 — G-sequence retroactive gap-fill (G0 done; G1.c → G8)

**Objective**: close the testing coverage debt accumulated across Wave 0-4 BEFORE adding more structural changes on top.

**Order** (architect-prescribed):

1. ~~**G0** — Build 4 smoke scripts (foundation)~~ ✅ SHIPPED `0ebcd0a4`
2. ~~**G1.a** — LegacyBaseEntity ctor tests (6 tests)~~ ✅ SHIPPED `b815905d`
3. ~~**G1.b** — Per-aggregate audit-field round-trip tests (5 aggregates)~~ ✅ SHIPPED `7f43b74b`
4. **G1.c** — Infrastructure tests for IAuditable Ignore coverage via Npgsql model-builder-only (P5). ~60 min.
5. **G1.d** — Staging mutator smokes per the route inventory (P3). Adds 4 routes to Smoke-Mutator RouteMap. ~90 min.
6. **G6** — Probe `notifications/media/forms` operational schemas exist on staging via `Smoke-Probe.ps1`. ~20 min.
7. **G2** — W4.2 Media write-path round-trip; smoke bootstraps its own event (P6) then creates album. ~90 min.
8. **G3** — W4.3 Forms write-path round-trip (route TBD via G3-prep audit). ~90 min.
9. **G4** — W4.0b Notifications write-path round-trip via mark-read trigger. ~60 min.
10. **G5** — W4.7 ICulturalCalendar DI resolution; unit-only (P4 classification). ~30 min.
11. **G7** — Wave 2 cultural type unit tests (one per moved type); unit-only (P4 classification). ~45 min.
12. **G8** — Wave 1 BB/SK surface tests — Event detail price-shape assertion + cross-module reference compile-check. ~30 min.

**Total remaining G-sequence wall-clock**: ~9 hours spread over 2 working days.

**Acceptance per gap**:
- Unit test(s) green via `dotnet test`
- API smoke (if applicable) green via `scripts/smoke/*.ps1`
- Log-silence assertion green
- Each commit follows the §13 commit-message annotation convention (`T-triggers: ...`, `S-class: ...`)

### Stage 3 — Wave 4.9 Phase 1.1 (Identity group IAuditable AddColumn)

**Objective**: ship the first per-schema IAuditable column-addition migration under the per-phase 5-section template.

**Concrete steps** (per architect D.1 + D.2 resolutions):

1. Identify the 8 Identity-schema entities (`User`, `UserProfile`, `UserAddress`, `UserPreference`, `RefreshToken`, `PasswordResetToken`, `EmailVerification`, `UserRole`).
2. Add the 8 types to `_phase1AuditableExclusions` HashSet in AppDbContext (`IgnoreAuditByActorPropertiesUntilPhase1` continues to ignore the other ~50 IAuditable entities; only the 8 Identity types are exempted from the ignore).
3. Add per-entity EF configuration for each of the 8: `HasColumnName("created_by")` + `HasColumnName("updated_by")` + `HasMaxLength(450)`.
4. Author snake_case column-name ArchTest in `tests/architecture/LankaConnect.ArchitectureTests/` — asserts every IAuditable entity has `^[a-z_]+$` column names for `CreatedBy`/`UpdatedBy`. Red until step 3 lands; permanent guard after.
5. `dotnet ef migrations add Phase1_1_IdentityIAuditableColumns --context AppDbContext` — generates the 32 AddColumn statements (4 columns × 8 tables) + the `_phase1AuditableExclusions` snapshot delta.
6. Wrap any pre-existing column entries (e.g., Sponsor.CreatedBy from Phase 6A.151) in `IF NOT EXISTS` raw-SQL blocks.
7. Ship 1-3 as a SINGLE commit (atomic per architect D.3).
8. Post-deploy: probe `\d identity.users` confirms 4 new columns; smoke User UpdateLocation; assert log silence.
9. Operator UAT — founder browses to `/profile`, edits location, saves, reloads, sees updated timestamp.
10. STAGING-VERIFIED flip in MASTER_TODO_WAVE_4_9.md (per §13.6 4-checkbox gate).

**Acceptance per the per-phase template (§C 5 sections)**: see `docs/architecture/TESTING_DISCIPLINE_RULING.md §C` for the full template.

**Estimated wall-clock**: ~2h 15min (per architect estimate for Phase 1.x).

### Stage 4 — Wave 4.9 Phases 1.2 through 1.10 + Phase 2 + Phase 3 redo

Each phase follows the **same 5-section template** as Phase 1.1. Architect's spec is in `docs/architecture/TESTING_DISCIPLINE_RULING.md §C`. Wall-clock ~2h per phase. Total ~20h spread across 5-7 working days.

### Stage 5 — Wave 5 (Products carve-out)

Per Plan v5: `Products/LankaEvents` csproj + EventPass + EventSignUp pivot. Two fat tasks:
- **W5.1** — Products/LankaEvents csproj + initial type moves (B 🟡)
- **W5.2** — EventPass + EventSignUp pivot into Products/LankaEvents (A 🔴)

Plus bookkeeping (C 🟢 one-liners).

### Stage 6 — Wave 6 (ArchTest hardening to 28 rules)

~12 ArchTest additions, each its own fat task (B 🟡). Each rule firs T6.

### Stage 7 — Wave 6.5 (NEW: Outbox cutover)

Retire the self-saving repository pattern. 4 fat tasks (one per affected repo):
- NotificationRepository → IUnitOfWork-coordinated
- PhotoAlbumRepository → same
- EventFormRepository → same
- FormResponseRepository → same

All A 🔴. T3 + T4 + T6 fire. S6 smoke + log-silence + existing-tests-must-pass non-negotiable.

### Stage 8 — Wave 7 (Frontend mirror, Turborepo)

Module package extraction + Money DTO migration + (some) mega-page split work. Per Plan v5 — 3 weeks nominal, ~6.5 weeks discipline-adjusted.

### Stage 9 — Wave 8 (Production cutover + stabilization)

Per-module flag flip, canary, full production smoke matrix. Per Plan v5 — 4 weeks nominal, ~5 weeks discipline-adjusted (the cutover discipline is already high).

---

## Calendar Impact (Approach 3 selected)

| Wave | Plan v5 nominal | Discipline-adjusted (Approach 3 parallel) |
|---|---|---|
| W0 | 1 wk | ✅ shipped |
| W1 | 2 wk | ✅ shipped |
| W2 | 2 wk | ✅ shipped |
| W3 | 2 wk | 🟡 in flight as part of W4 capability extractions |
| W4 | 5 wk | 🟡 partial — W4.0/W4.0b/W4.2/W4.3/W4.5/W4.7 shipped; W4.1/W4.4/W4.6 split into A/B remaining |
| **G0-G8 retroactive** | n/a (NEW) | ~9 hrs remaining |
| W4.9 | (NEW, schema realignment) | ~7 wk |
| W5 | 1 wk | ~2 wk |
| W6 | 1 wk | ~1.5 wk |
| **W6.5** outbox (NEW) | n/a | ~3 wk |
| W7 | 3 wk | ~6 wk |
| W8 | 4 wk | ~5 wk |
| **Total Phase A remaining** | **~21 wk (Plan v5 nominal)** | **~35 wk (Approach 3)** |

**Approach 3 fallback trigger**: if at any point the testing-track falls 2 full waves behind the structural-track, the pre-push hook hard-blocks structural pushes until testing catches up, AND I open an architect re-consult to switch to Approach 2 (46wk sequential).

---

## What I Will NOT Do (commitments to founder)

1. **No further architect re-consults** unless a new genuinely-unforeseen blocker surfaces during execution. The architect has answered every pre-execution question; mid-execution discoveries that don't match this plan must be re-baselined explicitly, not absorbed silently.
2. **No mid-execution deviations from the wave plan**. Plan v5 ratified by founder 2026-06-04 stands. Any scope cut requires explicit founder ratification, not unilateral developer judgment.
3. **No skipping of unit tests or smoke**. The §13 forcing functions (`pre-push.ps1`, `test-discipline-gate` CI job, `full-test-suite-must-be-green` CI job) make this mechanically enforced. Bypass via `git push --no-verify` is logged to `docs/audit/test-debt-overrides.log` and is founder-only convention.
4. **No fat-template surgery on shipped commits' messages**. Audit trail is sacred; retroactive fat is metadata-only annotation in the master TODO doc.
5. **No silent calendar slip**. Approach 3 selected with explicit fallback. If reality deviates >25% from Approach 3 estimate at quarter-end, surface it.

---

## What I WILL Do (commitments to architect)

1. Execute Stage 0 → Stage 9 in order. No reordering without architect approval.
2. Every commit touching `src/` or `tests/` carries `T-triggers:` and `S-class:` annotations. Pre-push hook enforces.
3. Every gap (G1.c → G8) ships with its acceptance evidence captured in commit body + paste into PROGRESS_TRACKER.
4. Every Wave 4.9 phase ships under the per-phase 5-section template with 4-checkbox gate in `MASTER_TODO_WAVE_4_9.md`.
5. Calendar audit at quarter boundaries (week 12, week 24, week 36 of remaining Phase A). Variance >25% triggers explicit re-baseline.

---

## Founder Decisions Already Locked

- ✅ No local Docker, no canary, no second DB (2026-06-07)
- ✅ Trunk-based dev (direct push to develop; PRs only for develop→main prod merges) (2026-05-11)
- ✅ Testing discipline as law (2026-06-07)
- ✅ Approach 3 calendar (this doc — architect decision per founder delegation 2026-06-08)
- ✅ G0-G8 retroactive gap-fill before Wave 4.9 Phase 1.x resumes (2026-06-08)

---

## Acceptance for THIS Plan Doc

This plan is "execution-ready" when:
- [x] All 10 P-blockers resolved with explicit decisions (Section above)
- [x] Cross-link audit captured (`docs/audit/master-todo-crosslinks.md`)
- [x] Route inventory audited + corrections recorded (`docs/audit/route-inventory-2026-06-08.md`)
- [ ] Master TODO surgery applied (Stage 0 — next commit)
- [ ] Founder review (next status report)

Once master TODO surgery commits, execution begins straight through Stages 1-9 without further plan-changes.

---

**Relevant files**:
- `docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` — surgery target
- `docs/architecture/TESTING_DISCIPLINE_RULING.md` — discipline spec (Section A-D)
- `docs/architecture/WAVE_4_9_PLAN.md` — Wave 4.9 per-phase template (Section C)
- `docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` — Plan v5 8-wave plan
- `docs/audit/master-todo-crosslinks.md` — P2 audit
- `docs/audit/route-inventory-2026-06-08.md` — P3 audit
- `CLAUDE.md` §13 — discipline encoded as law
- `scripts/smoke/` — 4-script smoke harness
- `scripts/hooks/pre-push.ps1` — forcing function
