# Hotfix Channel — IEventRecommendationEngine post-GAP-1-Part-A residual

**Agent:** hotfix-event-recommendation
**Reports to:** Tech Lead
**Opened:** 2026-07-19 (re-spawn after session-limit killed prior invocation with 0 commits)
**Head at start:** `717e259e`
**STATUS: COMPLETE**

## Problem

`deploy-staging.yml` RED since 2026-07-18 with 6 x CS0246 errors on
`src/Products/LankaEvents/LankaEvents.Domain/Services/IEventRecommendationEngine.cs`
(lines 191, 193, 194, 195, 196, 197) referencing types
`DiasporaFriendliness`, `FestivalPeriod`, `EventNature`, `SignificantDate`,
`CalendarValidationResult` that were retired from LankaEvents.Domain in
Wave 8.5 GAP-1 Part A commit `302af044`.

Root cause: the LOCAL `ICulturalCalendar` interface declaration inside
`IEventRecommendationEngine.cs` (a "supporting interface" block starting
line 184) was not removed when GAP-1 Part A retired the LankaEvents-side
`ICulturalCalendar` in favor of the promoted
`LankaConnect.Modules.CulturalIntelligence.Contracts.Services.ICulturalCalendar`.
The block still referenced the deleted supporting VOs.

## Fix

`src/Products/LankaEvents/LankaEvents.Domain/Services/IEventRecommendationEngine.cs`
- Added `using LankaConnect.Modules.CulturalIntelligence.Contracts.Services;`
- Deleted the stale local `ICulturalCalendar` interface block (12 lines).
  Consumers (`GetEventRecommendationsQueryHandler`, `EventRecommendationEngine`)
  already resolve `ICulturalCalendar` via the Contracts-namespace using
  directive that was landed with `302af044` / `4bef04cf`.

`src/Products/LankaEvents/LankaEvents.Domain/ValueObjects/ContactInfoPrimitives.cs`
- Deleted the stale local `CulturalAppropriateness` sealed class.
  The promoted `CulturalAppropriateness` record in
  `Modules/CulturalIntelligence/CulturalIntelligence.Contracts/Services/CulturalCalendarTypes.cs`
  is the single source of truth. Leaving the local class alongside the
  Contracts-namespace using directive would introduce an ambiguous-type
  compile error at any callsite using an unqualified reference.
- Comment updated to point at the Contracts location.

## Verify

- `dotnet build src/Products/LankaEvents/LankaEvents.Domain/LankaEvents.Domain.csproj -c Release --no-restore`
  -> **0 Warning(s), 0 Error(s)** in 36.58 sec.
- `dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release --no-restore`
  -> **5 Warning(s) (unrelated NuGet vuln advisories), 0 Error(s)** in 48:56.

## Commit

- SHA: `<filled in post-push>`
- deploy-staging.yml run: `<filled in post-push>`

## T-triggers / S-class

- **T-triggers:** T7 (namespace / interface relocation — no new tests; existing
  tests must compile + pass; production-code compile is the observable evidence).
- **S-class:** N/A. Pure compile-time contract-relocation. No runtime surface
  driven by these types is added or changed by this hotfix. The observable
  evidence is deploy-staging.yml going GREEN (i.e. the exact regression this
  fix targets is reversed).
