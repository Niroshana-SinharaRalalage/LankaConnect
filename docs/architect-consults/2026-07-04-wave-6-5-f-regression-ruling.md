# Architect Ruling — Wave 6.5.f Regression (Cross-Module Navigation Ignore Trap)

**Date**: 2026-07-04
**Participants**: Founder (Niroshana, executing agent), System Architect (Claude Opus 4.7 persona)
**Status**: BINDING — supersedes any sub-slice-level self-authorization taken between 2026-07-02 and now
**Related**: `2026-07-02-wave-6-5-scope-shape.md` §5 hard-STOP #2 (Wave 9 smoke regression), §5 hard-STOP #7 (dual-mapping snapshot drift); Blueprint §7.4 (module DbContext ownership), §7.8 (cross-module reference rules), §7.16 (LankaEventsDbContext extraction)

---

## 1. Diagnosis (what actually happened)

The 46-failure regression has ONE root cause with TWO surfaces:

**Root cause**: `LankaEventsDbContext.OnModelCreating` (lines 165-178) calls `modelBuilder.Ignore<>()` for four types that `EventConfiguration` — which IS registered in this context via `ApplyConfigurationsFromAssembly` at line 146 — declares navigations to:

| Ignored type | Owner | Navigation on `Event` | Include chain that breaks |
|---|---|---|---|
| `EventEmailGroupLink` | `Products.LankaEvents.Domain.Entities` (in-module junction) | `Event.EmailGroupLinks` (line 209-ish, backed by `_emailGroupLinks`) | `EventRepository.GetByIdAsync` line 74 |
| `EmailGroup` | `Modules.Communications.Domain.Entities` | none direct — FK principal for the junction | (indirect — junction depends on this being either mapped or the FK left scalar) |
| `EventBadge` | `Products.LankaEvents.Domain.Entities` (in-module junction) | `Event.Badges` (line 209 in `Event.cs`, backed by `_badges`) | `EventRepository.GetEventsWithBadgeAsync` line 874; `GetEventsWithExpiredBadgesAsync` line 887-888 |
| `Badge` | `LankaConnect.Domain.Badges` (cross-module principal) | `EventBadge.Badge` (via ThenInclude) | `GetEventsWithExpiredBadgesAsync` line 888 |

`modelBuilder.Ignore<T>()` runs LAST per the file's own comment (line 162-164). It wins over the `HasMany(e => e.EmailGroupLinks)` declared inside `EventConfiguration.cs:530`. Result: EF Core 8 model-build succeeds (nothing throws at startup), the navigation property still exists on the CLR type, but at query time EF cannot resolve `e.EmailGroupLinks` back to a mapped relationship — hence "not a property access." The identical failure lies in wait on the two Badge Include chains — those queries just aren't exercised by the RSVP / event-detail smoke path but ARE exercised by `ExpiredBadgeCleanupJob` and any admin badge query.

Wave 6.5.e's decision was structurally wrong in one specific way: **it treated cross-module navigation as a boolean (map-everything-or-Ignore-everything) when EF gives you three options** — (1) map the principal, (2) leave the FK as a scalar shadow property with no navigation, (3) map the junction only, keeping the far principal foreign. The Ignore<> path collapses (1) and (3) into "not mapped at all," which is why the assembly-sweep-registered `EventConfiguration.HasMany(e => e.EmailGroupLinks)` collides with the Ignore.

Wave 9.h.10.6 F30a already proved that "Infrastructure.Tests green" is not a sufficient signal for repo/DbContext-shape changes (a symptom that took real production data loss to surface). The 371/371 green on 6.5.e was measuring the wrong thing: unit tests exercise `AppDbContext` (which still has the full model), not `LankaEventsDbContext`. The dual-mapping design of 6.5.e means AppDbContext queries pass while LankaEventsDbContext queries fail — and 6.5.f.5's cutover of `EventRepository` + `RegistrationRepository` was the moment the failing context started serving those queries.

---

## 2. Binding decision — Option B (Forward-fix), executed as sub-slice 6.5.f.5-hotfix

**Rejected**: Option A (revert f.5) and Option C (revert all of f).

**Reasoning for the rejection**:

- **Against Option A**: The regression is not in `EventRepository` / `RegistrationRepository` logic — those cutovers are correct. The bug is in `LankaEventsDbContext.OnModelCreating`. Reverting f.5 removes the trigger but leaves the trap armed for f.4 (payment cluster). f.4 will hit the identical bug the moment it cuts over `RegistrationPaymentRepository` or `TicketRepository` if either transitively loads Event with EmailGroupLinks (grep confirms `RegistrationPaymentRepository` doesn't Include Event, but the payment-completed flow reads Event via `IEventRepository` from `PaymentCompletedEventHandler` — the same failing chain). Revert is a stall, not a fix.
- **Against Option C**: Discarding 6.5.a-e's correctness because 6.5.e made ONE localized wrong call is theatrical. 6.5.a (mechanism), 6.5.b/c/d (self-save retirement) all ship independently correct behavior. 6.5.e's DbContext + configs relocation is correct; only the Ignore<> block is wrong. Reverting all of f wastes ~4 sessions of correct work to fix one file's model-creating block.
- **Against a fourth option "temporarily strip the failing Include chains from EventRepository"**: would leak `Event.EmailGroupLinks` and `Event.Badges` as always-empty at runtime, silently corrupting authorization and template resolution across the entire Events surface. This is the F30a shape (silent data loss) — the 2026-07-02 ruling §Q6 forbade preserving that pattern.

**What Option B ships** (single commit, one working session, tagged `Wave-6.5.f.5-hotfix` for revert traceability):

### 2.1 EventEmailGroupLink — Path B1 (physical move + un-Ignore)

- Move `src/LankaConnect.Infrastructure/Data/Configurations/EventEmailGroupLinkConfiguration.cs` to `src/Products/LankaEvents/LankaEvents.Infrastructure/Configurations/EventEmailGroupLinkConfiguration.cs`.
- Change namespace from `LankaConnect.Infrastructure.Data.Configurations` to `LankaConnect.Products.LankaEvents.Infrastructure.Configurations`.
- Remove `modelBuilder.Ignore<EventEmailGroupLink>()` from `LankaEventsDbContext.cs:175`.
- Keep `modelBuilder.Ignore<EmailGroup>()` at line 176 — the junction's `EmailGroupId` is a scalar Guid; no navigation from the junction back to `EmailGroup` exists, so leaving the principal Ignored keeps LankaEventsDbContext's model free of Communications-module types. This is the correct application of "map the junction, leave the far principal foreign" per §7.8.
- In `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`: remove the explicit `modelBuilder.ApplyConfiguration(new EventEmailGroupLinkConfiguration())` on line 214 — the runtime `Assembly.Load(...)` sweep on line 201-203 will now pick it up from the moved location, so AppDbContext continues to map the junction identically. This preserves the dual-mapping intent of 6.5.e for the duration of 6.5.f.6 cleanup.
- **Justification for physical move**: `EventEmailGroupLink` is a `Products.LankaEvents.Domain.Entities` type. Its configuration was always mis-located in `LankaConnect.Infrastructure` — Wave 6.5.e should have moved it. This closes that oversight.

### 2.2 EventBadge — Path B2 (physical move + un-Ignore, junction-only)

- Move `src/LankaConnect.Infrastructure/Data/Configurations/EventBadgeConfiguration.cs` to `src/Products/LankaEvents/LankaEvents.Infrastructure/Configurations/EventBadgeConfiguration.cs`.
- Change namespace to `LankaConnect.Products.LankaEvents.Infrastructure.Configurations`.
- **In the moved config, DELETE the `HasOne(eb => eb.Badge)` block** (currently lines 79-82 of `EventBadgeConfiguration.cs`). That relationship is what forces `Badge` to be mapped in the same context. Replace it with nothing — `BadgeId` remains a scalar FK column mapped on line 29-30. The `Badge` navigation property on `EventBadge` becomes an unmapped CLR property (EF ignores it).
- Remove `modelBuilder.Ignore<EventBadge>()` from `LankaEventsDbContext.cs:178`.
- **Consequence for `EventRepository.GetEventsWithExpiredBadgesAsync` line 887-888**: `.ThenInclude(eb => eb.Badge)` will now fail at query time because `Badge` is not a mapped principal in `LankaEventsDbContext`. This is CORRECT behavior — the ThenInclude was a cross-module reach that should not exist.
- **Compensating change to `EventRepository`**: rewrite `GetEventsWithExpiredBadgesAsync` (lines 883-891) to project only `EventBadge` scalars (`.Select(e => new { e.Id, ExpiredBadges = e.Badges.Where(eb => eb.ExpiresAt.HasValue && eb.ExpiresAt < now).Select(eb => eb.BadgeId) })`) — the caller `ExpiredBadgeCleanupJob` only needs `BadgeId`s to enact removal. If the caller needs Badge names/images for logging, it queries `IBadgeRepository` separately by ID list. This is the cross-module read pattern §7.8 blesses: **each module queries its own DbContext; cross-module reads happen at the application layer via a Contracts-projection, not via `.ThenInclude`.**
- Grep target: `GetEventsWithExpiredBadgesAsync` has ONE caller (`ExpiredBadgeCleanupJob`). Verify + update it in the same commit.
- **In `AppDbContext.cs`**: remove line 265's explicit `modelBuilder.ApplyConfiguration(new EventBadgeConfiguration())`. The assembly-sweep on line 203 will pick it up from the moved location. This is important — leaving both the explicit line AND the sweep would double-register.
- Line 264's `modelBuilder.ApplyConfiguration(new BadgeConfiguration())` stays put. `Badge` remains owned exclusively by `LankaConnect.Domain.Badges` and mapped exclusively by `AppDbContext`. Nothing about Badge changes in this hotfix.

### 2.3 What Path B2 explicitly does NOT do

- **Does NOT physically move `BadgeConfiguration.cs`.** Badge is not a LankaEvents type. Its physical namespace is `LankaConnect.Domain.Badges` — Badge is a shared/cross-module aggregate (used by Events for overlays, but also referenced from `LankaConnect.Application/Badges/Queries/*`). Moving it into `Products/LankaEvents/LankaEvents.Infrastructure` would misrepresent ownership and create a reverse dependency (Products → LankaConnect.Domain for its own type). It stays in `LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs`, mapped by AppDbContext only.
- **Does NOT cross-register BadgeConfiguration in LankaEventsDbContext via `ApplyConfiguration(new BadgeConfiguration())`.** That would pull the Badge value-object graph (`ListingConfig`, `FeaturedConfig`, `DetailConfig` owned-entity trees) into a second context, doubling the snapshot surface and setting up a §7 hard-STOP #7 drift regression at 6.5.f.6 cleanup.
- **Does NOT preserve `.ThenInclude(eb => eb.Badge)` anywhere in `EventRepository`.** That call is the anti-pattern — the module boundary crossing via EF navigation is exactly what Wave 6.5 was chartered to remove.

### 2.4 Registration cross-module scan (proactive, same commit)

While hotfixing, grep `EventRepository.cs` and `RegistrationRepository.cs` for any other `.Include` / `.ThenInclude` that targets a type owned outside `Products.LankaEvents.Domain`. Two types of hit are possible:

- Includes of `User` — should already be broken if any exist; the current Ignore<User>() is intentional and correct per §7.4 (Identity is a Modules boundary, not a Products-internal type).
- Includes of `Newsletter`, `SupportTicket`, or other Modules types — treat identically to Badge: strip the ThenInclude, project scalar FK, cross-module read at application layer.

Report the scan result in the hotfix commit message so future waves have a clean bill of health on `EventRepository`'s cross-module navigation surface.

---

## 3. Acceptance criteria (what green looks like on staging)

The hotfix ships as commit `Wave 6.5.f.5-hotfix: un-Ignore junctions + move junction configs to Products.LankaEvents.Infrastructure` on branch `wave-6-5-f-5-hotfix`, PR-merged to `develop` after ALL of the following are green:

1. **Build**: `dotnet build LankaConnect.sln -c Release` clean.
2. **Full unit + integration test suite**: 371/371 as pre-hotfix baseline. No count regression. This ONLY proves the hotfix didn't break AppDbContext-path callers.
3. **Snapshot drift check**: `dotnet ef migrations has-pending-model-changes` returns "No pending model changes" for BOTH `AppDbContext` AND `LankaEventsDbContext`. If there IS drift, the migration surface has to be reviewed before merge (do NOT auto-generate a Wave 6.5.f.5-hotfix migration — that means the physical schema disagrees with the reshaped model and is a separate architect consult).
4. **Model-build parity test**: A new one-off unit test (may be a scratch test that gets deleted post-verification) that builds both DbContexts' models via `EF.Model` and asserts `EventEmailGroupLink` is a mapped entity in `LankaEventsDbContext.Model.GetEntityTypes()` and `EventBadge` likewise. This is the specific coverage gap that let 6.5.e ship broken.
5. **Local smoke against staging DB clone (if available) or targeted repo-level integration test**: query `EventRepository.GetByIdAsync(<any published event ID>, false)` through the DI graph and assert `Result.EmailGroupLinks` and `Result.Badges` collections materialize without throwing. This is the specific query that was failing — if it works locally against the same DbContext the API uses, it works on staging.
6. **Staging smoke**: `Run-Wave9.ps1` full run returns to the pre-Wave-6.5.f baseline. Specifically: 46 failures resolve to 0 in the Events / Sponsors / Donations / Collections / PhotoAlbums / AddOns / SponsorshipPackages / Newsletters / VenueLayouts / Analytics groups. Per-controller failure count for Events must be ≤ 2 (allowing for pre-existing skips → skips-not-fails). Any residual failure means the hotfix didn't cover a query path — halt and re-consult.
7. **No baseline JSON change**: `Wave6_5TransitionalBaseline.json` stays at 4 entries. The hotfix does not touch repositories or attributes.

Steps 3-5 are the pre-merge gates that 6.5.e / 6.5.f.5 lacked. Step 6 is the post-merge validation gate. Step 4's model-build parity test becomes a permanent test in `LankaConnect.Infrastructure.Tests/DbContextParityTests.cs` — see §5.Q5 below.

---

## 4. Sequencing implication for downstream sub-slices

The 2026-07-02 ruling sequenced `6.5.g` BEFORE `6.5.f.4`. That ordering STANDS. But 6.5.f.4 (payment cluster cutover) MUST NOT begin until:

- The hotfix in §2 has merged to `develop` and staging is green.
- A one-line check is added to the 6.5.f.4 pre-flight: the payment-cluster repositories DO NOT have any `.Include(x => x.Badges).ThenInclude(y => y.Badge)`-shaped cross-module navigation. Grep the four repositories (`AddOnPurchaseRepository`, `RegistrationPaymentRepository`, `RegistrationAdditionRepository`, `TicketRepository`) for `.ThenInclude` — the current inventory says these are payment-scalar-heavy repos and shouldn't have cross-module `.ThenInclude` chains, but this is exactly the assumption 6.5.e made and it turned out to be wrong. VERIFY before cutting over.

6.5.g and 6.5.h are unaffected by this ruling — they touch Application-layer references, not repository DbContext wiring.

6.5.f.6 (dual-mapping cleanup) MUST verify that `AppDbContext.OnModelCreating` no longer has explicit `modelBuilder.ApplyConfiguration(new EventEmailGroupLinkConfiguration())` and `EventBadgeConfiguration()` calls (the hotfix removes them and relies on the assembly sweep). If somehow both are re-added, hard-STOP #7 fires.

---

## 5. Rulings on process questions Q1-Q5

### Q1 — Subagent-added `Ignore<>()` requires architect re-consult

**Verdict**: YES. Non-negotiable. Elevated to a hard-STOP #8 for the remainder of Wave 6.5.

**Reasoning**: `modelBuilder.Ignore<T>()` is a semantic mapping decision, not a mechanical relocation. It says "this type is not in this bounded context." That is precisely a §7.4 module ownership statement, which the blueprint reserves to architect ruling. The 6.5.e subagent's inline comment — "keep LankaEventsDbContext scoped to the Event family" — is a correct instinct but wrong tool: EF gives THREE options (map, scalar-shadow-FK-only, Ignore), and Ignore is the highest-cost choice because it collides with any nav mapped via `HasMany`/`HasOne` in a swept config. The alternatives are mechanical (delete the `HasOne(eb => eb.Badge)` block, leave FK as scalar).

**Process rule** (add to `docs/PLATFORM_MASTER_PLAN.md` Agent Operating Protocol):

> **Rule 5b — Model-shape decisions require architect consult**: Any commit that adds `modelBuilder.Ignore<>()`, `HasNoKey()`, `HasQueryFilter()`, or changes an entity's `.ToTable(name, schema)` in a NEW or EXISTING DbContext, halts until architect consult approves the intent. "The tests pass" is not a substitute for the ruling because these are model-shape decisions that are silent at build time and loud at first cross-context query. The consult is cheap (a 2-message architect turn); the regression from skipping it is expensive (§5 hard-STOP #2 fires post-merge).

### Q2 — Staging-smoke pre-merge for repository cutovers

**Verdict**: YES for any commit that changes a repository's DbContext type OR a DbContext's `OnModelCreating` block. The current pre-push gate (Infrastructure.Tests + ArchTests) is provably insufficient.

**Reasoning**: `Infrastructure.Tests` in this repo exercises `AppDbContext`-backed integration paths with in-memory SQLite. `LankaEventsDbContext` was extracted in 6.5.e; there is no equivalent integration test surface for it. Local Postgres per `CLAUDE.md` is prohibited. That means the ONLY environment where `LankaEventsDbContext` gets exercised end-to-end is staging.

**Process rule** (add to CI + branch protection):

> **Rule 5c — Staging-smoke required for DbContext / repository-cutover commits**: A commit changing (a) a repository's constructor context type, or (b) any DbContext's `OnModelCreating` body, or (c) any `IEntityTypeConfiguration<T>` that declares a `HasOne`/`HasMany`/`HasQueryFilter`/`ApplyConfiguration` — that commit MUST deploy to staging on its own branch and pass `Run-Wave9.ps1` full-suite (≥ pre-commit baseline pass count) BEFORE merge to `develop`. Merges of such commits without a staging-green report attached to the PR are reverted on discovery. This trades 15-25 min of pre-merge latency for the class of regression that just cost 46 API failures and an emergency architect consult.

Concretely: the 6.5.f.5-hotfix PR follows this rule. The Wave6_5TransitionalBaseline.json edit workflow already gates 6.5.f.N; add "staging-smoke report attached" as a second gate. This does not slow founder-pace materially — `Run-Wave9.ps1` is 63 seconds per controller × ~30 controllers ≈ 20 min wall-clock, parallelizable.

### Q3 — Parallel 4-worktree fan-out as "planning change"

**Verdict**: YES — it was a planning change and DID require re-consult per the 2026-07-02 ruling §5 hard-STOP #6 (baseline JSON merge conflicts) implicitly, and per Rule 5 of the Agent Operating Protocol explicitly.

**Reasoning**: The 2026-07-02 ruling §Estimated sessions column called f.1/f.2/f.3/f.5 sequential (each is "one-session" or "half-session" not "parallel-session"). Parallelizing them across 4 worktrees is a materially different execution shape — it changes the merge-order dependency graph, introduces baseline-JSON conflict likelihood (which §Q5 of the 2026-07-02 ruling called "a positive signal" but with the counterpart that you're not supposed to intentionally engineer for it), and — the biggest reason — it eliminates the sequential validation gate where f.1's staging-green would have surfaced the 6.5.e Ignore<> trap BEFORE f.5 amplified it into 46 failures.

**However**: the fan-out itself is not architecturally wrong. It's operationally faster. The ruling would likely have BLESSED the fan-out with one caveat: "each cluster must land AND be staging-verified before the next cluster's PR merges." The bug was self-authorization skipping the re-consult, not the fan-out mechanism.

**Process rule**:

> **Rule 5d — Parallelism changes are planning changes**: Any deviation from the sub-slice estimate column (sequential-vs-parallel, worktree count, cluster reordering) requires architect re-consult. The consult is a single-message question with a single-message answer; skipping it forecloses the architect's opportunity to add pre-flight guards for the specific parallel-execution risk shape (e.g., "each cluster staging-verifies before the next merges").

### Q4 — 6.5.g ordering + cross-module navigation risk

**Verdict**: 6.5.g still sequences BEFORE 6.5.f.4 per the 2026-07-02 ruling — that ordering is UNCHANGED. But 6.5.g does have a cross-module navigation risk of a DIFFERENT shape than 6.5.f: it's not repository-side (Payments has its own DbContext already), it's contracts-side.

**The 6.5.g-specific risk**: 6.5.g's V1 records (`RegistrationConfirmedIntegrationEventV1`, `PaymentCompletedIntegrationEventV1`) will be authored in `Products/LankaEvents/LankaEvents.Contracts` and consumed by `Payments.Application`. If the V1 record fields are anything other than CLR primitives + Contracts-local enums (per 2026-07-02 §Q4), Payments.Application will need to reference LankaEvents.Domain — which reopens Rule 9b.

**Guard** (add to 6.5.g's acceptance criteria before it starts):
- The V1 record shape review: every field is `Guid`, `string`, `decimal`, `DateTimeOffset`, or a Contracts-local `enum`. No `Event`, no `Registration`, no `TicketTier`. If a field's semantic requires structural data (e.g., "list of ticket tiers registered on"), inline it as `IReadOnlyList<TicketTierPaidV1>` where `TicketTierPaidV1` is a Contracts-local record — not the domain type.
- The 6.5.g PR's diff must include a grep-negative-result comment: `grep -r "using LankaConnect.Products.LankaEvents.Domain" src/Modules/Payments/` returns zero hits. That's the specific canary.

6.5.g's risk is NOT the same shape as 6.5.f's Ignore<> trap. Different bug family; the 2026-07-02 §Q4 ruling already covers it.

### Q5 — Was "371/371 Infrastructure.Tests pass" sufficient signal for merging f.5?

**Verdict**: NO. Never was. Should not have been treated as such.

**What WOULD have been sufficient** (retroactive analysis; codify going forward):

1. A model-build parity test asserting `LankaEventsDbContext.Model.FindEntityType(typeof(EventEmailGroupLink))` is non-null and `FindEntityType(typeof(EventBadge))` is non-null. This test is 8 lines. It would have failed the moment 6.5.e's Ignore<> block landed. That it was never authored is the coverage gap — codify below.
2. A "smoke against staging DB clone" step: take a staging DB backup restored into a local Postgres (per §CLAUDE.md this is prohibited for daily work, but for DbContext-shape changes it's the ONLY validation surface). Even a 5-minute manual invocation of `EventRepository.GetByIdAsync` through the DI graph against the real schema would have caught this before merge.
3. Staging-branch deploy + `Run-Wave9.ps1 -Controllers Events` (60 seconds) before PR merge to `develop`. This is the Rule 5c formalization above.

**Process rule** (author permanent test):

> **Rule 5e — DbContext model-build parity test is mandatory permanent coverage**: Any DbContext under `src/Modules/*/*.Infrastructure/` or `src/Products/*/*.Infrastructure/` MUST have a corresponding `<ContextName>ModelParityTests.cs` in `tests/<matching>.Tests/` that asserts every entity the context claims to own (via `DbSet<T>`, via `ApplyConfigurationsFromAssembly`, via explicit `ApplyConfiguration`) resolves to a non-null `IEntityType` in the finalized model. For every `Ignore<T>()` call in `OnModelCreating`, a corresponding assertion `Assert.Null(context.Model.FindEntityType(typeof(T)))` — this documents the Ignore in test form and catches accidental un-Ignore regressions. This is a 20-line-per-context test; write it once per DbContext and it stops the entire class of "assembly-sweep + explicit Ignore collision" bugs at build time.

The 6.5.f.5-hotfix commit includes a scratch version of this test used for validation (per §3 acceptance criterion 4). The PERMANENT version lands in a separate follow-up commit `Wave 6.5.f.7: DbContext model-build parity tests` immediately after the hotfix, targeting `AppDbContext`, `LankaEventsDbContext`, `MediaDbContext`, `NotificationsDbContext`, `FormsDbContext`, `PaymentsDbContext`. Six ~20-line tests. Estimated 0.5 sessions.

---

## 6. Founder-executing-agent process rule (self-authorization discipline)

The founder explicitly requested a memory-updateable process rule to prevent recurrence of "authorized myself to run parallel 4-worktree fan-out" + "merged 6.5.e despite flagging cross-module navigation bleed as a risk in my own review."

**Add to `CLAUDE.md` and `docs/PLATFORM_MASTER_PLAN.md` §3 Agent Operating Protocol as Rule 5f**:

> **Rule 5f — Self-flagged risks are architect-consult triggers**: If the executing agent (human or AI) writes any of the following in a pre-merge review — "cross-module ...", "bleed", "leaking", "unclear ownership", "may need to revisit", "could pull in", "assumes X but haven't verified", "should be fine because Y" — that phrase, in the reviewer's own text, is a hard-STOP trigger. The executing agent may NOT merge the PR. The correct action is to open an architect consult with the flagged sentence quoted verbatim. The architect either resolves the concern (adding acceptance criteria or a scope refinement) or blesses the merge with a written "risk-accepted" ruling that becomes durable audit evidence. The founder's 6.5.e post-merge review flagged "cross-module navigation bleed" — that phrase alone should have blocked the merge. Codify this: **you cannot approve your own risk flags**.

Corollary — **Rule 5g** on parallelism:

> **Rule 5g — Parallelism changes always consult**: Any decision to fan out work across multiple worktrees / concurrent PRs / concurrent sub-slice execution — even if the individual sub-slices are pre-authorized — requires an architect consult on the ORDERING and inter-slice validation gates. One-message question, one-message answer. The 30-second consult cost is one two-hundredth of the regression cost when the parallelism assumption fails.

The two rules together directly address the two specific self-authorizations that produced this regression.

---

## 7. Estimate for hotfix + follow-ups

| Slice | Sessions | What ships |
|---|---|---|
| 6.5.f.5-hotfix | 0.5 | The Option B changes in §2. Includes the scratch parity test used for validation. Merges after §3 acceptance criteria pass. |
| 6.5.f.7 (new) | 0.5 | Permanent `<ContextName>ModelParityTests.cs` for all 6 DbContexts per §5.Q5. |
| 6.5.f.4 | 1.0 | Payment-cluster cutover — UNCHANGED from 2026-07-02 estimate; adds Rule 5c staging-smoke gate. |
| 6.5.f.6 | 0.5 | Dual-mapping cleanup — UNCHANGED; add verification that no explicit `ApplyConfiguration` for `EventEmailGroupLinkConfiguration` / `EventBadgeConfiguration` remains in AppDbContext. |
| 6.5.g / h | UNCHANGED | 2.0 + 2.0 per 2026-07-02 ruling. |

**Net calendar impact**: +1 session (0.5 hotfix + 0.5 permanent parity tests) — under half a working day at founder pace. Well within the 2.5-3 week Wave 6.5 envelope.

---

## 8. Ruling summary

Take Option B. Physically move `EventEmailGroupLinkConfiguration.cs` and `EventBadgeConfiguration.cs` into `Products/LankaEvents/LankaEvents.Infrastructure/Configurations` (they've always belonged there). Delete the `HasOne(eb => eb.Badge)` block inside the moved `EventBadgeConfiguration` — that navigation was the anti-pattern. Un-Ignore `EventEmailGroupLink` and `EventBadge` in `LankaEventsDbContext.OnModelCreating`; keep `EmailGroup` and `Badge` Ignored (foreign principals stay foreign — the correct application of §7.8). Rewrite `EventRepository.GetEventsWithExpiredBadgesAsync` to project scalar `BadgeId` instead of `.ThenInclude(eb => eb.Badge)` — cross-module hydration moves to application-layer join via `IBadgeRepository`. Remove the now-redundant explicit `ApplyConfiguration` lines from `AppDbContext.OnModelCreating` so the runtime assembly sweep is the single source of registration.

Gate the hotfix merge on §3's seven acceptance criteria; the model-build parity assertion (§3.4) is the specific coverage that would have caught 6.5.e's Ignore<> trap and becomes a permanent test in a follow-up 6.5.f.7 commit.

Codify Rules 5b (model-shape consult), 5c (staging-smoke pre-merge for DbContext / repo cutovers), 5d (parallelism consult), 5e (permanent parity tests), 5f (self-flagged risks are consult triggers — the founder cannot approve their own risk flags), 5g (parallelism always consults on ordering + gates). These six rules, taken together, close the class of bug 6.5.e / 6.5.f.5 produced.

Wave 6.5.f.5-hotfix + 6.5.f.7 add +1 session to the wave; f.4, f.6, g, h estimates and sequencing are unchanged. 6.5.g remains sequenced before f.4 per 2026-07-02 §Q4 ordering. Wave 6.5 stays inside the 2.5-3 week calendar envelope.
