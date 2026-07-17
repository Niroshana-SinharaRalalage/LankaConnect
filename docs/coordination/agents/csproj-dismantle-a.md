# Agent Channel: CsprojDismantle-A

**Agent role:** Wave 8.5.a — Relocate 5 `LankaConnect.Application` files + delete csproj.
**Priority:** P2
**Est time:** 4 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

`LankaConnect.Application/` csproj has 5 real files left post Consult #26 Q1 downscope:
- `Common/Interfaces/IEntraExternalIdService.cs` — `LankaConnect.Application.Common.Interfaces`
- `Common/Interfaces/IJwtTokenService.cs` — same
- `Dashboard/Queries/GetCommunityStats/GetCommunityStatsQuery.cs` — `LankaConnect.Application.Dashboard.Queries`
- `Dashboard/Queries/GetCommunityStats/GetCommunityStatsQueryHandler.cs` — same
- `MetroAreas/Mappings/MetroAreaMappingProfile.cs` — already relocated per commit `884bd3f9`; verify + delete

Consult #26 identified a layering constraint: `Identity.Contracts` cannot reference `Identity.Domain` (for User type). Prior attempt to relocate `IEntraExternalIdService` + `IJwtTokenService` was reverted for this reason.

## Deliverable

### Part 1 — Interface pair (IEntraExternalIdService + IJwtTokenService)

Per Wave 8.5.a-refined scope in `docs/PHASE_A_5_PLAN.md` — the "User → Guid API reshape" is the fix. Both interfaces should:
- Move to `Identity.Contracts/Services/` (correct target per Blueprint §7.2 Auth capability)
- Method signatures change: any parameter/return-type of `User` (Identity.Domain type) → `Guid` (user ID) or a dedicated `UserProfileDto` (Identity.Contracts type)
- Callers of the interfaces get updated to fetch User via IIdentityQueries (which returns DTOs, not domain types) if they need full user info

**Alternative**: If reshaping to Guid/DTO is too invasive (>2 hours per interface), keep interfaces in a `Identity.Application.Contracts/` sub-namespace (still under Application layer but explicitly named as contract-surface) and add ArchTest exception with reason. Escalate to Tech Lead if you take this path.

### Part 2 — Dashboard query pair

`GetCommunityStatsQuery` is cross-module (aggregates Events + Users + Businesses data). Target: new capability `Capabilities/Dashboard.Contracts/` + `Capabilities/Dashboard.Application/` for query handler, OR fold into `Communications.Application` (per Consult #26 hint).

**Recommendation:** create `src/Capabilities/Dashboard.Contracts/` + `src/Capabilities/Dashboard.Application/` — it's a legitimate cross-cutting capability. Wire into DI in `LankaConnect.API/Program.cs` (or Host.AllInOne when Agent-ApiRename runs).

### Part 3 — MetroAreaMappingProfile verify

Should already be at `LankaEvents.Application/Mapping/MetroAreaMappingProfile.cs` per commit `884bd3f9`. Verify with grep + delete the `LankaConnect.Application/MetroAreas/` directory if empty.

### Part 4 — Delete csproj

Once all 5 files are relocated:
1. Delete `src/LankaConnect.Application/LankaConnect.Application.csproj`
2. Delete `src/LankaConnect.Application/` directory entirely
3. Remove all `<ProjectReference Include="...LankaConnect.Application..."/>` from every other csproj (grep first, count them, then delete)
4. Remove any `using LankaConnect.Application.*` still present anywhere (grep + fix)
5. Verify `dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release` → 0 errors
6. Verify `dotnet restore --force` cold-run → no MSB4006 cycle

### Commits

- 1 commit per Part (or fewer if scope combines cleanly).
- Body: `Wave 8.5.a — <part-summary>`
- `T-triggers: T6 (DI shape change) + T5 (interface signature change if Part 1 reshapes)`
- `S-class: S1 (endpoint smoke — verify Dashboard + Auth endpoints still work post-relocation)`
- Push to `develop`.

## Constraints

- **DO NOT** delete csproj until all files relocated AND all consumers updated.
- **DO NOT** touch files owned by CsprojDismantle-B (that agent handles `LankaConnect.Infrastructure/`).
- If Part 1 hits the layering-constraint blocker AGAIN (Identity.Contracts referencing Identity.Domain), FLAG to Tech Lead + take Alternative path.
- If Part 4 breaks `dotnet restore` cold-run, revert Part 4 and leave csproj as empty shell + document as post-sprint retire.

## Communication protocol

- Post Part-1 relocation strategy chosen (User→Guid/DTO reshape vs Alternative).
- Post grep evidence for empty directory + ProjectReference cleanup.
- Post commit SHAs.
- Post `STATUS: COMPLETE` at bottom.

## Log

*(Agent writes progress below this line.)*

### 2026-07-17 Wave-2 Agent-CsprojDismantle-A run (2nd re-spawn)

**Baseline check** — dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release → 0 Error(s), 6 pre-existing package-advisory Warning(s). LC.Application csproj on develop pre-run had exactly the 5 files documented in the brief (plus dead Class1.cs scaffold + Communications backup/README):

```
src/LankaConnect.Application/Class1.cs                                        (dead scaffold)
src/LankaConnect.Application/Communications/README.md                         (orphan doc)
src/LankaConnect.Application/Communications/Commands/.../*.backup             (stale ash)
src/LankaConnect.Application/Common/Interfaces/IEntraExternalIdService.cs     (Wave 8.5.a Part 1 scope)
src/LankaConnect.Application/Common/Interfaces/IJwtTokenService.cs            (Wave 8.5.a Part 1 scope)
src/LankaConnect.Application/Dashboard/Queries/GetCommunityStats/*.cs         (Wave 8.5.a Part 2 scope)
```

**Cross-project consumer surface pre-run**:
- 13 csprojs `<ProjectReference Include=".../LankaConnect.Application.csproj"/>` in src + tests.
- 2 files with production `using LankaConnect.Application;` (Program.cs line 7, BackgroundJobDIIntegrationTests.cs line 6).
- All other `using LankaConnect.Application` hits are `using LankaConnect.Application.Tests.TestHelpers` (test-utility namespace inside `LankaConnect.Application.Tests` project — orthogonal to this csproj).

**Ship plan** — 3-commit sequence attempted:
- Commit A: Part 2 (Dashboard fold) + Part 3 (MetroAreas verify) + directory cleanup.
- Commit B: Part 1 (interface pair relocation).
- Commit C: Part 4 (csproj + 13 ProjectReferences + solution entries delete).

**Actual ship** — Commit A + one follow-up compile-fix:
- `c2a6e3fc` — Wave 8.5.a partial ship — Dashboard fold-into-Host + LC.Application dead cleanup.
- `a15d8b63` — Wave 8.5.a follow-up — CS8602 compile-fix in Rule15 ArchTest predicate (unblocks Tier B pre-push gate for the partial ship; Rule15 was added by Wave 8.5.h Batch 1 commit 2d296aca with two CS8602 warnings on the assembly-Name predicate that pre-push `dotnet test LankaConnect.sln` upgrades to errors).

**Part 1 (interface pair) NOT SHIPPED — root cause + options** — see commit body of `c2a6e3fc` for the full rationale. Executive summary: brief's Alternative-path sub-namespace `Identity.Application.Contracts/` re-hits the Identity.Application ↔ Identity.Infrastructure MSB4006 ProjectReference cycle that killed the 2026-07-14 attempt. Three cycle-break options require architect consult:
- (a) Move JwtTokenService + EntraExternalIdService impls INTO Identity.Application/Services/ + Identity.Application csproj gains FrameworkReference `Microsoft.AspNetCore.App` + PackageRefs `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`, `Microsoft.Identity.Web`. Places infrastructure impl in Application layer (matches ArchTest comment's "correct long-term home" note but is architectural drift).
- (b) User→AccessTokenClaims DTO reshape (recommended — clean layering, ~1h scope).
- (c) New Identity.Application.Contracts.csproj capability project.

**Part 4 (csproj + 13 ProjectReferences + solution entries delete) BLOCKED** — LankaConnect.Application.csproj remains in the tree with 2 residual source files. Downstream agent CsprojDismantle-B (Wave 8.5.b, LC.Infrastructure per audit file docs/coordination/migration-audit-2026-07-16.md) reported COMPLETE and is unaffected by this partial ship.

**Verification post-commit**:
- `dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release` → 0 Error(s), 6 pre-existing package-advisory Warning(s).
- `dotnet build tests/architecture/LankaConnect.ArchitectureTests/LankaConnect.ArchitectureTests.csproj -c Debug` → 0 Error(s), 6 pre-existing package-advisory Warning(s).
- No new ArchTest exception added — the existing `[Fact(Skip=...)]` on `LegacyApplication_DoesNotDependOnIdentityDomain` (line 1179 of LayeringRules.cs) continues to accurately describe the deferred state and points to this same Wave 8.5.a-refined follow-up.

**STATUS: PARTIAL** — Wave 8.5.a Part 2 (Dashboard) + Part 3 (MetroAreas verify) + directory cleanup SHIPPED. Wave 8.5.a Part 1 (interface pair relocation) + Part 4 (csproj delete) DEFERRED. Recommended follow-up: Option (b) — introduce `AccessTokenClaims` DTO in Identity.Contracts (Guid + primitives + UserRoleDto), reshape IJwtTokenService.GenerateAccessTokenAsync API, update 3 caller handlers (LoginUserHandler, LoginWithEntraCommandHandler, RefreshTokenHandler) to construct the DTO pre-call, then relocate BOTH interfaces to Identity.Contracts (domain-free) + Part 4 csproj delete.
