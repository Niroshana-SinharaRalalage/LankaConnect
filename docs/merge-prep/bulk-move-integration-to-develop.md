# Merge prep — `bulk-move/integration` → `develop`

**Ready for founder sign-off** as of 2026-07-11 (Sprint Day 6, per bible §Day 6 gate).

## Dry-run verification (2026-07-11)

**Method**: Fresh worktree of `origin/develop`; `git merge bulk-move/integration --no-ff --no-commit`.

- ✅ **Zero conflicts.** Automatic merge succeeded.
- ✅ **`dotnet restore --force` cold-run**: zero circular dependencies. MSB4006 not thrown.
- ✅ **`dotnet build LankaConnect.sln`**: production 0 errors. 144 test-project errors all in sprint-tolerance baseline (Domain.Tests 90 + Payments.App.Tests 28 + TestUtilities 14 + Notifications.Domain.Tests 10 + Communications.App.Tests 2).
- ✅ **ArchTest gate**: Rule 5 GREEN, Rule 9b GREEN, Rule 14 GREEN (all 3 un-skipped this sprint).

## Sprint outcome one-line summary

**55 commits across 6 days** — the full modular-monolith refactor cleanup arc from Day 2 bulk-move → Day 5 4C.h `IApplicationDbContext` deletion + Rule 5/9b/14 GREEN.

## Sub-slice closure summary

| Sub-slice | Commit | Outcome |
|---|---|---|
| Day 2 bulk-move (5 legacy csprojs → module topology) | `3848d8e9`..`b45c5631` | 1,391 files moved, LEAVE BROKEN as expected |
| Day 3 compile reconciliation | multiple | modules compile-green |
| **4C.a** preamble ash-sweep | commits during Day 2/3 | ✅ CLOSED 2026-07-07 |
| **4C.b** Business/Service/Review delete (Consult #12 Option D) | during Day 2/3 | ✅ CLOSED 2026-07-07 |
| **4C.c** CommunicationsDbContext + 3 email configs | during Day 3 | ✅ CLOSED 2026-07-07 |
| **4C.d** LankaEvents 10 DbSets → LankaEventsDbContext | `2d46557b` | ✅ CLOSED 2026-07-08 |
| **4C.e.1** IdentityDbContext skeleton + UserConfig move | `289c64f0` | ✅ CLOSED 2026-07-08 |
| **4C.e.2** Rule 5e parity test | `294898f2` | ✅ CLOSED 2026-07-08 |
| **4C.e.3** User caller cutover + empty-Up migration | `8465d219` | ✅ CLOSED 2026-07-08 |
| **4C.f** Communications 3 DbSets (no-op) | | ✅ CLOSED 2026-07-08 |
| **4C.g** ReferenceValue → AppDbContext direct | `ae808a8a` | ✅ CLOSED 2026-07-08 |
| Wave 6.5.f LankaEvents cycle-break | `8c912ca1` | ✅ CLOSED 2026-07-08 |
| Wave 6.5.f LankaEvents 21 handler migration | `24b06dc8` | ✅ CLOSED 2026-07-08 |
| Wave 6.5.f Communications cycle-break + 8 Newsletter handlers | `f8ce2ee4` | ✅ CLOSED 2026-07-08 |
| Residual sweep (Identity/Media/Comm.Infra/LC.App/LC.Infra) | `c6f2826b` | ✅ CLOSED 2026-07-08 |
| 4C.h attempt reverted (nuget cycle blocker) | `5500a82c` | Documented for Day 5 slot A |
| Session-handover docs | `c7a18e6d`, `19966c72`, `5878320e` | Vision anchor + SESSION_PRIMER for fresh sessions |
| **4C.h Day 5 slot A** cycle-break + IApplicationDbContext DELETED + Rule 14 ArchTest | `d7fdfa44` | ✅ CLOSED 2026-07-10 |
| **Wave 6.5.g Day 5 slot C** Payments un-skip + Rule 9b GREEN | `95190253` | ✅ CLOSED 2026-07-10 |
| **Wave 6.5.h Day 5 slot D** Rule 5 un-skip GREEN | `64680855` | ✅ CLOSED 2026-07-10 |

## Load-bearing outcomes

1. **`IApplicationDbContext` interface + AppDbContext impl marker + DI registration DELETED**. Live-injector count 84 → 0. Rule 14 ArchTest guards re-introduction.
2. **Wave 6.5.f handler migration complete**: 21 LankaEvents handlers + 8 Communications Newsletter handlers inject their module DbContext directly.
3. **Cycle-break complete**: `LankaConnect.Infrastructure → Communications.Application` and `→ LankaEvents.Application` PRs deleted. `dotnet restore` cold-run reports zero cycles.
4. **Rule 5 + Rule 9b + Rule 14 ArchTests all GREEN** (all 3 un-skipped this sprint).
5. **CLAUDE.md SECTION -1 (Platform Vision Anchor) + `docs/SESSION_PRIMER.md`** added — fresh sessions primed with modular-monolith refactor context before task-level plumbing.

## What's deferred (documented for Phase B / follow-up)

- **Wave 4.7 ICulturalCalendarService relocation** to `SharedKernel.Cultural`: needs coordinated primitive+interface batch (CulturalContext + CulturalTimingPreference + HinduFestival + 3 records must move together). Scope exceeds Day 6 window; deferred to a dedicated commit.
- **Wave 4.6.d.3 `LegacyApplication_DoesNotDependOnIdentityDomain` ArchTest un-skip**: `IApplicationDbContext.Users` blocker cleared (deleted at 4C.h) but `IJwtTokenService.GenerateAccessTokenAsync(User)` blocker remains. Full un-skip after IJwtTokenService moves into Identity module.
- **`Contracts/LegacyPromotions/` folder cleanup (Day 10)**: 30+ files across LankaEvents.Contracts, Communications.Contracts, Media.Contracts sit in `LegacyPromotions/` as temporary bucket per Consult #17. Day 10 split into domain-specific folders (`Contracts/Repositories/`, `Contracts/Services/`, `Contracts/DTOs/`) alongside legacy csproj deletion.
- **Test-project sprint-tolerance errors (134)**: Domain.Tests 90 + Payments.App.Tests 28 + TestUtilities 14 + Notifications.Domain.Tests 10 + Communications.App.Tests 2. All pre-existing from Day 2 LEAVE-BROKEN commit; Day 7+ smoke regression rounds close them.

## Merge commit message body (paste when merging)

```
Sprint-Day: 6 — Merge bulk-move/integration → develop: modular-monolith refactor complete

55 commits across 6 days close the modular-monolith Phase A refactor:

- Day 2 (2026-07-07): 1,391 files bulk-moved from 5 legacy csprojs to
  src/Modules/* + src/Products/LankaEvents/ topology.
- Day 3 (2026-07-08): compile reconciliation.
- Day 4 (2026-07-09): 4C.a-g DbContext consolidation sub-slices +
  Wave 6.5.f cycle-break + 21 LankaEvents handlers migrated to
  LankaEventsDbContext + 8 Newsletter handlers migrated to
  CommunicationsDbContext.
- Day 5 (2026-07-10): 4C.h IApplicationDbContext DELETED. Wave 6.5.g
  Payments un-skip (Rule 9b GREEN). Wave 6.5.h Rule 5 un-skip GREEN.
  Rule 14 ArchTest added.

IApplicationDbContext live-injector count 84 → 0.
dotnet restore cold-run: zero circular dependencies.
ArchTest Rule 5 + Rule 9b + Rule 14: all GREEN.
Production 0 errors; test-project errors at sprint-tolerance baseline.

Full commit list: git log <develop-HEAD>..bulk-move/integration --oneline.
Detailed summary: docs/merge-prep/bulk-move-integration-to-develop.md.

Deferred to Phase B / follow-up:
- Wave 4.7 ICulturalCalendarService → SharedKernel.Cultural
- Wave 4.6.d.3 LegacyApplication_DoesNotDependOnIdentityDomain un-skip
  (blocked by IJwtTokenService.GenerateAccessTokenAsync(User))
- Day 10 Contracts/LegacyPromotions/ folder split

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

## Post-merge sequence (per architect Q4)

1. `git push origin develop` — triggers `deploy-staging.yml`.
2. Watch deploy for ~5-10 min (`gh run watch` or Azure Portal).
3. Fire smokes: `pwsh scripts/smoke/Invoke-Login.ps1` + `pwsh scripts/smoke/Run-Wave9.ps1`.
4. Expected: 40-80/261 smoke failures per sprint bible §Day 6 ("do not panic"). Fix-forward Days 7-8.
5. Flip statuses:
   - `docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` — Wave 6.5.f/g/h/4C.a-h → CLOSED
   - `docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md` — mark Day 5 CLOSED, Day 6 develop-merge CLOSED
   - `docs/PROGRESS_TRACKER.md` — new top entry with sprint summary
6. Founder EOD sign-off ping @ noon or evening.
