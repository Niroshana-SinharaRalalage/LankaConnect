# ADR-004: Feature Flag Strategy (Trunk-Based Development)

| | |
|---|---|
| **Status** | Accepted (2026-04-26 — D4 resolved: Ops.* TTL = 5s) |
| **Date** | 2026-04-26 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | (replaces long-lived branch strategy from v1 plan) |
| **Related** | ADR-006 (Migration strategy — depends on flags for cutover) |

## Context

The architecture review rejected the original plan of running a 14-week long-lived `refactor/modular-monolith-phase-a` branch. Industry experience shows long-lived refactor branches against an actively-developed `develop` lose 25–40% of calendar to merge resolution.

The chosen alternative: **trunk-based development with feature flags + expand/contract migrations**. Each module extraction lands on `develop` with both old and new code paths live, gated by a flag. Cutover is flipping the flag in environment configuration, not merging a giant branch.

This requires a feature flag infrastructure with consistent naming, lifecycle, and governance.

## Options Considered

### Option A: Microsoft.FeatureManagement with structured naming convention (RECOMMENDED)

`Microsoft.FeatureManagement` and `Microsoft.FeatureManagement.AspNetCore` packages. Flags configured via `appsettings.json` per environment, overridable via Azure App Configuration or env vars. Frontend mirrors via API endpoint that exposes flag state for the client bundle.

### Option B: LaunchDarkly or similar SaaS

Hosted feature management with rich UI, A/B testing, percentage rollouts.

### Option C: Custom database-backed flags

Build a custom `feature_flags` table + admin UI.

### Option D: Environment variables only

Use plain env vars in Container Apps revisions to gate features.

## Decision

**Adopt Option A (Microsoft.FeatureManagement)** with the naming convention and lifecycle defined below.

### Naming Convention

Format: `<Category>.<Subject>.<Variant>`

| Category | Purpose | Lifetime | Example |
|---|---|---|---|
| `Refactor.*` | Phase A migration cutover gates | ≤ 4 weeks per flag | `Refactor.Notifications.UseNewModule` |
| `Feature.*` | Long-lived business feature gates | Indefinite | `Feature.Commerce.SeylaEnabled` |
| `Country.*` | Geo gating | Indefinite | `Country.LK.Enabled` |
| `Experiment.*` | A/B tests with explicit expiry | ≤ 8 weeks | `Experiment.Checkout.OneClick` |
| `Ops.*` | Kill-switches for runtime issues | Indefinite | `Ops.Email.RateLimit.Aggressive` |

### Lifecycle Discipline

1. **Every flag has a registry entry** in `docs/feature-flags.md`:
   - Name, category, owner, created date, sunset date (if applicable), description
2. **`Refactor.*` flags MUST have a sunset date ≤ 4 weeks** from creation
3. **CI check** — `tools/check-stale-flags.sh` runs in CI; fails if any `Refactor.*` flag exists past its sunset date
4. **Cleanup PR template** — when flag is removed, the PR must:
   - Delete the flag from `appsettings*.json`
   - Delete the legacy code path it gated
   - Remove the registry entry
   - Be a single PR, not interleaved with feature work

### Phase A Migration Pattern

Per module extraction:

```csharp
// In the controller / orchestration layer:
if (await _featureManager.IsEnabledAsync("Refactor.Notifications.UseNewModule")) {
    return await _newNotificationsModule.Send(request);
} else {
    return await _legacyHandler.Send(request);  // existing code path
}
```

Flag transitions per module:

1. **Week N (PR lands)**: Flag added, defaulted to `false` in all environments. Old code unchanged. New module code present but unreachable.
2. **Week N+1 (staging soak)**: Flag flipped `true` in staging. 7-day soak period. Compare metrics (latency, error rate) old vs new.
3. **Week N+2 (production canary)**: Flag flipped `true` in production for 10% traffic via Azure App Configuration's targeting filter. Monitor 24h.
4. **Week N+2 (production full)**: 50% → 100% over 24h.
5. **Week N+4 (cleanup, sunset reached)**: Single cleanup PR removes flag + legacy code. Registry updated.

### Frontend Flag Distribution

- Backend exposes `GET /api/featureflags` returning user-applicable flag states. **Cache TTL by category**:
  - `Ops.*` flags: ≤5s TTL (kill-switches must propagate fast)
  - `Refactor.*` flags: 60s TTL
  - `Feature.*` and `Experiment.*` flags: 60s TTL
  - `Country.*` flags: 60s TTL
- `@lankaconnect/feature-flags` package provides `useFlag('Feature.Commerce.SeylaEnabled')` hook
- Flags evaluated at request time on backend (truth source); frontend never makes UI decisions on stale state

### Cutover Mechanism Disambiguation (per architect review)

The original "10% canary" wording is ambiguous between two mechanisms with different failure modes:

| Mechanism | Use case | Failure mode |
|---|---|---|
| **Container Apps revision-based traffic split** | Phase A module cutover (W15 production) | Each request routes to one revision; sticky for whole request including auth — safe for payment flows |
| **FeatureManagement percentage filter** | A/B tests within a single deployment | Per-user evaluation; requires sticky targeting context (e.g., user ID) — without sticky context, a single user oscillates between paths within a session, breaking payment flows |

**Decision**:
- **Phase A module cutover**: use **Container Apps revision-based traffic split** (configured in Bicep, executed via `az containerapp revision set-mode` + `az containerapp ingress traffic set`)
- **A/B experiments** (`Experiment.*`): use FeatureManagement percentage filter with mandatory `IUserTargetingContext` providing user ID for stickiness
- **Kill-switches** (`Ops.*`): no canary — flip 0% → 100% atomically

### Flag Evaluation Outage Default

If FeatureManagement cannot reach Azure App Configuration, flag evaluation falls back to:
- `Refactor.*` → defaults closed (legacy code path serves) — fail-safe for refactor cutover
- `Ops.*` → per-flag default declared in registry; choose based on whether the flag is "enable safer behavior" (default open) or "disable risky behavior" (default closed)
- `Feature.*` → defaults to last-cached value; if no cache, defaults closed
- `Experiment.*` → defaults closed (control group)
- `Country.*` → defaults closed (no traffic accepted from country with stale config)

### CI Stale-Flag Gate Behavior Matrix

| Category | CI behavior past sunset | Behavior with no sunset |
|---|---|---|
| `Refactor.*` | **Hard fail** PR | Sunset is mandatory; no-sunset = hard fail |
| `Experiment.*` | **Warn** (experiments may legitimately extend) | Sunset is mandatory; no-sunset = warn |
| `Feature.*` | n/a (no sunset) | Annual owner re-attestation; flag deleted if owner unresponsive 30 days after attestation due |
| `Ops.*` | n/a (no sunset) | Same annual re-attestation as `Feature.*` |
| `Country.*` | n/a (no sunset) | Reviewed at country sunset (if country exits) |

## Consequences

### Positive

- Eliminates rebase hell of long-lived branches
- Real per-module rollback in <30s (config flip, not git revert + redeploy)
- Both code paths verified in production traffic before legacy is deleted
- Each PR remains small (≤ 400 LOC) — reviewable in single sitting
- Trunk-based dev: `develop` always green and deployable
- Sunset discipline prevents flag accumulation (industry anti-pattern)

### Negative / Trade-offs

- Code temporarily contains BOTH old and new paths (~2–4 weeks per module)
- Requires discipline to delete old path after sunset
- Slightly larger code review burden during migration window (review BOTH paths)
- Risk of flag-driven branching becoming permanent if cleanup neglected

### Risks

- **Risk: stale flags accumulate.** Mitigation: CI gate on `Refactor.*` sunset dates; quarterly flag-debt audit.
- **Risk: developer forgets to flag-gate new code.** Mitigation: PR review checklist + ArchTest rule that all controller actions must have explicit flag handling during refactor window.
- **Risk: legacy and new paths diverge in subtle bugs.** Mitigation: integration test suite runs against BOTH paths during the soak period.

## Rejected Alternatives

- **Option B (LaunchDarkly)**: $$$ per seat. Solo founder doesn't need targeting/segmentation features. Revisit if marketing team is hired.
- **Option C (custom)**: Reinventing wheel. `Microsoft.FeatureManagement` is sufficient and free.
- **Option D (env vars only)**: No granularity (no percentage rollouts, no targeting). Container restart required to flip. Insufficient for canary deploys.

## Implementation Checklist (Week 1) — REVISED per architect

- [ ] `dotnet add package Microsoft.FeatureManagement.AspNetCore` in `BuildingBlocks.Web` (W2 actual extraction)
- [ ] `appsettings.json` `FeatureManagement` section with empty initial flag set
- [ ] `appsettings.Staging.json` and `appsettings.Production.json` overrides
- [ ] Azure App Configuration resource provisioned (Bicep)
- [ ] **`tools/check-stale-flags.sh` script created in W1.6** (NOT W12) — manual review until CI gate enabled in W12, but the script must exist throughout migration
- [ ] CI workflow step (enabled in W12): run stale-flag check on every PR with category-specific behavior (per matrix above)
- [ ] `docs/feature-flags.md` registry stub with columns: `name | category | owner | created_date | sunset_date | description | default_on_outage`
- [ ] PR template updated to require flag declaration for any module-cutover code
- [ ] Cache TTL configured per-category in `Program.cs` (per Frontend Flag Distribution section)

## References

- Architect review: 2026-04-26 (Question 6 — Branching strategy flagged ❌)
- Industry: GitHub, Shopify, Etsy patterns on trunk-based + flags
