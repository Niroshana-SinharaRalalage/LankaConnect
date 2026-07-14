# Consult #25 — Sprint Day 7 Attack Order + UoW Disposition (2026-07-13)

## Context

Sprint Day 7 EOD (2026-07-13). 17 hotfix deploys landed; Wave 9 API smoke pass rate moved 43.33% → 65.91% (130 → 263 pass out of 399). Sprint bible §Day 7 target "100+/261 pass" cleared by 141.

**Deploy chain summary:**

| Deploy | Change | Wave 9 |
|---|---|---|
| 13th `22cb1170` | Boot cascade cleared (Ignore chain + Communications outbox migration + CI wire) | 130/53/117 (43.33%) |
| 14th `169ad6a0` | Restored `[ApiController]/[Route]/[Produces]` on 4 local BaseController copies (4C.d.vi regression) | 135/48/117 (45.00%) |
| 15th `96a2d618` | `UserRepository.GetByIdAsync` guard shadow-nav Include on IdentityDbContext model | 241/47/110 (60.55%) |
| 16th `52291520` | CreateEventCommandHandler → `IMultiContextUnitOfWork.CommitAsync(new[] { _dbContext }, ct)` | ATTEMPT FAILED — cross-connection tx error |
| 17th `494bcb8a` | Backed off to `_dbContext.SaveChangesAsync(ct)` directly | 263/34/102 (65.91%) |

## Residual 34 fails (Consult #25 questions target these)

| Cluster | Fail | Shape |
|---|---:|---|
| Events | 12 | Mixed: paid-event Currency serialize, list-endpoint JSON reader trap, attendees 500, export services 500, ics 404 |
| AdminUsers | 7 | **Stubborn — same count across 4 baselines; different root-cause family** |
| Newsletters + Newsletter + EmailGroups | 6 | Not diagnosed |
| Sponsors/SP-pkg/Donations/Coll/AddOns | 6 | Downstream of paid-event fixture |
| Businesses/Users/Auth/PhotoAlbums | ~6 | Various singles |

## Debt-flag items surfaced today

1. **~95 LankaEvents.Application handlers still on `IUnitOfWork`** — same write-loss shape as CreateEvent split-brain. Hidden until traffic hits.
2. **Wave 6.5.a `IMultiContextUnitOfWork.CommitAsync(DbContext[])` broken** — `moduleContext.Database.UseTransactionAsync(appTx, ...)` throws "transaction not associated with the current connection". AppDbContext + module contexts pull separate Npgsql pool connections.
3. **Domain-event dispatch gap on LankaEventsDbContext** — dispatched only via `AppDbContext.CommitAsync`. Wave 6.5.b-d design says per-module SaveChangesInterceptor; not wired.
4. **Metro-area preference write path** — `UpdateUserPreferredMetroAreasCommandHandler` uses `_context.Entry(user).Collection("_preferredMetroAreaEntities")` under IdentityDbContext which `Ignore<MetroArea>()`s.
5. **LegacyPromotions folder split** (Consult #17 Day 10 debt).
6. **Test-project full-solution build** (SPRINT-D7.1 per Consult #18). CI narrowed to `LankaConnect.API.csproj`. Restore Day 10.

## Questions posed to architect

- **Q1** — Complete Day 7 attack order for 34 residual: (a) Events list-endpoint JSON reader, (b) AdminUsers 7, (c) paid-event Currency, (d) parallel small clusters. Give ordering with rationale.
- **Q2** — Wave 6.5.a broken UoW disposition: (A) fix cross-connection wiring properly (~2-3 days) vs (B) migrate 95 handlers to direct `_dbContext.SaveChangesAsync(ct)` pattern.
- **Q3** — AdminUsers 7 stubborn hypothesis.
- **Q4** — Day 8 vs jump-to-Day 9 (target 150+/261 already cleared).
- **Q5** — Debt-flag sequencing (in-sprint vs Wave 6.5 vs Phase A.5).
- **Q6** — Rule 5b blanket for direct-SaveChanges pattern (analog to Consult #19 Ignore<T> blanket).

## Architect rulings

### Q1 — Attack order

Execute in this sequence:
1. **(a) Events list-endpoint JSON reader trap FIRST** — single root cause, cascades to Sponsors/SponsorshipPackages/Donations/Collections/AddOns (6 downstream). Fix `EventRepository.GetByOrganizerAsync` JSON converter registration. ~10-12 pass expected.
2. **(d) In parallel: farm Newsletters (3) + singletons** — unrelated to money-flow and unblock quickly.
3. **(c) Paid-event Currency serialize-as-object** — do AFTER (a) because (a)'s converter fix may resolve the same shape.
4. **(b) AdminUsers 7 LAST** — different root-cause family, doesn't cascade.

**Rationale:** highest cascade-multiplier first; parallelize independent tails; leave the stubborn isolated cluster where it can't block anything else.

### Q2 — Wave 6.5.a broken UoW disposition

**Option B (pragmatic — direct `_dbContext.SaveChangesAsync`).** Wave 6.5.a's `UseTransactionAsync` cross-connection model was Consult #7 Delta's speculative wiring; never staging-proven. Fixing it properly (shared `NpgsqlConnection` per scope, DbContext factory rewire, transaction enlistment) is a ~2-3 day surface with Rule 5b + Rule 5c + Rule 5j.4 audit — **out of sprint scope**.

**Blanket condition:** domain-event dispatch must be preserved. Wire a per-module `SaveChangesInterceptor` that calls the same `DomainEventDispatcher` — ~4-hour surface, **do it in Day 8 as prerequisite**. Atomicity across contexts stays best-effort until Phase A.5.

### Q3 — AdminUsers 7 stubborn hypothesis

**Guess: permissions filter / IIdentityQueries scaffolding gap**, not write-path. If it were IdentityDbContext write-path it would have moved with the 15th deploy's `GetByIdAsync` shadow-nav guard.

**First grep:** `grep -rn "IApplicationDbContext\|_context\.Users\|_context\.Roles" src/Products/LankaEvents/Admin/ src/Capabilities/Identity/` — expect admin-surface queries still hitting removed injector or unmapped nav.

### Q4 — Day 8 vs jump-to-Day 9

**Run Day 8 as sprint bible frames it. Non-negotiable.** Day 9's Baseline Restoration gate (182/0/79 + ArchTest 57/0/0) will not hold with 34 → even 15 residual. Day 8's 5-agent structure exists specifically to dissolve the residual before the baseline gate. Leapfrogging = certain Day 9 rollback.

### Q5 — Debt-flag sequencing

- **In-sprint Day 7-9**: #3 domain-event dispatch interceptor (Day 8, mandatory per Q2), #1 direct-SaveChanges handler migration (Day 8, mechanical after Q6 blanket).
- **Wave 6.5.f/g/h queue**: #4 metro-area cross-module write (needs Blueprint §7.8 `IMetroAreaRepository` scaffold — Wave 6.5.g adjacent).
- **Phase A.5**: #2 proper `IMultiContextUnitOfWork` shared-connection rewire, #5 LegacyPromotions folder split (already Day 10 debt), #6 test-project full-solution build restore (already Day 10 debt).

### Q6 — Rule 5b blanket on direct-SaveChanges pattern

**Blanket approved** under these three conditions (analog to Consult #19):
1. Handler writes to exactly ONE module DbContext (no cross-module writes in the same handler).
2. Domain-event dispatch interceptor is wired on that DbContext BEFORE the migration commit lands (Day 8 prerequisite per Q2).
3. Commit body lists T-9 trigger + names the handler + confirms single-context scope.

Cross-module write handlers (metro-area, cross-aggregate) still need per-handler consult. If you hit one, STOP and open a targeted consult.

### Status flip line for sprint bible §Day 7

`Day 7 — STAGING-VERIFIED 2026-07-13 — 263/34/102/65.91% (bible target 100+/261 cleared by 141); residual attack order Q1 approved; UoW disposition Option B ratified; domain-event interceptor Day 8 mandatory; Day 8 proceeds as planned.`

## Execution outcome (retro after Consult #25 attack)

**Consult #25 rulings all executed in sequence 2026-07-13 → 2026-07-14 across deploys 18-22:**

| Deploy | SHA | Executed | Wave 9 |
|---|---|---|---|
| 18th | `55ee3174` | Batch (a)+(d): HeadCount jsonb drop + CommunicationsDbContext Newsletter model + Newsletter/User handler cross-module MetroArea injection + MetroAreaMappingProfile AutoMapper assembly scan | Failed EF migration (NewsletterConfig.HasOne<Event> cascade) |
| 19th | `d3216796` | Attack (c): Currency `[JsonConverter]` emits ISO 4217 code string | Failed same EF migration |
| 20th | `9117e430` | CommunicationsDbContext.Ignore<Event>+Ignore<MetroArea> unblocks migration; batch (a)+(d)+(c) all live | 285/27/88 (71.25%) — Users/Newsletters/Newsletter/EmailGroups fully green |
| 21st | `8bcb1148` | Attack (b): IdentityDbContext.Ignore('_preferredMetroAreaEntities' shadow-nav) + RegisterUserHandler raw-SQL junction insert | Register still 500 — call site was in UserRepository.AddAsync override, not RepositoryBase |
| 22nd | `ddd1d2a8` | UserRepository.AddAsync shadow-nav sync block DELETED | (measured post-deploy) |

**Q3 diagnosis update:** AdminUsers 7 was NOT a permissions filter — it was the CASCADE of the AutoMapper MetroArea profile miss (Fix from Q1(d)). All 7 died at fixture-setup `Get-LcAnyMetroAreaId → /api/metro-areas 400`. Once metro-areas endpoint returned 200 (post-Fix-4 AutoMapper scan), AdminUsers moved to the next block (`register 500`), which was resolved by the 22nd deploy UserRepo fix.

**Q1(a) partial-resolution:** Events list-endpoint `HeadCount HasColumnType("jsonb")` drop was correct diagnosis for one shape but the ACTUAL failure lives deeper in `OwnsOne(TicketPrice).ToJson("ticket_price")` MaterializeJsonEntity — cross-row JSON shape drift across legacy vs post-Consult-#23 writes. Requires Consult #26 (data-migration decision or ToJson→scalar-column refactor). Deferred to Phase A.5.

**Deploy-day realities:** Attack order landed as 22 total commits over 07-13 → 07-14, not the 1-day sprint expectation. Actual calendar Day 9 close.
