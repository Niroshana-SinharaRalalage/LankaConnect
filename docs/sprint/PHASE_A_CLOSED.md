# PHASE A CLOSED — LankaConnect Modular Monolith Refactor

**Closed:** 2026-07-15 (executed 4 calendar days early vs sprint bible plan of 2026-07-19)
**Head SHA at close:** `f3033074` on `develop`
**Tag:** `phase-a-close`

## Evidence set

| Gate | Sprint bible target | Delivered at close | Status |
|---|---|---|---|
| Bulk-move ~1,391 legacy files → 5-layer topology | Sprint scope | ✅ Days 2-6 executed | ✅ |
| Wave 6.5.f handler migration | ~120 handlers → `IMultiContextUnitOfWork` | ✅ Cycle-break shipped (Consult #17 LegacyPromotions); 3 critical handlers direct-SaveChanges (CreateEvent, RegisterUser, UpdateUserPreferredMetroAreas); 95 remaining routed → Wave 8.5.g | ⚠ ratified |
| Wave 6.5.g Payments un-skip | 11 handlers → integration events | ✅ landed Day 5 | ✅ |
| Wave 6.5.h Rule 5 legacy Infra un-skip | 14 services + 7 webhook handlers | ✅ landed Day 5 | ✅ |
| Wave 4.6 Identity.Contracts | 15-20 types | ✅ trivial cleanup complete | ✅ |
| Wave 9 API smoke restoration | 182/0/79 (69.73%) | **291 / 21 / 88 / 72.75%** (+109 pass, +3pp) | ✅ **+191 over Day 7 gate** |
| ArchTest | 57/0/0 | **49 / 0 / 9** (all skips Wave 8.5-tracked) | ✅ zero CI-blocking fails |
| Migration drift on staging | zero | **zero** — all 7 DbContexts up to date | ✅ |
| Frontend `web` build against refactored backend | build-green | ✅ `deploy-ui-staging.yml` run `29384577093` on `f3033074` SUCCESS | ✅ |
| Legacy csproj deletion | 5/5 gone + API rename | 2/5 gone (Domain, Shared) + 1/5 files relocated (MetroAreaMappingProfile) + Wave 8.5.a-refined/8.5.b/8.5.c carryover | ⚠ ratified per Consult #26 Q5 |
| Sprint bible §Stop Conditions | all 4 avoided | ✅ all 4 avoided | ✅ |

## Phase-A definition-of-done (Consult #27 Q4 canonical record)

**Phase A backend structural refactor is complete when:**
- Solution builds against LankaConnect.API entry point (0 errors)
- Wave 9 API smoke pass count ≥ pre-sprint baseline (182)
- ArchTest zero CI-blocking failures (skips permitted with Wave-tracked debt reference)
- Zero staging migration drift across all module DbContexts
- Frontend web workspace builds against the refactored backend API contract

**All 5 conditions MET at head `f3033074`.**

## Residual 21 Wave 9 fails — all routed to Phase A.5 Wave 8.5

| Cluster | Fail | Root cause | Debt item |
|---|---:|---|---|
| Events | 11 | `OwnsOne(TicketPrice).ToJson` MaterializeJsonEntity fails on legacy JSON shape drift (Consult #23 aftermath) | Wave 8.5.j |
| Money-flow (Sponsors/SP/Don/Coll/AddOns) | 6 | Cascade of Events ToJson trap | Wave 8.5.j |
| AdminUsers | 2 | Admin-specific policy check (5 of original 7 cleared with AutoMapper fix) | Wave 8.5.l diag |
| PhotoAlbums | 1 | Wave 8.5.f dispatch gap (same family as RSVP) — event stays status=0 after publish returns 200 | Wave 8.5.f (subsumes 8.5.l for this row) |
| Businesses | 1 | Controller physically absent (aggregate deleted per Consult #12 Option D — LankaBusiness surfaces in Phase B) | Wave 8.5.k product decision |

## Consult trail (17 total during sprint window; Wave 6.5+ era)

Consult #7 (Delta multi-DbContext) → #9 (2-week bulk-move plan approval) → #10-19 (Wave 6.5.f rulings + AppDbContext ownership boundary) → #20 (AppDbContext Ignore sweep) → #21-24 (Day 7 fix-forward priorities) → **Consult #25 (Day 7 attack order + UoW disposition + direct-SaveChanges blanket)** → **Consult #26 (Day 10 scope freeze + Application relocation)** → **Consult #27 (Phase A close-out + Phase B readiness ratification)**.

## Phase B readiness ruling (Consult #27 Q5)

**NUANCED green-light.**

Immediately unblocked for Phase B:
- Module csproj scaffolding: LankaTemples / LankaBusiness / LankaHomes / LankaMart / LankaSeyla / LankaNivasa Domain + Application + Infrastructure shells
- Module-DbContext skeletons with empty `OnModelCreating`
- ArchTest rule additions for the new modules
- Contract-first API stubs (empty controllers returning 501)

Blocked until named Wave 8.5 items land:
- Any Phase-B handler using multi-context UoW → gated on **Wave 8.5.f** (per-module SaveChangesInterceptor) + **Wave 8.5.h** (MultiContextUoW shared connection)
- Any Phase-B handler with JSON-column value objects → gated on **Wave 8.5.j** (ToJson data-drift resolution + ADR standardization)
- Copy-paste patterning from LankaConnect.Infrastructure → gated on **Wave 8.5.b** (566-file dismantle)

Gate phrase for founder briefings: *"Phase B scaffolding starts immediately; the first cross-module write handler lands only after Wave 8.5.f + 8.5.h close."*

## Founder sign-off

- [ ] Niroshana (founder) — reviewed and accepts Phase A close on head `f3033074` with the 21 Wave 8.5-tracked residuals + 2/5 legacy csproj partial delivery. Ratifies Phase A.5 kickoff.
- Date: __________
- Notes: __________

## Next milestones

- **Phase A.5 kicks off 2026-07-20** — Wave 7 (Frontend Mirror ~4-6 wks) + Wave 8 (Prod Cutover ~5 wks) + Wave 8.5 (sprint-deferred debt catalog, ~2-3 wks). See [PHASE_A_5_PLAN.md](../PHASE_A_5_PLAN.md).
- **Phase B kicks off in parallel** for scaffolding-only work per Consult #27 Q5. Cross-module writes land after Wave 8.5.f + 8.5.h close.
