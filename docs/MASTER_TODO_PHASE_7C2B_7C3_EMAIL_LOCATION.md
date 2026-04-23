# Master TODO — Phase 7C.2b + 7C.3 Email Location Decomposition

**Created**: 2026-04-22
**Owner**: current session
**Architect approval**: ✅ *Expand-to-all-15 chunked across Chunk 0 → 1 → 2 → 3*, value-object + shared writer abstraction (narrow, not a base-class hierarchy), reduced test matrix (one writer unit suite + 4 layout-variant snapshots + per-chunk parameterized migration harness + per-chunk damaging-regex guard).
**Scope**: Every email template that currently displays Event Details should render the Phase 7C.1 decomposed Venue Name + Address + optional Secondary Location block (Parking Lot / Secondary Venue). Today only 1 of 16 event-detail-showing templates renders correctly end-to-end. The remaining 15 are broken either at the body layer (5) or at BOTH body + params-class layers (10). This TODO closes that gap.
**Source of truth**: This file. Mirrored into in-session TodoWrite. Closing entries into `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md` per chunk.

---

## Canonical decomposed block (the single shared constant)

Identical to the final `NewBlock` shipped in [20260421150451_Phase7C2_FreeEventTemplate_FixElseClause.cs:52-63](../src/LankaConnect.Infrastructure/Data/Migrations/20260421150451_Phase7C2_FreeEventTemplate_FixElseClause.cs#L52-L63) — two sibling `{{#if}}` blocks (no `{{else}}` — custom engine does not support it; `LocationAddress` is guaranteed non-empty by the projection, falls back to `"Online Event"`).

```handlebars
{{#if HasLocationName}}<span ...>{{LocationName}}</span>{{/if}}
<span ...>{{LocationAddress}}</span>
{{#if HasSecondaryLocation}}
  <span ...>{{SecondaryLocationLabel}}</span>
  {{#if HasSecondaryLocationName}}<span ...>{{SecondaryLocationName}}</span>{{/if}}
  <span ...>{{SecondaryLocationAddress}}</span>
{{/if}}
```

This is the string that replaces every `{{EventLocation}}` token across the 15 remaining templates. Each chunk's migration does the same `REPLACE(html_template, '{{EventLocation}}', <NewBlock>)` — no regex, no wrapper-literal pinning, token is unique per template.

---

## Template scope (16 total, 1 working, 15 to fix)

| # | Template | Current body | Code-side ready | Chunk |
|---|---|---|---|---|
| 1 | `template-free-event-registration-confirmation` | ✅ decomposed | ✅ | — (done) |
| 2 | `template-signup-list-commitment-confirmation` | ❌ flat | ✅ | 1 |
| 3 | `template-signup-list-commitment-update` | ❌ flat | ✅ | 1 |
| 4 | `template-signup-list-commitment-cancellation` | — no `{{EventLocation}}` by design | ✅ | 1 (no-op) |
| 5 | `template-volunteer-commitment-confirmation` | ❌ flat | ✅ | 1 |
| 6 | `template-volunteer-commitment-cancellation` | — no `{{EventLocation}}` by design | ✅ | 1 (no-op) |
| 7 | `template-paid-event-registration-confirmation-with-ticket` | ❌ flat | ❌ | 2 |
| 8 | `template-event-registration-cancellation` | ❌ flat | ❌ | 2 |
| 9 | `template-event-cancellation-notifications` | ❌ flat | ❌ | 2 |
| 10 | `template-event-approval` | ❌ flat | ❌ | 2 |
| 11 | `template-event-reminder` | ❌ flat | ❌ | 2 |
| 12 | `template-attendees-added-confirmation` | ❌ flat | ❌ | 2 |
| 13 | `template-preliminary-registration-payment-pending` | ❌ flat | ❌ | 2 |
| 14 | `template-form-response-confirmation` | ❌ flat | ❌ | 3 |
| 15 | `template-form-response-update` | ❌ flat | ❌ | 3 |
| 16 | `template-form-response-cancellation` | ❌ flat | ❌ | 3 |

Cancellation templates 4 and 6 don't currently contain `{{EventLocation}}` and don't render an EVENT DETAILS card by design. Chunk 1's migration skips them explicitly (RAISE NOTICE only).

---

## Chunk 0 — Foundation (no migration, no template touch)

**Goal**: Pin the canonical decomposed block as a shared constant so the 3 subsequent migrations reference the same string. Add a structured log to `CommitmentCancelledEmailHandler` to disambiguate the Symptom 2 inbox-confusion question before the next inbox test.

**Acceptance**:
- [ ] `EmailLocationBlockHtml.DecomposedBlock` constant exists, byte-identical to the `NewBlock` in `20260421150451_Phase7C2_FreeEventTemplate_FixElseClause.NewBlock`.
- [ ] Unit test asserts the constant contains `{{LocationName}}`, `{{LocationAddress}}`, `{{SecondaryLocationName}}`, `{{SecondaryLocationAddress}}`, `{{SecondaryLocationLabel}}`, does NOT contain `{{EventLocation}}`, `{{else}}`, `{{/else}}`.
- [ ] Unit test asserts the constant matches the pilot migration's `NewBlock` exactly (reflection-based or regenerate-and-compare).
- [ ] `CommitmentCancelledEmailHandler.Handle(...)` emits `Information` log with `EventId`, `EventTitle`, `HasSecondaryLocation`, `PrimaryAddress`, `UserId`, `CommitmentId`, a correlation id. Unit test verifies the log line via `ITestOutputHelper`/`Serilog.Sinks.InMemory`.
- [ ] `dotnet build LankaConnect.sln` → 0 errors.
- [ ] `dotnet test` suite baseline stays green.
- [ ] Commit + push `develop` → `deploy-staging.yml` green.
- [ ] `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md` closing entry.

**Tasks**:
- [ ] 0.1 RED: add unit test asserting `EmailLocationBlockHtml.DecomposedBlock` shape and equality with `Phase7C2_FreeEventTemplate_FixElseClause.NewBlock`.
- [ ] 0.2 GREEN: create `src/LankaConnect.Shared/Email/Helpers/EmailLocationBlockHtml.cs` with the constant.
- [ ] 0.3 RED: add unit test for `CommitmentCancelledEmailHandler` structured log (via `Serilog.Sinks.InMemory`).
- [ ] 0.4 GREEN: add `_logger.LogInformation("Cancellation email — eventId={EventId} ...", ...)` at entry of the handler.
- [ ] 0.5 Run full build + tests locally.
- [ ] 0.6 Commit, push, wait for `deploy-staging.yml` green.
- [ ] 0.7 Update `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md`.

---

## Chunk 1 — Signup & volunteer commitments (5 templates; 3 active REPLACEs, 2 no-ops)

**Goal**: Re-apply the decomposed block that the Phase 7C.2 recovery migration erased. Code side is already ready (`SignupCommitmentEmailParams` + `UserCommittedToSignUpEventHandler` + `CommitmentUpdatedEventHandler` + `CommitmentCancelledEmailHandler` already call `ProjectEmailLocation()` / `WithLocationDetails()`).

**Migration**: `Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates` (EF Core `migrations add`).

**Scope**:
- `template-signup-list-commitment-confirmation`, `-update`, `template-volunteer-commitment-confirmation` — active REPLACE of `{{EventLocation}}` → `EmailLocationBlockHtml.DecomposedBlock`.
- `template-signup-list-commitment-cancellation`, `template-volunteer-commitment-cancellation` — RAISE NOTICE only (no body change).
- Chunk-scoped backup table `communications.email_templates_backup_phase7c2b`.
- 5 post-UPDATE `RAISE EXCEPTION` invariants per active template: `ROW_COUNT = 1`, body no longer contains `{{EventLocation}}`, body contains `{{LocationName}}`, body contains `{{UserName}}`, `length(body) ≥ 50000`.

**Tasks**:
- [ ] 1.1 RED: Testcontainers integration test — seed template body with `{{EventLocation}}`, apply migration, assert decomposed block present + old token absent + backup table populated; `Down()` restores.
- [ ] 1.2 RED: render-snapshot test — run `AzureEmailService.RenderTemplateContent` against the post-migration body with (a) multi-venue fixture, (b) single-venue null-`LocationName` fixture, assert expected HTML.
- [ ] 1.3 GREEN: `dotnet ef migrations add Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates`.
- [ ] 1.4 Fill `Up()` + `Down()` using `EmailLocationBlockHtml.DecomposedBlock` + chunk-scoped backup table + 5 invariants per active template.
- [ ] 1.5 Run full build + tests.
- [ ] 1.6 Commit, push, wait for `deploy-staging.yml` green.
- [ ] 1.7 Staging DB probe — SELECT `has_old`, `has_new`, `length` on all 5 templates, verify 3 updated + 2 unchanged.
- [ ] 1.8 Live inbox smoke — commit/update/cancel on event `d543629f` (multi-venue), inbox-verify Venue Name bold + secondary Parking Lot block. Also test a commit on a single-venue event with no `locationName`.
- [ ] 1.9 Update tracking docs.

---

## Chunk 2 — Registration & event-lifecycle (7 templates, 7 params classes)

**Goal**: Both body-layer AND code-layer fix for the paid-ticket + registration + lifecycle family. Every one of these 7 params classes currently hand-writes only a flat `"EventLocation"` key.

**Migration**: `Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates`.

**Params classes to extend**:
- `TicketConfirmationEmailParams` (paid-ticket)
- `RegistrationCancellationEmailParams`
- `EventCancellationEmailParams`
- `EventApprovalEmailParams`
- `EventReminderEmailParams`
- `AttendeesAddedEmailParams`
- `PreliminaryRegistrationPaymentEmailParams`

**Per-class pattern** (identical to what `FreeEventRegistrationEmailParams` already does):
1. Add `LocationDetails : LocationEmailProjection?` property + `WithLocationDetails(projection)` fluent setter.
2. In `ToDictionary()` replace the hand-written `"EventLocation"` line with `LocationEmailDictionaryWriter.WriteTo(dict, LocationDetails ?? LocationEmailProjection.Online)`.
3. Ensure the sending handler (or factory) threads `@event.ProjectEmailLocation()` into `WithLocationDetails(...)`.

**Tasks**:
- [x] 2.1 RED: unit test per params class — `ToDictionary()` writes all 8 decomposed keys + legacy `EventLocation` key when `LocationDetails` is set (7 tests). *Delivered by Chunk 2a (commit 93f83122).*
- [x] 2.2 GREEN: extend each params class + sending handler (7 code-side pairs). *Chunk 2a.*
- [x] 2.3 RED: transformation-logic unit tests (`Phase7C3aDecomposeLocationTests`, 6 tests green) — assert `string.Replace` semantics for `{{EventLocation}}` and the `{{Location}}` variant + idempotence + token uniqueness.
- [x] 2.4 GREEN: migration `20260423065018_Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates` with chunk-scoped backup `_phase7c3a`, per-template REPLACE, and 5 `RAISE EXCEPTION` invariants each.
- [x] 2.5 Run full build + tests. *Infrastructure.Tests 317/317 green; Phase 7C slice 51/51 green.*
- [x] 2.6 Commit, push, `deploy-staging.yml` green. *First deploy (run 24853603855, commit ab885c2f) failed at event-cancellation-notifications anchor invariant — body uses "Dear LankaConnect Community," not {{UserName}}. Staging probe confirmed the transaction rolled back cleanly (no backup table, no history row). Amended migration (commit 8c7c5bf4) switched anchor to {{EventTitle}} and demoted preliminary-registration-payment-pending (no location token) to NOTICE-only no-op. Re-deploy run 24855280067 green.*
- [x] 2.7 Staging DB probe — 6 active templates `has_standard=False, has_decomposed=True`; preliminary length unchanged (57228). Byte-delta math matches (+713 for `{{EventLocation}}` instances, +718 for the `{{Location}}` variant in event-reminder).
- [ ] 2.8 Live inbox smoke — at minimum: paid-ticket registration confirmation on a multi-venue event; registration cancellation; event approval; event reminder. (4 of 7 covered manually; the remaining 3 are code-path-identical.)
- [ ] 2.9 Update tracking docs.

---

## Chunk 3 — Form-response (3 templates, 1 shared params class)

**Goal**: Body + code-layer fix for form-response family. Smallest chunk.

**Migration**: `Phase7C3b_DecomposeLocationInFormResponseTemplates`.

**Params class**: `FormResponseEmailParams` (one class used by all three templates).

**Tasks**:
- [ ] 3.1 RED: unit test for `FormResponseEmailParams.ToDictionary()` writes decomposed keys.
- [ ] 3.2 GREEN: extend `FormResponseEmailParams` + sending handler.
- [ ] 3.3 RED: Testcontainers integration test (parameterized over 3 template names).
- [ ] 3.4 GREEN: `dotnet ef migrations add Phase7C3b_DecomposeLocationInFormResponseTemplates` + fill Up/Down + backup table `_phase7c3b` + per-template invariants.
- [ ] 3.5 Run full build + tests.
- [ ] 3.6 Commit, push, `deploy-staging.yml` green.
- [ ] 3.7 Staging DB probe — 3 templates `has_old=false, has_new=true`.
- [ ] 3.8 Live inbox smoke — submit a form response on a multi-venue event, inbox-verify.
- [ ] 3.9 Update tracking docs, close this TODO.

---

## Cross-chunk discipline (enforced per chunk)

1. **No regex on email HTML body** (MEMORY rule `feedback_regex_on_email_html.md`) — every migration uses literal `REPLACE(...)` on the unique `{{EventLocation}}` token.
2. **Chunk-scoped backup table** — never reuse `_phase7c2` or `_phase7c2b` across chunks. Recovery-scoping lesson from the Phase 7C.2 incident.
3. **Per-template `RAISE EXCEPTION` invariants** — `ROW_COUNT = 1`, `{{EventLocation}}` gone, `{{LocationName}}` present, `{{UserName}}` present, body length ≥ 50000.
4. **EF Core migrations with `.Designer.cs`** (MEMORY 6A.133) — always via `dotnet ef migrations add`, never hand-written.
5. **Test surface reduction** — one `LocationEmailDictionaryWriter` unit suite exists already (extend as needed); per-chunk parameterized integration + snapshot tests, not per-template.
6. **Observability** — every handler that sends an event-location email must log `EventId`, `EventTitle`, `HasSecondaryLocation`, `PrimaryAddress` at `Information` before the send, inside a `try/catch` that logs `Error` with full exception on failure.

---

## Verification (the user's scope-question answer, once all chunks land)

- N_identified = 16.
- N_updated via decomposed block = 16 (1 already + 5 Chunk 1 + 7 Chunk 2 + 3 Chunk 3).
- N_currently working = 16.
- N_broken = 0.

---

## Rollback plan

Each chunk's `Down()` restores from its own `_phase7c2b` / `_phase7c3a` / `_phase7c3b` backup table keyed by `Id`. A failed migration aborts inside its Postgres transaction (invariants `RAISE EXCEPTION`) and the backup table is the canonical pre-migration snapshot. `__EFMigrationsHistory` will only list a chunk if its Up() completed cleanly.
