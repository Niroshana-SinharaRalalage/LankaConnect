# Wave 5.3 — Forms Contracts (IFormQueries) — Implementation Plan

**Status**: PLANNED — architect consult complete 2026-06-11. Ready for next-session implementation. NOT YET STARTED.

**Architect consult**: 2026-06-11 session. Full reasoning captured below.

**Predecessor**: Wave 5.2c (`fa23fbd7`) shipped 2026-06-11 — Form aggregate has OwnerEntityId + OwnerEntityType columns. Wave 5.2d gated by ≥48h soak (independent of 5.3).

**Successor unblock**: Wave 4.1.2 retry (Communications Domain extraction) — same Contracts pattern; Event aggregate then refactors `List<EmailGroup>` → `IReadOnlyList<Guid>` + IEmailGroupQueries.

---

## TL;DR

Wave 5.3 is BIGGER than master TODO line 1382-1384 implies. It splits into **5 atomic commits (5.3a → 5.3e)** because the cross-module surface is wider than a single IFormQueries shim. The realization: most of the Forms-touching code currently in `LankaConnect.Application/Events/` (CreateEventForm command handler, 6 other Form mutator handlers, 4 FormResponse* email handlers, etc.) doesn't conceptually belong in Events — it belongs in Forms.Application. Wave 5.3 is partly a MOVE, not just a Contracts injection. After the move:
- Events handlers that only READ Forms (11 EventHandlers — RegistrationConfirmed, AttendeesAdded, etc.) inject `IFormQueries` from Forms.Contracts.
- ONE cross-module mutator stays: `IFormCommands.DeleteResponsesByEventAndUserAsync` for cancel-RSVP cleanup.
- `LankaConnect.Application.csproj` drops `Forms.Domain` ProjectReference, adds `Forms.Contracts`.
- ArchTest rule pins the cut.

NO DB migrations in any 5.3 sub-phase. Pure CLR refactor. PITR rollback unused.

---

## Sub-phase decomposition

### 5.3a — Define `Forms.Contracts` surface (additive)
12 new files in `src/Modules/Forms/Forms.Contracts/`:
- `IFormQueries.cs`, `IFormCommands.cs`
- DTOs: `FormSummaryDto`, `FormDetailDto`, `FormResponseDetailDto`, `FormQuestionContractDto`, `QuestionOptionContractDto`, `FormAnswerContractDto`
- Contracts enums: `FormStatusDto`, `FormQuestionTypeDto`, `FormOwnerEntityTypeDto` (deliberately duplicated from Domain — Contracts must not pull Forms.Domain)

T-triggers: T1 (new public interfaces). Tests: `Forms.Contracts.Tests/` — interface-shape pinning (1 test). S-class: none.

**ArchTest rule 1 lands HERE** (added to LayeringRules.cs): `Modules_Forms_Contracts_DependsOnlyOnBuildingBlocksContracts`.

### 5.3b — Implement `FormQueries` + `FormCommands` in Forms.Application
- `Forms.Application/Queries/FormQueries.cs` (~120 LOC) — wraps `IFormRepository` + `IFormResponseRepository`
- `Forms.Application/Commands/FormCommands.cs` (~30 LOC) — wraps `IFormResponseRepository.DeleteAsync`
- `Forms.Application/Mappings/FormContractMappings.cs` (~80 LOC) — static `ToContractDto()` extensions
- `Forms.Api/FormsModule.cs` — DI: `services.AddScoped<IFormQueries, FormQueries>()` + same for IFormCommands
- `Forms.Application.csproj` adds `<ProjectReference Forms.Contracts>`

T-triggers: T3 (new handlers) + T6 (DI registration). Tests: `Forms.Application.Tests/Queries/FormQueriesTests.cs` (6 tests). S-class: S1 (read-only GET endpoint shape parity).

### 5.3c — MOVE handlers into Forms.Application (THE BIG COMMIT)
~50 files moved from `src/LankaConnect.Application/Events/` to `src/Modules/Forms/Forms.Application/`:
- Mutator command handlers (13 files): `Create/Update/Delete/Publish/Close/ReopenEventForm` + `Add/Update/Delete/ReorderFormQuestion` + `Submit/Update/DeleteFormResponse`
- Query handlers (7 files): `GetEventForms/GetEventFormDetail/GetFormResponses/GetMyFormResponse/GetMyFormResponseByUserId/GetPublicFormResponses/ExportFormResponses`
- DTOs (`EventFormDtos.cs`)
- FormResponse* event handlers (4 files): `FormResponseSubmittedEmailHandler`, `FormResponseUpdatedEmailHandler`, `FormResponseDeletedEmailHandler`, `FormResponseWhatsAppHandler` — these become Forms-module subscribers consuming in-process MediatR (no new integration events in 5.3)
- Test files (~20) move to `tests/Modules/Forms/Forms.Application.Tests/`

API controllers (under `LankaConnect.API/Controllers/`) DO NOT MOVE — they continue dispatching MediatR `IRequest` interfaces. Only handler namespaces change.

T-triggers: T1 (namespace move = new public types in Forms.Application assembly) + T3 + T7 (existing tests must compile and pass). Evidence: paste `dotnet test --filter FullyQualifiedName~Forms.Application.Tests` output into commit body. S-class: S2 (POST→GET round-trip on form create) + S3 (log silence) + S1 (list query).

**This is the highest-risk commit.** Has rollback escape hatch via `git revert` (no migrations).

**Possible split** if 5.3c is too fat at implementation time: 5.3c.1 (commands) + 5.3c.2 (queries + DTOs) + 5.3c.3 (FormResponse* event handlers). Decide at scratch-worktree build-time.

### 5.3d — Cut `LankaConnect.Application → Forms.Domain` edge
11 EventHandlers in `src/LankaConnect.Application/Events/EventHandlers/`:
- RegistrationConfirmedEventHandler, AnonymousRegistrationConfirmedEventHandler
- AttendeesAddedEventHandler, UserCommittedToSignUpEventHandler, CommitmentUpdatedEventHandler, CommitmentCancelledEmailHandler
- RegistrationCancelledEventHandler, EventPublishedEventHandler
- PaymentCompletedEventHandler, RefundCompletedEventHandler, RefundRequestedEventHandler, RefundRequestCompletedEventHandler

All swap `IFormRepository` injection → `IFormQueries`. `.GetByEventIdAsync(eventId)` becomes `.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, eventId)`. `f.Status == FormStatus.Active` becomes `f.Status == FormStatusDto.Active`.

PLUS: `CancelRsvpCommandHandler.cs` swaps `IFormResponseRepository.DeleteAsync` → `IFormCommands.DeleteResponsesByEventAndUserAsync`.

FINALLY: `LankaConnect.Application.csproj`:
- REMOVE `<ProjectReference Include="...\Forms.Domain.csproj" />`
- ADD `<ProjectReference Include="...\Forms.Contracts.csproj" />`

T-triggers: T3 (constructor DI shape changes). Tests: 11 EventHandler unit tests + CancelRsvp test all get Moq `IFormRepository` → `IFormQueries`. S-class: S2 (RSVP cancel flow with form-response cleanup) + S1 (RegistrationConfirmed email body must still mention form).

### 5.3e — ArchTest enforcement
2 rules in `tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs`:
1. `Modules_Forms_Contracts_DependsOnlyOnBuildingBlocksContracts` (already added in 5.3a)
2. `LegacyApplication_DoesNotDependOnFormsDomain` — the master-TODO-mandated rule. Must land LAST because it red-lights any earlier state.

T-triggers: T6. Tests: the 2 new ArchTest facts ARE the tests. S-class: none.

---

## ProjectReference graph (final state)

```
Forms.Contracts.csproj
  → BuildingBlocks.Contracts                              (existing)

Forms.Application.csproj
  → Forms.Contracts                                       (NEW edge — 5.3b)
  → Forms.Domain                                          (existing)
  → BuildingBlocks.Application                            (existing)

Forms.Infrastructure.csproj
  → Forms.Application                                     (existing)
  → Forms.Domain                                          (existing)

LankaConnect.Application.csproj
  → Forms.Contracts                                       (NEW edge — 5.3d)
  → Forms.Domain                                          REMOVED — 5.3d (the cut)
```

**No circular ref.** Today `Forms.Application` has zero ProjectReference to `LankaConnect.Application`. The new edge is `LankaConnect.Application → Forms.Contracts` (a Contracts leaf).

---

## Contract surface (concrete)

```csharp
public interface IFormQueries
{
    Task<IReadOnlyList<FormSummaryDto>> GetByOwnerAsync(
        FormOwnerEntityTypeDto ownerType, Guid ownerId, CancellationToken ct = default);
    Task<FormSummaryDto?> GetByIdAsync(Guid formId, CancellationToken ct = default);
    Task<FormDetailDto?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken ct = default);
    Task<FormResponseDetailDto?> GetResponseWithAnswersAsync(Guid responseId, CancellationToken ct = default);
}

public interface IFormCommands
{
    Task<int> DeleteResponsesByEventAndUserAsync(
        Guid eventId, Guid userId, CancellationToken ct = default);
}
```

**FormSummaryDto fields**: `Id, OwnerEntityType, OwnerEntityId, Title, Description?, Status, AllowMultipleResponses, ResponseDeadline?, MaxResponses?, HasResponses, QuestionCount, ResponseCount, CreatedAt, UpdatedAt?, AllowAttendeesToViewResponses`.

**FormDetailDto** = FormSummaryDto + `IReadOnlyList<FormQuestionContractDto> Questions`.

---

## Risks

1. **5.3c fat commit (~50 files moved + tests re-pointed)**. Mitigation: scratch worktree per-folder rebase + incremental smoke; if too big, split 5.3c.1/.2/.3.

2. **API controller verification needed** before implementation: grep `src/LankaConnect.API/Controllers/` for `CreateEventFormCommand` / `UpdateEventFormCommand` etc. If controllers reference Command CLASSES (not just `IRequest<>`), 5.3c's file list grows.

3. **`EventFormDtos.cs` Phase 6A.146 PII redaction**: public-DTO surface (`PublicFormResponseDto`) has reflection-asserted PII-redaction tests. Verify those tests still pass post-move.

4. **5.3d two-DbContext atomicity lost**: `CancelRsvpCommandHandler` today commits AppDbContext + FormsDbContext via single `IUnitOfWork.CommitAsync` (if FormsDbContext is wired into that UnitOfWork — verify). After 5.3d, `FormCommands.DeleteResponsesByEventAndUserAsync` self-saves on FormsDbContext. Two independent transactions. **Architecturally correct per ADR-010** but means partial-failure semantics. Mitigation: surface as `Result.Warning` to existing `warnings` collection in CancelRsvpCommandHandler.cs:196.

5. **5.3c dead usings in CancelRsvpCommand.cs** — the 5 `using LankaConnect.Modules.Forms.Domain.*` lines exist because of the Wave 5.2a sed batch. After 5.3d, IFormCommands replaces the dead imports. Don't forget to clean them up (5.3e ArchTest Rule 2 would otherwise emit a confusing error).

---

## Wave 4.1.2 applicability (the unblock path)

After Wave 5.3 lands, retry Wave 4.1.2:

1. **`Communications.Contracts/IEmailGroupQueries.cs`** — `GetByEventAsync(eventId, ct) → IReadOnlyList<EmailGroupSummaryDto>`.
2. **`Communications.Contracts/ICommunicationsCommands.cs`** — `AddEmailGroupToEventAsync(eventId, groupId)` / `RemoveEmailGroupFromEventAsync` for Event-side M2M mutations.
3. **Event aggregate refactor**: drop `private readonly List<EmailGroup> _emailGroupEntities`; replace with `private readonly List<Guid> _emailGroupIds`. EF maps M2M junction `event_email_groups` as owned Guid collection on Event side. EmailGroup side keeps its own aggregate.
4. **Junction table location**: stays where it is today (legacy or events schema). Relocate in a future Wave 4.9.X-style schema move. EF config for junction lives on AppDbContext side (Event owner) and reads `event_email_groups` cross-schema.
5. **ArchTest rule**: "LankaConnect.Domain.Events must not reference Communications.Domain" replaces today's shim.

The Wave 5.3 rehearsal de-risks step 3 — if Form had a typed nav like Event-to-EmailGroup, 5.3 would have hit the same Wave 4.1.2 wall. It doesn't, which is why 5.3 works.

---

## Implementation checklist (next-session resumption)

- [ ] **5.3a**: define Forms.Contracts surface (12 files) + Contracts.Tests + ArchTest Rule 1
- [ ] **5.3b**: implement FormQueries + FormCommands in Forms.Application + DI + 6 query tests
- [ ] **5.3c**: move ~50 handler files into Forms.Application (consider split if too fat)
- [ ] **5.3d**: swap 11 EventHandlers to IFormQueries + CancelRsvp to IFormCommands + cut Forms.Domain edge
- [ ] **5.3e**: ArchTest Rule 2 (LegacyApplication_DoesNotDependOnFormsDomain)

**Pre-flight before 5.3a**: grep `src/LankaConnect.API/Controllers/` for direct Command-class references. Add to 5.3c file list if found.
