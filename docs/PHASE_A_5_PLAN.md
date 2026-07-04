# Phase A.5 Plan — Post-Sprint Continuation

**Status:** DRAFT — activated after 2026-07-19 (Phase A backend sprint completion)
**Author:** Claude / system-architect, 2026-07-04
**Founder ruling ratifying scope split (2026-07-04):**
> "GO. You have to stick to the plan. No deviation, no lagging..." (approval of Consult #9's Wave 7 + Wave 8 cut from the 2-week window)

## Purpose

Consult #9 (2026-07-04) proved that the 2-week bulk-move sprint is only mathematically feasible when Wave 7 (Frontend Mirror) and Wave 8 (Production Cutover) are removed from the window. Those two waves DO NOT disappear — they move to Phase A.5.

**On 2026-07-19, "Phase A complete" means backend structural refactor done.** Production still runs off the current pre-refactor branch. Frontend still points at Host.AllInOne serving the current API surface identically.

Phase A.5 exists to complete both waves in their proper scope after the sprint proves the backend refactor stable.

## Scope

### Wave 7 — Frontend Mirror (~180h, ~4-6 calendar weeks)

Turborepo workspace mirror of the backend modular structure. Independent frontend track — does NOT block Wave 8.

**Owner:** frontend team (Nirmal / whoever founder assigns). Claude assists as consulting engineer, not primary implementer.

**Slices:**
- 7.a Workspace scaffold (Turborepo + shared packages)
- 7.b Package migration by feature: events / marketplace / auth / admin / cultural / shared
- 7.c Build + config wiring
- 7.d UAT + frontend smoke

**Prerequisite:** Sprint Day 10 (Wed 2026-07-15) — Host.AllInOne serving stable API contract.

### Wave 8 — Production Cutover (~150h, ~5 calendar weeks)

Migrate production off the pre-refactor branch onto the modular-monolith branch. Blue-green cutover with rollback rehearsal.

**Owner:** DevOps + founder. Claude assists with runbook authoring.

**Slices:**
- 8.a Prod migration re-parenting audit (verify Wave 6.5.f migration history is clean vs prod)
- 8.b Blue-green environment provisioning
- 8.c Canary traffic split (10% → 50% → 100%)
- 8.d 24h prod soak + rollback rehearsal
- 8.e Legacy prod branch decommission

**Prerequisite:** Wave 7 complete OR founder ruling that frontend can continue against pre-Wave-7 API contract during cutover.

## Cadence

Phase A.5 is calendar-driven, not sprint-driven. Founder schedules Wave 7 kick-off after Sprint Day 14 (Sun 2026-07-19). Wave 8 kicks off after founder pre-cutover approval, typically 2 weeks post Wave 7 close.

## What Phase A.5 does NOT include

- Wave 4.9.1 retroactive testing gap-fill — **DELETED** in sprint MASTER_TODO surgery (Consult #9 L2)
- New feature work — Phase B territory
- Additional module extractions — Phase B territory

## Change Log

- 2026-07-04: Created as part of sprint Day 1 doc surgery (pulled forward to Day 0.5).
