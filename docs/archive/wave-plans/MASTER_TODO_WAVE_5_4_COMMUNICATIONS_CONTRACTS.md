# Wave 5.4 — Communications.Contracts (IEmailGroupQueries + ICommunicationsCommands) — Implementation Plan

**Status**: PLANNED — architect consult complete 2026-06-13. Successor to Wave 5.3 (Forms.Contracts cut, shipped + STAGING-VERIFIED 2026-06-13).

**Architect consult**: 2026-06-13. Full ruling captured below + in commit history of master-TODO planning doc.

**Predecessor**: Wave 5.3 cut `LankaConnect.Application -> Forms.Domain` (ArchTest Rule 2 `LegacyApplication_DoesNotDependOnFormsDomain` enforces). Wave 5.3d.3 `9218febe`. Wave 5.4 mirrors the same Contracts pattern for the Communications module — but adds a critical pre-cursor phase (5.4.c.0) because the prior Wave 4.1.2 attempt (2026-06-10) was reverted by the EF Core 8 typed-nav blocker on `Event.cs`.

**Memory pin**: `feedback_module_extraction_cross_aggregate_nav.md`. Event.cs holds `List<EmailGroup>` typed M2M nav. EF Core 8 does NOT support pure shadow collection navs without a CLR property — verified via runtime InvalidOperationException. 5.4.c.0 is the load-bearing fix the prior attempt got wrong.

---

## TL;DR

Wave 5.4 splits into **9 atomic commits (5.4.a -> 5.4.d.3)**. Same shape as Wave 5.3 with one inserted phase (5.4.c.0) that surgically rebuilds the Event -> EmailGroup junction using an explicit CLR link type instead of the typed M2M nav. Same physical junction table (`event_email_groups`), same columns, EF-snapshot-only migration. After the surgery, the rest is a 1:1 port of Wave 5.3.

Final state:
- `EmailGroup` lives in `Communications.Domain` (moved from `LankaConnect.Domain.Communications`).
- `LankaConnect.Application` consumes `IEmailGroupQueries.GetByIdsAsync` + `ICommunicationsCommands.*` (no Communications.Domain edge).
- `LankaConnect.Domain` references only the local `EventEmailGroupLink` (no Communications.Domain edge).
- ArchTest Rule 3 `LegacyApplication_DoesNotDependOnCommunicationsDomain` + Rule 4 `LegacyDomain_DoesNotDependOnCommunicationsDomain` pin both edges.
- 1 EF Core migration (junction-table EF-snapshot rebaseline, empty `Up()`/`Down()`).

---

## Sub-phase decomposition

### 5.4.a — Define `Communications.Contracts` surface (additive)

New files in `src/Modules/Communications/Communications.Contracts/`:
- `IEmailGroupQueries.cs`, `ICommunicationsCommands.cs`
- DTOs: `EmailGroupSummaryDto`, `EmailGroupDetailDto` (with member ids), command-result DTOs
- Enums: `EmailGroupStatusDto`, `EmailGroupOwnerKindDto` (deliberately duplicated from Domain — Contracts must not pull Communications.Domain per the matching Forms.Contracts pattern)

T-triggers: T1 (new public interfaces). Tests: `Communications.Contracts.Tests/` — interface-shape pinning (~1 test).

**ArchTest rule lands HERE** (added to LayeringRules.cs): `Modules_Communications_Contracts_DependsOnlyOnBuildingBlocksContracts`.

### 5.4.b — Implement `EmailGroupQueries` + `CommunicationsCommands` in Communications.Application

- `Communications.Application/Queries/EmailGroupQueries.cs` — wraps the legacy `IEmailGroupRepository` (transitional; delegates via DI seam until 5.4.d.2)
- `Communications.Application/Commands/CommunicationsCommands.cs` — mutator surface for cross-module consumers
- `Communications.Application/Mappings/EmailGroupContractMappings.cs` — `ToContractDto()` extensions
- `Communications.Api/CommunicationsModule.cs` — DI: `services.AddScoped<IEmailGroupQueries, EmailGroupQueries>()` etc.
- `Communications.Application.csproj` adds `<ProjectReference Communications.Contracts>` + transitional edge to `LankaConnect.Application` (mirrors Forms.Application 5.3c.0)

T-triggers: T3 (new handlers) + T6 (DI registration). Tests: `Communications.Application.Tests/Queries/EmailGroupQueriesTests.cs` (~6 tests).

### 5.4.c.0 — Event.cs typed-nav surgery (THE CRITICAL FIX)

**Must land BEFORE handler moves. Non-negotiable ordering per architect ruling.**

1. New CLR type `src/LankaConnect.Domain/Events/Entities/EventEmailGroupLink.cs`:
   ```csharp
   public class EventEmailGroupLink {
       public Guid EventId { get; private set; }
       public Guid EmailGroupId { get; private set; }  // raw Guid - no nav to EmailGroup
   }
   ```
2. Replace `Event.cs:33` `private readonly List<EmailGroup> _emailGroupEntities` with `private readonly List<EventEmailGroupLink> _emailGroupLinks`.
3. Update `Event.cs` sync logic to map `_emailGroupIds <-> _emailGroupLinks` (no more EmailGroup type reference).
4. Update `EventConfiguration.cs:515-520`:
   ```csharp
   builder.HasMany<EventEmailGroupLink>("_emailGroupLinks")
          .WithOne()
          .HasForeignKey(l => l.EventId)
          .OnDelete(DeleteBehavior.Cascade);
   ```
   And junction-row entity config for `EventEmailGroupLink` -> table `event_email_groups` (same name as today's M2M junction).
5. EF Core migration `RebaseEventEmailGroupJunction`. **CRITICAL**: EF will try to generate `DropTable`/`CreateTable`. Use the empty-Up()/Down() rebaseline pattern per memory pin `feedback_empty_up_snapshot_rebaseline.md`. Keep EF-regenerated snapshot.

T-triggers: T1 (new entity) + T4 (EF Core config change) + T8 (migration).

Tests:
- `LankaConnect.Domain.Tests/Events/Entities/EventEmailGroupLinkTests.cs` (~3 tests pinning the type)
- `LankaConnect.Domain.Tests/Events/EventTests.cs` — add tests asserting `_emailGroupIds` and `_emailGroupLinks` stay in sync after AddEmailGroup/RemoveEmailGroup/ReplaceEmailGroups/ClearEmailGroups.

S-class: S3 (mapping change) + S5 (schema probe).
Smoke:
- `\d events.event_email_groups` pre + post — assert SAME column shape (`event_id uuid`, `email_group_id uuid`, composite PK).
- POST /api/EmailGroups (create) -> POST /api/Events/{id}/email-groups/{groupId} (link) -> GET event detail -> assert email group reflected.
- Cancel test: link 2 groups, remove 1, assert correct one remains.

### 5.4.c.1 — Move 5 command handlers into Communications.Application

Files moved from `src/LankaConnect.Application/Communications/Commands/` to `src/Modules/Communications/Communications.Application/Commands/`:
- CreateEmailGroup, UpdateEmailGroup, DeleteEmailGroup
- CreateNewsletter (touches EmailGroup AND Newsletter — verify Newsletter aggregate is also moved or split per architect Risk 2)
- UpdateNewsletter (ditto)

API controllers DO NOT MOVE. T-triggers: T1+T3+T7. S-class: S2 (POST -> GET round-trip on EmailGroup create).

### 5.4.c.2 — Move 4 query handlers into Communications.Application

Files: GetEmailGroupById, GetEmailGroups, GetNewsletterById, GetNewslettersByCreator. Includes `EmailGroupDto.cs` move (mirrors `EventFormDtos.cs` treatment in 5.3c.2). T-triggers: T1+T3+T7. S-class: S1 (read-only GET shape parity).

### 5.4.c.3 — Gap verification

Grep `src/LankaConnect.Application/Events/EventHandlers/` for any handler that imports `LankaConnect.Domain.Communications.Entities.EmailGroup` or `IEmailGroupRepository`. Equivalent to Forms 5.3c.3 (FormResponse*Handlers).

If zero found: document in commit message as "no cross-module event handlers consume EmailGroup".
If non-zero: split into 5.4.c.3a (move handlers) + 5.4.c.3b (DI rewire) per Forms precedent.

### 5.4.d.1 — Swap read-side cross-module consumers in LankaConnect.Application

Replace direct `IEmailGroupRepository` injection with `IEmailGroupQueries` in consumers that only READ EmailGroup. List determined by `grep IEmailGroupRepository src/LankaConnect.Application` at execution time (parallel to 12-handler list in 5.3d.1).

T-triggers: T3. S-class: S1 (read-only routing change).

### 5.4.d.2 — Swap mutator consumers + physically move EmailGroup files

Two atomic moves combined per architect ruling:

**Move 1 — physical relocation** (`git mv` for blame preservation):
- `src/LankaConnect.Domain/Communications/Entities/EmailGroup.cs` -> `src/Modules/Communications/Communications.Domain/Entities/EmailGroup.cs`
- `src/LankaConnect.Domain/Communications/IEmailGroupRepository.cs` -> `src/Modules/Communications/Communications.Domain/Repositories/IEmailGroupRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/EmailGroupRepository.cs` -> `src/Modules/Communications/Communications.Infrastructure/Repositories/EmailGroupRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Configurations/EmailGroupConfiguration.cs` -> `src/Modules/Communications/Communications.Infrastructure/Configurations/EmailGroupConfiguration.cs`
- `src/LankaConnect.API/Controllers/EmailGroupsController.cs` -> `src/Modules/Communications/Communications.Api/Controllers/EmailGroupsController.cs` (verify Api layer hosts controllers; may stay in legacy API per Forms precedent)
- `src/LankaConnect.Application/Communications/Common/EmailGroupDto.cs` -> (already moved in 5.4.c.2 if architect treats DTOs as Application-layer concern)

**Move 2 — namespace patches**: bulk sed `LankaConnect.Domain.Communications` -> `LankaConnect.Modules.Communications.Domain` etc.

**Move 3 — `CommunicationsModule.cs` DI**: register the moved `IEmailGroupRepository -> EmailGroupRepository`, swap `EmailGroupQueries` to inject the moved repo directly (no transitional shim).

**Move 4 — CancelRsvp / equivalent mutator path swap**: if any LankaConnect.Application command path mutates EmailGroup (newsletter creation likely does), swap to `ICommunicationsCommands` per the 5.3d.2 CancelRsvp pattern.

T-triggers: T1+T3+T6+T7. S-class: S2 (mutator round-trip) + S3 (log silence) + S1 (list query).

### 5.4.d.3 — Cut edges + ArchTest Rules 3 + 4

1. REMOVE `<ProjectReference Include="...\Communications.Domain.csproj" />` (or the legacy `LankaConnect.Domain.Communications` import root if it exists) from `LankaConnect.Application.csproj` and any other consumers.
2. ADD direct `<ProjectReference Communications.Domain>` to `LankaConnect.Infrastructure` if needed (mirrors 5.3d.3 Infrastructure handling for Forms.Domain).
3. ADD ArchTest Rule 3 `LegacyApplication_DoesNotDependOnCommunicationsDomain` in `LayeringRules.cs`.
4. ADD ArchTest Rule 4 `LegacyDomain_DoesNotDependOnCommunicationsDomain` — this rule is unique to 5.4 (5.3 didn't need it because Forms didn't have a cross-aggregate nav from LankaConnect.Domain).

T-triggers: T6. Tests: the 2 new ArchTest facts ARE the tests. S-class: none.

---

## ProjectReference graph (final state)

```
Communications.Contracts.csproj
  -> BuildingBlocks.Contracts                              (existing)

Communications.Application.csproj
  -> Communications.Contracts                              (NEW edge - 5.4.b)
  -> Communications.Domain                                 (NEW edge - 5.4.b)
  -> BuildingBlocks.Application                            (existing)
  -> LankaConnect.Application                              (transitional, 5.4.b)

Communications.Infrastructure.csproj
  -> Communications.Application                            (NEW edge - 5.4.d.2)
  -> Communications.Domain                                 (NEW edge - 5.4.d.2)

LankaConnect.Application.csproj
  -> Communications.Contracts                              (NEW edge - 5.4.d.1)
  -> LankaConnect.Domain.Communications                    REMOVED - 5.4.d.3 (the cut)

LankaConnect.Domain.csproj
  -> Communications.Domain                                 REMOVED - 5.4.d.3 (the second cut)
  -> (Event.EventEmailGroupLink is local; no cross-module ref)

LankaConnect.Infrastructure.csproj
  -> Communications.Domain                                 (NEW direct edge - 5.4.d.3, mirror of 5.3d.3 Forms.Domain handling)
```

---

## Contract surface (concrete, per architect ruling)

```csharp
public interface IEmailGroupQueries
{
    Task<IReadOnlyList<EmailGroupSummaryDto>> GetByIdsAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<EmailGroupSummaryDto?> GetByIdAsync(
        Guid id, CancellationToken ct = default);
    Task<EmailGroupDetailDto?> GetByIdWithMembersAsync(
        Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmailGroupSummaryDto>> GetByOwnerAsync(
        Guid ownerId, CancellationToken ct = default);
}

public interface ICommunicationsCommands
{
    // Cross-module mutator surface - shape TBD at 5.4.b based on real consumers
    // discovered in 5.4.d.1 audit.
}
```

**No `GetByEventAsync`** — architect explicit ruling. Event aggregate owns its `_emailGroupIds`; the cross-aggregate fetch goes `event.EmailGroupIds -> IEmailGroupQueries.GetByIdsAsync(ids)` (preserves Phase 6A.32 N+1 batch-fetch pattern + respects module boundary).

---

## Risks (architect-flagged)

1. **5.4.c.0 migration trap** — EF will generate `DropTable`/`CreateTable` for `event_email_groups`. Apply the empty-Up() rebaseline pattern per memory pin `feedback_empty_up_snapshot_rebaseline.md`. Probe with `\d events.event_email_groups` pre + post.
2. **Newsletter handlers** — `CreateNewsletter` and `UpdateNewsletter` touch BOTH EmailGroup AND Newsletter aggregates. Verify Newsletter is also moving to Communications before the handler move, or split the handler per Forms 5.3c precedent.
3. **`SyncEmailGroupIdsFromEntities` back-door** in Event.cs:2209 — must delete in 5.4.c.0 once the junction CLR type lands; otherwise it leaks the old shape into the new design.
4. **ArchTest blind spot** — 5.3 only added the Application-layer ArchTest. 5.4 MUST add Rule 4 (LegacyDomain -> CommunicationsDomain) too; otherwise a future `using LankaConnect.Modules.Communications.Domain` in LankaConnect.Domain.Events regresses silently.
5. **5.4.c.0 ordering non-negotiable** — must land BEFORE 5.4.c.1/.c.2. Handler moves before nav surgery will fight the typed-nav blocker that broke 4.1.2.

---

## Implementation checklist (next-session resumption)

- [ ] **5.4.a**: define Communications.Contracts surface + Contracts.Tests + ArchTest rule
- [ ] **5.4.b**: implement EmailGroupQueries + CommunicationsCommands in Communications.Application + DI + ~6 query tests
- [ ] **5.4.c.0**: Event.cs typed-nav surgery + EventEmailGroupLink + EF mapping flip + EF-snapshot rebaseline migration + unit tests + S3+S5 smoke
- [ ] **5.4.c.1**: move 5 command handlers into Communications.Application
- [ ] **5.4.c.2**: move 4 query handlers + EmailGroupDto into Communications.Application
- [ ] **5.4.c.3**: gap-verification grep, document or split
- [ ] **5.4.d.1**: swap read-side cross-module consumers to IEmailGroupQueries
- [ ] **5.4.d.2**: physical move of EmailGroup files + namespace patch + Communications DI wire-up + mutator swap
- [ ] **5.4.d.3**: cut Application + Domain edges + ArchTest Rule 3 + Rule 4

**Pre-flight before 5.4.a**: grep `src/LankaConnect.Application/Events/EventHandlers/` for `EmailGroup\b` to identify cross-module event-handler scope (deferred to 5.4.c.3 audit).
