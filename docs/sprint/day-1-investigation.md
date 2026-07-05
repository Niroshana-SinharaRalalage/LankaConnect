# Sprint Day 1 Investigation: 54 Fail Root Cause

**Founder ruling 2026-07-04 evening:** Option B — delay Day 2 by 1 day, investigate 54 fails first.

**Result:** ROOT CAUSE IDENTIFIED + FIXED (commit `761a593b`) within 1 hour. Not a systemic issue.

## What was broken

**Wave 9 smoke against staging** returned 54 failures. Reproduced `GET /api/Events/my-events` → HTTP 500 `DatabaseConfigurationError`.

**Actual Postgres error** (from staging container logs): `42703: column t.CreatedBy does not exist`.

**Stack trace:** `EventRepository.GetByOrganizerAsync` line 331 → `.Include(e => e.TicketTiers)`.

## Root cause

`TicketTierConfiguration.cs` was missed in the **Wave 4.9.2.10a Phase 1.10a** physical-column sweep (2026-06-09). All other IAuditable entity configs got `.Property(e => e.CreatedBy).HasColumnName("created_by")` — TicketTier didn't.

- The DB has `created_by` (snake_case) from the Wave 4.9 migration.
- TicketTier is `Entity<Guid> + IAuditable` (per Wave 3.C W3C migration).
- Without HasColumnName, EF defaults to PascalCase `CreatedBy`.
- Every query that `.Include(e => e.TicketTiers)` emits SQL with `t.CreatedBy` (PascalCase) — Postgres returns 42703.

## Fix

Commit [`761a593b`](https://github.com/Niroshana-SinharaRalalage/LankaConnect/commit/761a593b) adds explicit `HasColumnName` on:
- `TicketTierConfiguration` (primary — fixes the observed 31 Events failures + downstream)
- `TicketScanLogConfiguration` (preemptive — same class LegacyBaseEntity gap, no smoke currently hits it but the bug would manifest)

Total 2 files, +13 lines. No migration needed (DB already has `created_by`/`updated_by` columns).

## Audit of remaining LankaEvents configs

Multi-line grep on all 38 `LankaEvents.Infrastructure.Configurations/*.cs`:

| Entity | HasColumnName-CreatedBy | Verdict |
|---|:-:|---|
| Event | ✅ | OK |
| Registration | ✅ | OK |
| EventImage | ✅ | OK |
| **TicketTier** | ❌ → ✅ | FIXED |
| **TicketScanLog** | ❌ → ✅ | FIXED |
| EventBadge | ✅ (multi-line, initial grep missed) | OK |
| EventAnalytics | ✅ (multi-line, initial grep missed) | OK |
| EventEmailGroupLink | N/A (plain class, not IAuditable) | OK |
| TierAssignment | N/A (plain class, not IAuditable) | OK |
| EventViewRecord | N/A (plain class, not IAuditable) | OK |
| All other 28 IAuditable configs | ✅ | OK |

## Cascade prediction

All 54 failures are on Event-scoped endpoints (`/api/Events/*` and `/api/events/{eventId}/*`):
- Events (31): direct fail on TicketTier include
- Sponsors (5), Donations (5), Collections (5), AddOns (4), SponsorshipPackages (2): all under `/api/events/{eventId}/*` — the smoke fixture reads Event first (which fails → 500), then downstream sub-resource operations either cascade-fail or receive an invalid/null event ID
- Newsletters (1), PhotoAlbums (1): POST creates with `{eventId}` that came from failing Event read

**Prediction:** post-fix Wave 9 smoke reduces from **251/54/94** to **~300+/0-5/94**. Any residual fails will be unrelated to the TicketTier issue.

## Time spent

- Investigation: 15 min (login → reproduce → container log grep → stack trace)
- Fix + audit: 20 min (targeted edits + multi-line grep verification)
- Deploy wait: ~15 min (Azure Container App rebuild)

**Total: ~50 min** from founder's Option B ruling to fix deployed.

## Next

1. Confirm 761a593b/710305d4 deploy success (in progress)
2. Re-run Wave 9 smoke
3. If failures ≤ ~5 unrelated: FIRE Day 2 (Agents A-E in parallel; Agent F done)
4. If failures > 10: iterate on remaining, likely not TicketTier-family

## Sprint schedule impact

Day 2 originally Mon 2026-07-06. With this investigation done today Fri, Day 2 could fire tonight/tomorrow (Sat). NET impact on 2-week deadline: **zero — or +1 day of buffer.**
