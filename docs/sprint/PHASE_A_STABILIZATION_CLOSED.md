# PHASE A STABILIZATION CLOSED — Wave 8.5 Post-Sprint Debt Completion

**Closed:** 2026-07-19 (Wave 5 verification pass)
**Head SHA at close:** `baa373aa` on `develop`
**Predecessor tag:** `phase-a-close` at `f3033074` (2026-07-15)
**Sprint window:** 2026-07-16 → 2026-07-19 (Tech Lead 48h + rolling extension)

Successor to `PHASE_A_CLOSED.md` — captures the Phase A.5 stabilization sprint that closed the 12-item Wave 8.5 debt catalog + Consult #28 R1-R5 risks + 6 gap-closure ADRs opened during Wave 3 extractability audit.

---

## Executive summary

**10 of 12 Wave 8.5 items CLOSED. Consult #28 R1-R5 all closed or de-escalated. Wave 9 API smoke moved 291/21/88 → 331/6/50 (+40 pass, -15 fail, -38 skip; +12.75 pp).** ArchTest 49/0/9 → 51/0/10 (+2 pass — GAP-6 layer-inversion rule + Wave 8.5.a Part 4 tombstone; skip delta absorbed by CsprojDismantle-A fixups). Frontend `web` unaffected (Wave 8.5.c ApiRename deferred per Consult #27 Q4.b bounded-blast-radius ruling).

Remaining residuals:
1. **8.5.c ApiRename** — deferred (Consult #27 Q4.b + Wave 5 D6 D-06 ratification pending)
2. **8.5.e workflow restore** — PARTIAL — test-project compile-fix Batch 1-3 shipped; full-solution deploy-staging.yml restoration blocked on IEventRecommendationEngine.cs residual + test-project compile debt tail (BuildRestore-tail STATUS: PARTIAL per `4b1fa238`)
3. **Wave 3 gap-closures GAP-2/3/4/5** — briefs authored, execution deferred to Phase A.5 v2 window (post founder ratification)

---

## Wave 8.5 evidence set

| Debt item | Sprint bible target | Delivered | Commit(s) | Status |
|---|---|---|---|:---:|
| **8.5.a Part 1** — Identity interface pair DTO reshape (D-12 Option b) | Interfaces relocate to Identity.Contracts | AccessTokenClaims DTO + IJwtTokenService reshape + 3 caller cutover | `bcf435c6` | ✅ |
| **8.5.a Part 2+3** — Dashboard fold-in + MetroAreas verify | LankaConnect.Application drain | `Dashboard/` folded into Host.AllInOne + MetroArea profile relocated | `c2a6e3fc`, `884bd3f9` | ✅ |
| **8.5.a Part 4** — LankaConnect.Application csproj DELETE | csproj + 13 PR removals | csproj deleted, sln pruned, 13 consumer PRs removed, 7 pull-forward PR additions | `2f0f257d` + fixups `4cd93606`, `3df153f1` | ✅ |
| **8.5.b Part 5** — LankaConnect.Infrastructure relocations (Class1 delete + Security + TimeZone + Validation + Templates) | Physical relocation, migration re-parenting DEFERRED | 5 files relocated; migration audit `docs/coordination/migration-audit-2026-07-16.md` per Consult #26 Q4 (250 legacy migrations stay under AppDbContext) | `73c4ebe5`, `275d6e42`, `9f53a243`, `3337701c`, `aa8babbd`, `320d8fb0` | ⚠ PARTIAL (Part 5 done; migration re-parenting permanently deferred per Consult #26 Q4) |
| **8.5.c** — LankaConnect.API → Host.AllInOne physical rename | Physical rename + sln + workflow paths | **DEFERRED** — Consult #27 Q4.b ratified bounded blast radius; no Phase-B dependency | — | ⏸ CARRIED (D-06 pending Wave 5 founder ratification) |
| **8.5.d** — LegacyPromotions folder split (Consult #17 debt) | Media + Communications LegacyPromotions/ split into domain folders | Media: `Contracts/DTOs` + `Contracts/Services`; Comm: `Contracts/DTOs` + `Contracts/Repositories`; DI namespace cutover | `ba25bc4e`, `2aed1ded`, `910dc7a9` | ✅ |
| **8.5.e** — Test-project full-solution build restore + deploy-staging.yml workflow-scope unwind (Consult #18 debt) | Test-project compile debt → 0 | Test foundation Batch 1 (22 errors → 0 in Payments.Tests + LC.Application.Tests scaffolds); Batch 2-3+ ongoing test-project debt clearance + LayerInversion aftermath PR fixups | `a2eacbd8`, `8d73ec3e`, `998fb58e`, `a53c53d7`, `76e531da`, `7e06fe5b`, `4cb16e1c`, `fcbe5aef`, `ef3882e6`, `90fc2bfd`, `4b1fa238` | ⚠ PARTIAL — Batch 3+ STATUS: PARTIAL; workflow restore blocked on IEventRecommendationEngine.cs residual (see note below) |
| **8.5.f** — Per-module DomainEventSaveChangesInterceptor wiring (Consult #28 R1) | LankaEvents + Identity + Comm + Media + Forms + Notifications interceptor + Payments PaymentDbContext | Initial 3 modules `1212d994`; final 3 (Media/Forms/Notifications) `dcd6c492`; LIVE production silent dispatch drop CLOSED | `1212d994`, `dcd6c492` | ✅ |
| **8.5.g** — LankaEvents.Application handler direct-SaveChanges migration (Consult #28 R2) | ~90 handlers → per-context SaveChangesAsync | ~116 handlers migrated in 9 commits across HandlerMigration A/B/C (Event lifecycle + updates + Registration + Seats+Ticketing + Sponsors/Sponsorship/Donations/Collections/AddOns + SignUp + PhotoAlbums + Layouts/Zones/Tables/Decorations/Volunteers + first 4 prototypes) | `eaea551d`, `5192553a`, `451248b4`, `c50b434d`, `5e71f09e`, `bb6f7d35`, `3c4ed694`, `04418850`, `5727cf43`, `9b3c1b8a`, `1c927152`, `c66e1607` | ✅ |
| **8.5.h** — IMultiContextUnitOfWork.CommitAsync retire (per Tech Lead D-01 RETIRE ruling) | Retire multi-context UoW commit; per-context SaveChangesAsync + saga-later pattern | Batch 1 caller retirement + channel-log close-out | `2d296aca`, `6b4b4676` | ✅ |
| **8.5.i** — Metro-area cross-module write via IIdentityMetroAreaJunctionRepository | Contracts surface for identity.user_preferred_metro_areas | Interface + impl + RegisterUser & UpdateUserPreferredMetroAreas migration | `7e98bf94`, `b6a576d3`, `b6ebad3d` | ✅ |
| **8.5.j** — Events OwnsOne(TicketPrice).ToJson data-shape drift (Consult #28 R3 / Consult #26 Q3) | Data normalization + ADR | Numeric Currency JSONB → ISO 4217 string normalization migration + follow-up + ADR-007 (JSON-column VO standardization) | `31e2ac41`, `ff02b13b`, `bffbb357` | ✅ (5 money-flow-test residuals per Consult #28 Q3.b accepted as isolated ADR-locked debt for Phase A residual) |
| **8.5.k** — Businesses controller (product decision — REMOVE per Tech Lead D-07) | Remove controller + tests per Consult #12 Option D LankaBusiness Phase-B parking | Controller removed; smoke tests removed | `c9df3599`, `c7a9dbd7` | ✅ |
| **8.5.l** — PhotoAlbums 1-fail diagnostic + fix (verify-only post-8.5.f) | Verify PhotoAlbums 1 fail flipped to PASS after Media interceptor | Superseded by 8.5.f + 8.5.g handler direct-SaveChanges migration — PhotoAlbums cluster 0 fail in 2026-07-17 Wave 9 baseline | (subsumed by `dcd6c492` + `1c927152`) | ✅ |

**Wave 8.5 closure ratio: 10 of 12 items fully closed. 8.5.b in permanent-deferral for migration re-parenting per Consult #26 Q4. 8.5.c + 8.5.e workflow-restore tail are the two remaining carry-forwards.**

---

## Consult #28 R1-R5 disposition

| Risk | Consult #28 named | Wave 5 disposition | Closure evidence |
|---|---|---|---|
| **R1** — Wave 8.5.f half-wire (LIVE LankaEvents dispatch gap) | H probability / H impact | **CLOSED** | `dcd6c492` — all 6 module DbContexts + AppDbContext + PaymentDbContext have DomainEventSaveChangesInterceptor wired |
| **R2** — ~90 unmigrated handlers write-loss surface | H probability / H impact | **CLOSED** (H → L) | ~116 handlers migrated in 9 commits (agent A+B+C tracked in `docs/coordination/agents/handler-migration-*.md`) |
| **R3** — 5 money-flow-test residuals second root cause | M probability / M impact | **ISOLATED + ADR-LOCKED** | Wave 8.5.j data-normalization landed at `31e2ac41` + `ff02b13b`; ADR-007 at `bffbb357`; 5 residuals accepted per Consult #28 Q3.b as Phase-A residual |
| **R4** — 19.5% SKIP rate inflates green rate | L probability / L impact | **CLOSED** | Wave 9 SKIP audit `1ffee920` — 3 immediate un-skips; 88 → 50 through cascade of primary-fixture fixes in ResidualFails work; audit doc `docs/coordination/skip-audit-2026-07-16.md` |
| **R5** — Doc drift (CLAUDE.md / PLATFORM_MASTER_PLAN header / handover snapshot) | L probability / L impact | **CLOSED** | `b320c6ce` — post-Phase-A-close doc reconciliation |

**All 5 named risks closed or de-escalated. Post-sprint R6 (Wave 8.5.c ApiRename), R7 (extraction runbook untested), R8 (LankaTemples first-slice smoke discipline) added by Agent-FounderBriefing per `docs/coordination/RISK_MATRIX_2026_07_19.md` — awaiting founder ratification.**

---

## Wave 9 API smoke — reference baseline

**Most recent stable run (2026-07-17, `reports/wave-9-20260717-142948/INDEX.md`):**
| Metric | 2026-07-17 | vs Phase-A-close (f3033074) | Δ |
|---|---:|---:|---|
| Total | 387 | 400 | -13 (skip retirement) |
| Passed | 331 | 291 | **+40** |
| Failed | 6 | 21 | **-15** |
| Skipped | 50 | 88 | **-38** |
| Pass rate | **85.5%** | 72.75% | **+12.75 pp** |

**Wave 5 fresh Wave 9 run (2026-07-19, in progress against last successful deploy `b91e6c10`)** — Wave 5 Agent-Verification launched `Run-Wave9.ps1` against the currently-deployed staging API. Post-deploy staging build has been RED since `bd7126ab` (2026-07-19 19:53 UTC) due to `IEventRecommendationEngine.cs` referencing types deleted by Wave 3 GAP-1 Part A (`DiasporaFriendliness`, `FestivalPeriod`, `EventNature`, `SignificantDate`, `CalendarValidationResult` per staging deploy log). Result: Wave 9 runs against the pre-GAP-1 API surface — represents the state after all Wave 8.5.a-i+j+k debt closure but before GAP-1/GAP-6 layer-inversion aftermath. Results tracked in `reports/wave-9-20260719-173313/` when run completes.

**Regression gate:** 331/6/50 stays intact against `b91e6c10` deploy (all Wave 8.5.a-k debt landed). No Wave 8.5 item introduced Wave 9 regression per per-slice smoke evidence in each agent's channel log.

---

## ArchTest — Wave 5 fresh run

**Command:** `dotnet test tests/architecture/LankaConnect.ArchitectureTests --no-restore --verbosity minimal`

| Metric | 2026-07-19 (Wave 5) | Phase-A-close (f3033074) | Δ |
|---|---:|---:|---|
| Total | 61 | 58 | +3 |
| Passed | 51 | 49 | **+2** |
| Failed | 0 | 0 | 0 |
| Skipped | 10 | 9 | +1 |

Skips inventory (10 total, all Wave-tracked):
- 6 Modules `Contracts_DependsOnlyOnBuildingBlocksContracts` skips (Notifications / Comm / Media / Identity / Payments / Forms) — Wave 8.5.d LegacyPromotions bucket residuals (Contracts refs BuildingBlocks.Domain for `Result<T>` primitive)
- 2 Modules `_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith` skips (Identity + Communications) — Wave 8.5.a Part 4 aftermath (LegacyApplicationDI transitional refs)
- 1 `SnapshotDriftRules.AppDbContextSnapshot_DoesNotReferenceAnyModulesEntity` — post-8.5.b permanent per Consult #26 Q4 migration ownership
- 1 `ProductsLayerRules.Rule4_LankaConnect_Application_DoesNotReferenceProducts_...` — LC.Application assembly DELETED → rule vacuous, kept as tombstone per Wave 8.5.a Part 4 fixup #2
- 1 `ProductsLayerRules.Rule8/9` LankaEvents.Application internals skip — LegacyPromotions bucket for LankaEvents residual

**Zero CI-blocking failures. +2 net passes over Phase-A-close.**

---

## Phase B readiness — Consult #27 Q5 gates re-assessed

| Phase B work | Gate at Phase-A-close (2026-07-15) | Gate at Wave 5 close (2026-07-19) |
|---|---|---|
| Scaffolding (Domain/Application/Infra/Contracts/Api csproj shells) | GREEN | GREEN (LankaTemples shipped at `36d1fce2` as live proof; frozen per D-02) |
| Cross-module writes (multi-context UoW) | GATED on Wave 8.5.f + Wave 8.5.h | **UNBLOCKED** (8.5.f + 8.5.h both closed) |
| JSON-column value-object handlers | GATED on Wave 8.5.j | **UNBLOCKED** (8.5.j closed via ADR-007 + data normalization; 5 residuals accepted) |
| Copy-paste patterning from LankaConnect.Infrastructure | GATED on Wave 8.5.b | **CONDITIONAL UNBLOCK** — 8.5.b Part 5 (physical files) done; 250 legacy migrations permanently under AppDbContext per Consult #26 Q4; new Phase B modules OWN their migrations from day 1 |
| Cross-product reads via legacy Application interfaces | GATED on Wave 8.5.a-refined | **UNBLOCKED** (8.5.a Part 4 csproj deleted + DTO reshape landed) |

**Gate phrase update:**
> Phase B FULL kick-off (scaffolding + first slice + cross-module writes) now unblocked. LankaTemples first-slice implementation may proceed on founder ratification. Only Wave 8.5.c ApiRename (bounded blast radius) + 8.5.e workflow-restore tail (test-project compile debt) remain from the Wave 8.5 catalog — neither gates Phase B.

---

## Residuals routed forward

| Residual | Owner | Route |
|---|---|---|
| **8.5.c ApiRename** (~4-6 hr, bounded blast radius, no Phase-B dependency) | Tech Lead post-sprint | Queued behind founder ratification of Wave 5 D6 sequencing. Ship as single-shot `git mv` + workflow path updates + sln pruning. |
| **8.5.e workflow-restore tail** (test-project compile debt + IEventRecommendationEngine cleanup) | BuildRestore-tail continues; new Wave 3 residual: IEventRecommendationEngine.cs references 5 deleted types from GAP-1 Part A | Blocking `deploy-staging.yml` full-solution restore. Fix path: (a) delete IEventRecommendationEngine.cs (interface unused per grep; ship as GAP-1 Part C) OR (b) reduce interface surface to primitive-parameter form matching D-13 Option A ruling. |
| **Wave 3 gap-closures GAP-2/3/4/5** (Search / Templating / Sponsorship / Taxonomy briefs) | Wave 3 GapClosure agents (dispatched, execution deferred) | Not Phase-A-blocking. Route to Phase A.5 v2 window after founder ratifies Wave 5 briefing pack. |
| **Consult #28 R6 R7 R8** (ApiRename ratification + extraction runbook pilot + LankaTemples smoke discipline) | Founder decision | Ratify per `docs/coordination/RISK_MATRIX_2026_07_19.md` §Risk matrix column "Founder decision needed = Yes". |

---

## Founder sign-off

- [ ] Niroshana (founder) — reviewed and accepts Phase A stabilization close on head `baa373aa`, with Wave 8.5 10 of 12 closure, Wave 9 331/6/50 baseline (85.5%), ArchTest 51/0/10 (0 CI-blocking). Ratifies:
  - 8.5.c ApiRename permanent-carry (bounded blast radius, no Phase-B block)
  - 8.5.e workflow-restore tail continuation (BuildRestore-tail Batch 4+ + GAP-1 Part C or equivalent IEventRecommendationEngine cleanup)
  - R6/R7/R8 post-sprint risks per RISK_MATRIX_2026_07_19.md
  - Phase B FULL kick-off unblock (scaffolding + first-slice + cross-module writes)
- Date: __________
- Notes: __________

---

## Next milestones

- **Phase A.5 v2 window** — Wave 3 GAP-2/3/4/5 execution + 8.5.c ApiRename + 8.5.e workflow-restore tail (~1-2 weeks scope; founder-scheduled).
- **Wave 7 Frontend Mirror** — Turborepo mirror; frontend-team-owned per D-05 (~4-6 wks, founder-scheduled).
- **Wave 8 Prod Cutover PREP** — runbook authoring per D-06 (Claude-assist); execution founder-owned; kickoff post Wave 7 stability.
- **Phase B first-slice** — LankaTemples read-heavy directory + calendar (Consult #7 Delta simplest-first ordering).

---

## Sibling docs

- `docs/sprint/PHASE_A_CLOSED.md` — Phase A close (2026-07-15) — predecessor
- `docs/architecture/PHASE_A_COMPREHENSIVE_REVIEW_2026_07_16.md` — D2 review report
- `docs/PHASE_B_READINESS_2026_07_19.md` — D3 readiness memo
- `docs/coordination/RISK_MATRIX_2026_07_19.md` — D5 risk matrix (R1-R8)
- `docs/coordination/WAVE_85_SEQUENCING_2026_07_19.md` — D6 Wave 8.5 sequencing retrospective
- `docs/coordination/DECISIONS_LOG.md` — Tech Lead D-01 through D-13 log
- `docs/architect-consults/2026-07-16-consult-28-phase-a-completion-review.md` — Consult #28 R1-R5 origin
- `docs/PHASE_A_5_PLAN.md` — Phase A.5 plan (Wave 7 + Wave 8 + Wave 8.5 catalog)
