# Phase 6A.148 — Refund Approval Workflow

**Date opened:** 2026-05-16
**Branch:** `feat/phase-6a-148-refund-approval-workflow` (to be created off current `Production_05_09_2026` or `main` — confirm before code starts)
**Status:** 📋 Master TODO ready — awaiting user approval before code changes
**Architect validation:** ✅ APPROVED-WITH-CHANGES (12 must-fix items folded into plan)
**Plan source of truth:** `C:\Users\Niroshana\.claude\plans\you-need-to-create-rippling-pizza.md`

---

## Goal in one sentence

Stop refunds from happening on their own when an attendee clicks "Cancel & Refund" — record the request, require organizer approval (per-bucket choice), and only then call Stripe; with scan-guard, no-show, withdraw, and organizer-initiated flows all wired in.

---

## Operator's verbatim ask (2026-05-16)

> Currently refund doesn't have any limitation or approval. Even anyone can refund the money after the event and even after scanning and accepting the ticket.
>
> 1. Anyone can request a refund at anytime. Then a refund should be recorded somewhere and need to go for event organizers for approval. If they approve only, the refund will initiate and complete.
> 2. If the ticket is accepted, user cannot request a refund.
> 3. If the attendee is not participated but the event is already happened, attendee should be able to request a refund.
> 4. Organizer should be able decide which portion to refund (ticket price, add-on, sponsorship, contributions…etc).
> 5. Refund should trigger once organizer is approved, until then just a record in the database and on the registration page display "Refund is requested and waiting for organizer approval." Once approved everything works as today.
> 6. **GATE: No refund happen without approval.**
> 7. Plan in the event management page under attendees tab for organizers to manage/handle refunds.

---

## Decisions locked-in by product owner (2026-05-16)

| # | Decision | Locked |
|---|---|---|
| D1 | **Donations**: not refundable through this workflow (4 line-item types only: Ticket, AddOn, Collection, Sponsor). | ✅ |
| D2 | **Organizer-initiated refunds**: enabled from day one — skips Pending, goes straight to Approved → Processing. | ✅ |
| D3 | **Pending SLA**: no auto-action; organizer UI shows "Overdue >3 days" badge. | ✅ |
| D4 | **Phasing**: A + B + C combined into one PR — full feature ships together. | ✅ |
| D5 | **Approver scope**: anyone with `IEventAuthorizationService.CanManageEvent` (owner + co-organizers + event managers). | ✅ |
| D6 | **Scan-guard override**: organizer-initiated path only; requires non-empty `OrganizerNotes`. | ✅ |

---

## Architect findings (Architecture agent, 2026-05-16)

**Verdict: APPROVED-WITH-CHANGES** — 12 blockers identified, all folded into the plan file before this TODO was written.

### 🔴 Must-fix-before-implementation (all incorporated)

| # | Finding | Action |
|---|---|---|
| F1 | Single-pending guard predicate too narrow — only blocks `Pending`, lets `Approved`/`Processing` slip through. | Domain validation in `Registration.CreateRefundRequest` checks `Pending \| Approved \| Processing`. |
| F2 | Approve-with-all-zero is not a valid state — was "degenerate Reject" in initial design, but loses the `RejectionReason` field. | `ApproveRefundRequestCommandHandler` returns 400 ValidationError if `sum(ApprovedAmount) == 0`. Organizer must use `/reject` instead. |
| F3 | `RowVersion` column deviates from project convention. | Use `UseXminAsConcurrencyToken()` (matches `RegistrationConfiguration.cs:327`). No explicit column. |
| F4 | Webhook idempotency must be explicit; 4 distinct webhook handlers exist (Registration / AddOnPurchase / Collection / Sponsor). | Idempotency guard in all 4 handlers: if line item already `Refunded` / `Failed`, return without re-emitting events. |
| F5 | Legacy `/rsvp/withdraw-refund` route must stay bound during transition window. | Route stays in `EventsController:1050` behind feature flag; returns `410 Gone` when flag ON; removed in follow-up PR after frontend ships. |
| F6 | `OrganizerNotes` must not leak to attendee DTOs. | `GetMyRefundRequestQueryHandler` projects a DTO that excludes `OrganizerNotes`. Unit test asserts the field is absent. |
| F7 | `ScanGuardOverridden == true` must require non-empty `OrganizerNotes` (audit trail). | Domain invariant in `CreateRefundRequest`. |
| F8 | Currency-match invariant on `ApprovedAmount` vs `RequestedAmount`. | Domain invariant in `RefundRequest.Approve`. |
| F9 | One `RefundRequestLineItem` per `AddOnPurchase` (Add-Only Attendees creates multiple purchases per attendee). | Domain `CreateRefundRequest` enforces uniqueness per `(Type, ReferenceId)`; `ReferenceId = AddOnPurchase.Id`. |
| F10 | Stripe dispatch must happen AFTER `Approve` commits — today's code holds a DB transaction across the Stripe HTTP call. | `RefundExecutionService` invoked outside the approve transaction (fresh DbContext scope or Hangfire job). Per-line Stripe call has its own DB save. |
| F11 | `RefundReconciliationService` must be extended for stuck `Approved` rows (process crashed between Approve commit and Stripe dispatch). | Existing cron extended to scan `RefundRequest.Status == Approved` older than 10 min. |
| F12 | Wrong test password in smoke-test curls. | Corrected to `12!@qwASzx`. |

### 🟡 Should-address (folded into plan)
- Out-of-scope: per-ticket partial refund within a single registration (organizer-initiated + partial `ApprovedAmount` covers the escape hatch).
- Documented: refunds are gross (not net of Stripe fees) — matches today's behaviour.
- Hard-delete protection: domain guard prevents `Registration` delete when any `RefundRequest.Status >= Approved`.

### 🟢 Nice-to-have / out-of-scope
- Auto-approve/auto-reject SLA — overdue badge only.
- Donations refundability — not refundable for v1.

---

## Pre-flight (before any code)

- [ ] Reserve phase number **6A.148** in `docs/PHASE_6A_MASTER_INDEX.md` (add row between 6A.147 and start of 6B).
- [ ] Create branch `feat/phase-6a-148-refund-approval-workflow` off appropriate base (confirm with user — `main` vs `Production_05_09_2026`).
- [ ] Confirm `.env` has staging Stripe test keys available for end-to-end webhook testing.
- [ ] Confirm test event in staging has: (a) a paid registration, (b) at least one add-on purchase, (c) at least one collection, (d) at least one sponsor — for full per-bucket smoke matrix.

---

## Phase task checklist

### Phase 1 — Domain layer (TDD, RED → GREEN)

#### 1.A New enums
- [ ] Write failing tests for `RefundRequestStatus` (Pending=0, Approved=1, Processing=2, Completed=3, Rejected=4, Withdrawn=5) — sanity-check values.
- [ ] Write failing tests for `RefundLineItemType` (Ticket=0, AddOn=1, Collection=2, Sponsor=3).
- [ ] Write failing tests for `RefundLineItemStatus` (Requested=0, Approved=1, Rejected=2, Processing=3, Refunded=4, Failed=5).
- [ ] Implement 3 enum files in `src/LankaConnect.Domain/Events/Enums/`.

#### 1.B `RegistrationStatus.PendingRefundApproval = 10`
- [ ] Write failing test for new enum value (it doesn't collide with existing values and is documented).
- [ ] Add to `src/LankaConnect.Domain/Events/Enums/RegistrationStatus.cs` (additive, backward-compatible — existing `RefundRequested = 9` unchanged).

#### 1.C `RefundRequestLineItem` entity
- [ ] Write failing tests:
  - Constructor sets all fields correctly.
  - `RequestedAmount > 0` required.
  - `ApprovedAmount` null until reviewed.
  - **Invariant**: `ApprovedAmount.Currency == RequestedAmount.Currency` (F8).
  - State transitions: `Requested → Approved` (when `ApprovedAmount > 0`), `Requested → Rejected` (when `ApprovedAmount == 0`).
  - `MarkProcessing(stripeRefundId, stripeChargeId)`, `MarkRefunded(processedAt)`, `MarkFailed(reason)`.
  - **Idempotency**: calling `MarkRefunded` twice is a no-op (F4 webhook idempotency guard).
- [ ] Implement entity in `src/LankaConnect.Domain/Events/Entities/RefundRequestLineItem.cs`.

#### 1.D `RefundRequest` entity
- [ ] Write failing tests:
  - Factory methods: `CreatePending(...)` (attendee path) and `CreateOrganizerInitiated(...)` (organizer path, skips Pending → Approved directly).
  - State machine — all valid transitions and all forbidden backward transitions (once Approved, no backward).
  - **`Approve(perLineApprovedAmounts)` requires `sum(ApprovedAmount) > 0`** (F2) — otherwise returns failure.
  - `Reject(reason)` requires non-empty reason.
  - `Withdraw()` only allowed in `Pending` state.
  - Concurrency: `RowVersion` increment behavior is NOT manually checked here — Postgres `xmin` handles it at infrastructure level (F3). But add a test ensuring the entity exposes the relevant state for EF to track.
  - **`ScanGuardOverridden == true` requires non-empty `OrganizerNotes`** (F7).
  - Raises 5 domain events at correct transitions.
- [ ] Implement entity in `src/LankaConnect.Domain/Events/Entities/RefundRequest.cs`.

#### 1.E `Registration` aggregate extensions
- [ ] Write failing tests for `Registration.CreateRefundRequest(...)`:
  - Returns failure when `Status != Confirmed`.
  - Returns failure when `PaymentStatus != Completed`.
  - Returns failure when `StripePaymentIntentId` is empty.
  - **No-active-request check (F1)**: returns failure if any existing `RefundRequest.Status` is in `{Pending, Approved, Processing}`.
  - **Scan guard**: returns failure when any ticket has `ValidatedAt != null` AND `isOrganizerInitiated == false`.
  - **Scan guard override**: allows when `isOrganizerInitiated == true` AND `overrideScanGuard == true` AND `OrganizerNotes` non-empty (F7).
  - **No-show post-event**: allows after `Event.EndDate` provided scan guard passes (Rule #3).
  - **One line item per `(Type, ReferenceId)`** — duplicates rejected (F9).
  - Transitions `Registration.Status: Confirmed → PendingRefundApproval`.
  - Raises `RefundRequestCreatedEvent` (attendee path) or `OrganizerInitiatedRefundCreatedEvent` (organizer path).
- [ ] Write failing tests for internal state-transition helpers: `MoveToPendingRefundApproval()`, `MoveToRefundRequestedFromApproval()`, `MoveToConfirmedFromApproval()`.
- [ ] Write failing test for **hard-delete protection**: `Registration` cannot be deleted while any `RefundRequest.Status >= Approved`.
- [ ] Implement `CreateRefundRequest` and helpers in `Registration.cs`. Wire navigation collection `_refundRequests`.

#### 1.F Domain events
- [ ] Create 5 new domain event records in `src/LankaConnect.Domain/Events/DomainEvents/`:
  - `RefundRequestCreatedEvent`
  - `OrganizerInitiatedRefundCreatedEvent`
  - `RefundRequestApprovedEvent`
  - `RefundRequestRejectedEvent`
  - `RefundRequestWithdrawnEvent`
  - (existing `RefundCompletedEvent` is reused on webhook completion — no change needed.)

#### 1.G Gate
- [ ] `dotnet test tests/LankaConnect.Application.Tests --filter "FullyQualifiedName~Domain.RefundRequest"` → ALL GREEN.
- [ ] `dotnet test tests/LankaConnect.Application.Tests --filter "FullyQualifiedName~Domain.Registration.CreateRefundRequest"` → ALL GREEN.
- [ ] `dotnet build src/LankaConnect.sln` → zero errors, zero warnings (project rule #2).

---

### Phase 2 — Infrastructure (EF Core)

#### 2.A Entity configurations
- [ ] Create `RefundRequestEntityConfiguration.cs` in `src/LankaConnect.Infrastructure/Data/Configurations/`:
  - Table name + schema: `refund_requests` in `events` schema.
  - Primary key + FKs (registration_id → registrations.id ON DELETE RESTRICT).
  - **`UseXminAsConcurrencyToken()`** (F3).
  - All columns mapped per plan §6.1.
  - Indexes: `(registration_id)`, `(status)`.
- [ ] Create `RefundRequestLineItemEntityConfiguration.cs`:
  - Table name + schema: `refund_request_line_items` in `events` schema.
  - Primary key + FK (refund_request_id → refund_requests.id ON DELETE CASCADE).
  - Money owned-type mapping (Amount + Currency).
  - Indexes: `(refund_request_id)`, `(stripe_refund_id)`.
- [ ] Update `RegistrationEntityConfiguration.cs` to add `HasMany(r => r._refundRequests)` navigation.

#### 2.B Repository
- [ ] Create `IRefundRequestRepository` interface in `src/LankaConnect.Domain/Events/Repositories/`:
  - `GetByIdAsync(id, ct)`
  - `GetByRegistrationIdAsync(registrationId, ct)`
  - `ListPendingByEventAsync(eventId, ct)` (organizer queue — `AsNoTracking` projection)
  - `ListByEventAsync(eventId, statusFilter?, ct)` (organizer queue with filter)
  - `GetMyForEventAsync(eventId, userId, ct)` (attendee view)
  - `ListStuckApprovedAsync(olderThan, ct)` (for `RefundReconciliationService` F11)
- [ ] Implement in `src/LankaConnect.Infrastructure/Data/Repositories/RefundRequestRepository.cs`.
- [ ] Register in DI container.

#### 2.C Migration
- [ ] Generate: `dotnet ef migrations add Phase6A148_AddRefundApprovalWorkflow --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API`.
- [ ] **Verify `[Migration("...")]` attribute is present in `.Designer.cs`** (project rule #8 — EF Core ignores migrations without this attribute).
- [ ] Review generated SQL — should be additive only (no touches to existing Registration columns).
- [ ] Test up locally on a clean DB: `dotnet ef database update`.
- [ ] Verify in psql: `\d events.refund_requests`, `\d events.refund_request_line_items`.
- [ ] Test down locally: `dotnet ef migrations remove` then re-apply.

#### 2.D Gate
- [ ] All Phase 1 tests still GREEN.
- [ ] No compilation errors across solution.

---

### Phase 3 — Application layer (CQRS handlers)

#### 3.A `CreateRefundRequestCommandHandler` (attendee path)
- [ ] Write failing tests:
  - Happy path: creates Pending request, transitions Registration, raises `RefundRequestCreatedEvent`.
  - Auth: returns 401 if `UserId` doesn't match `Registration.UserId`.
  - Scan-guarded ticket: returns 400.
  - No active request validation: returns 400.
  - Empty line items list: returns 400.
- [ ] Implement handler. Map domain `Result` to `Result<CreateRefundRequestResult>`.

#### 3.B `CreateOrganizerInitiatedRefundCommandHandler`
- [ ] Write failing tests:
  - Happy path: creates request directly in `Approved` state, transitions Registration to `PendingRefundApproval`, raises `OrganizerInitiatedRefundCreatedEvent`, dispatches Stripe via `IRefundExecutionService` (mocked).
  - Auth: returns 403 when caller fails `IEventAuthorizationService.CanManageEvent`.
  - Scan-guard override path: `overrideScanGuard=true` + non-empty `organizerNotes` → success.
  - Override without notes: returns 400 (F7).
- [ ] Implement.

#### 3.C `ApproveRefundRequestCommandHandler`
- [ ] Write failing tests:
  - Happy path: transitions to `Approved`, raises `RefundRequestApprovedEvent`, schedules `IRefundExecutionService` outside the transaction (F10).
  - **Approve-with-all-zero**: returns 400 ValidationError (F2).
  - Auth: returns 403 when caller fails `CanManageEvent`.
  - **Concurrency (F3)**: two parallel approves on same request — first wins; second gets `DbUpdateConcurrencyException` → mapped to 409 Conflict.
  - Currency mismatch (`ApprovedAmount.Currency != RequestedAmount.Currency`): returns 400 (F8).
  - Line item not in request: returns 400.
- [ ] Implement. Map exceptions to API responses.

#### 3.D `RejectRefundRequestCommandHandler`
- [ ] Write failing tests:
  - Happy path: transitions to `Rejected`, raises `RefundRequestRejectedEvent`, transitions `Registration.Status → Confirmed`.
  - Empty reason: returns 400.
  - Auth: returns 403 when caller fails `CanManageEvent`.
  - Concurrency: same xmin pattern as Approve.
- [ ] Implement.

#### 3.E `WithdrawRefundRequestV2CommandHandler`
- [ ] Write failing tests:
  - Happy path: attendee withdraws own Pending request → `Status = Withdrawn`, Registration back to `Confirmed`.
  - Not own request: returns 403.
  - Not in Pending state: returns 400 ("Cannot withdraw after approval").
- [ ] Implement.

#### 3.F Query handlers
- [ ] `GetMyRefundRequestQueryHandler`:
  - Write test asserting **`OrganizerNotes` is NOT in the response DTO** (F6).
  - Returns current/historical request for the caller.
- [ ] `GetEventRefundRequestsQueryHandler`:
  - Paginated, filterable by status.
  - Auth: caller must pass `CanManageEvent`.
  - Includes `OrganizerNotes` (organizer-side view).
- [ ] Implement both with separate DTOs (`AttendeeRefundRequestDto` vs `OrganizerRefundRequestDto`).

#### 3.G `RefundExecutionService`
- [ ] Write failing tests:
  - Iterates approved line items; calls Stripe per line (mocked `IStripePaymentService`).
  - Failed Stripe call → line item moved to `Failed` with `FailureReason`; other lines continue.
  - **Runs in fresh DbContext scope** (F10) — verify via DI scoping test.
  - Marks `RefundRequest.Status: Approved → Processing` after first successful dispatch.
  - Transitions `Registration.Status: PendingRefundApproval → RefundRequested`.
  - One Stripe call per `AddOnPurchase` (F9), not aggregated.
- [ ] Implement by refactoring existing `RegistrationRefundService` + `AddOnRefundService` + sponsor/collection refund logic into a single execution service.
- [ ] Wire as domain event handler for `RefundRequestApprovedEvent` and `OrganizerInitiatedRefundCreatedEvent`.

#### 3.H `CancelRsvpCommandHandler` modification (gated)
- [ ] Add feature-flag check: when `Refund:ApprovalWorkflow:Enabled == true` AND registration is paid → return error pointing to new endpoint.
- [ ] Free-registration cancellation logic unchanged.
- [ ] Tests: flag ON path + flag OFF path (existing behavior preserved).

#### 3.I Webhook handler updates
- [ ] Update **all 4** webhook handlers (`IRegistrationWebhookHandler`, `IAddOnPurchaseWebhookHandler`, `ICollectionWebhookHandler`, `ISponsorWebhookHandler`) to mark the corresponding `RefundRequestLineItem` as Refunded (F4).
- [ ] **Idempotency guard (F4)**: if line item already in terminal state, log + return — do NOT raise duplicate events.
- [ ] When all line items in a request reach terminal state, transition `RefundRequest.Status: Processing → Completed`.
- [ ] Existing `RefundCompletedEvent` continues to fire (preserves existing email job).

#### 3.J `RefundReconciliationService` extension (F11)
- [ ] Write failing test: stuck `Approved` requests older than 10 min are re-dispatched.
- [ ] Extend existing service to also scan `RefundRequest.Status == Approved`.
- [ ] Keep existing stuck `RefundRequested` recovery untouched.

#### 3.K Legacy `WithdrawRefundRequestCommandHandler` (F5)
- [ ] Add feature-flag check: when flag ON, return `410 Gone` with Problem Details pointing to new endpoint.
- [ ] When flag OFF, run legacy flow unchanged.
- [ ] Tests cover both paths.

#### 3.L Gate
- [ ] All Application tests GREEN.
- [ ] Zero compilation errors.

---

### Phase 4 — API layer

#### 4.A Endpoints in `EventsController`
- [ ] `POST /api/events/{eventId}/refund-requests` — attendee creates Pending request.
- [ ] `GET /api/events/{eventId}/refund-requests/me` — attendee fetches own.
- [ ] `POST /api/events/{eventId}/refund-requests/me/withdraw` — attendee withdraws.
- [ ] `GET /api/events/{eventId}/refund-requests?status=...` — organizer lists.
- [ ] `POST /api/events/{eventId}/refund-requests/organizer-initiated` — organizer creates on behalf.
- [ ] `POST /api/events/{eventId}/refund-requests/{id}/approve` — organizer approves with per-line amounts.
- [ ] `POST /api/events/{eventId}/refund-requests/{id}/reject` — organizer rejects with reason.

#### 4.B Request/response DTOs
- [ ] `CreateRefundRequestPayload` (attendee + organizer paths).
- [ ] `ApproveRefundRequestPayload` (per-line amounts).
- [ ] `RejectRefundRequestPayload`.
- [ ] `AttendeeRefundRequestDto` (no `OrganizerNotes`).
- [ ] `OrganizerRefundRequestDto` (full fields).

#### 4.C Authorization
- [ ] `[Authorize]` on all endpoints.
- [ ] Organizer endpoints use `IEventAuthorizationService.CanManageEvent` (D5).
- [ ] Attendee endpoints verify registration ownership.

#### 4.D Feature flag check
- [ ] At controller-level filter: when `Refund:ApprovalWorkflow:Enabled == false`, return 404.

#### 4.E Logging + observability (project rule #6)
- [ ] Structured logs at each endpoint entry/exit with `RegistrationId`, `RefundRequestId`, `UserId`, `EventId`.
- [ ] Try/catch with logged exceptions before rethrow.
- [ ] Log at INFO for state transitions; WARN for validation failures; ERROR for Stripe/webhook failures.

#### 4.F Gate
- [ ] All integration tests pass (where applicable).
- [ ] Swagger documentation rendered correctly for all 7 endpoints.

---

### Phase 5 — Backend deployment & staging smoke

#### 5.A Commit + push
- [ ] `git add` only the relevant files (no `-A`).
- [ ] Commit with Co-Authored-By trailer per project convention.
- [ ] Push `feat/phase-6a-148-refund-approval-workflow` branch.

#### 5.B Verify staging deploy
- [ ] `deploy-staging.yml` run succeeds (check Actions tab).
- [ ] Container logs show successful startup; no exceptions during DI resolution.
- [ ] Migration applied: query `events.refund_requests` table exists.

#### 5.C Pre-test setup
- [ ] Get token:
  ```bash
  TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
    -H 'Content-Type: application/json' \
    -d '{"email":"niroshhh@gmail.com","password":"12!@qwASzx","rememberMe":true,"ipAddress":"string"}' \
    | jq -r '.token')
  ```
- [ ] Identify test event in staging with at least: 1 paid registration, 1 add-on, 1 collection, 1 sponsor.
- [ ] Identify organizer credentials for the same event.

#### 5.D API smoke matrix (per project rule #10)

**T1 — Attendee creates Pending request**
- [ ] `POST /api/events/{eventId}/refund-requests` with Ticket line.
- [ ] Expect 200 + RefundRequestId. Verify in DB: `Registration.Status == PendingRefundApproval`, request row in `events.refund_requests`.

**T2 — Attendee gets own request**
- [ ] `GET .../refund-requests/me` → Status=Pending.
- [ ] **Assert response body does NOT contain `organizerNotes`** (F6).

**T3 — Scan guard blocks request**
- [ ] Validate a ticket via scanner UI.
- [ ] Repeat T1 → expect 400 with "scanned and used" message.

**T4 — No-show post-event allowance (Rule #3)**
- [ ] Use a registration where `Event.EndDate < UtcNow` AND no tickets scanned.
- [ ] Repeat T1 → expect 200 (post-event refund allowed for no-show).

**T5 — Organizer lists Pending requests**
- [ ] `GET .../refund-requests?status=Pending` with organizer token → expect array containing T1's request.

**T6 — Organizer rejects**
- [ ] `POST .../refund-requests/{id}/reject` with `rejectionReason`.
- [ ] Expect 200. Verify `Registration.Status == Confirmed`. **Verify Stripe was NOT called** (check Stripe dashboard / no charge.refunded webhook event).

**T7 — Organizer approves (full amount, single line)**
- [ ] Re-create T1's request.
- [ ] Approve all lines with full amounts.
- [ ] Verify `Registration.Status: PendingRefundApproval → RefundRequested`.
- [ ] Verify line items transition `Approved → Processing` then `Refunded` after webhook.
- [ ] Confirm Stripe Test Mode dashboard shows the refund.

**T8 — Organizer approves partial (per-bucket — Rule #4)**
- [ ] Create request with 4 lines (Ticket + AddOn + Collection + Sponsor).
- [ ] Approve only Ticket (full) + AddOn (50%) — reject Collection + Sponsor (0).
- [ ] Verify only 2 Stripe refund calls made with correct amounts.
- [ ] Verify rejected lines have `Status = Rejected` with `ApprovedAmount = 0`.

**T9 — Organizer-initiated refund (skips Pending — D2)**
- [ ] `POST .../refund-requests/organizer-initiated` with `RegistrationId`, line items, `overrideScanGuard: false`, `organizerNotes`.
- [ ] Verify request created directly in `Approved` state.
- [ ] Stripe dispatch happens immediately.

**T10 — Organizer-initiated with scan-guard override (D6)**
- [ ] Validate ticket first.
- [ ] Repeat T9 with `overrideScanGuard: false` → expect 400.
- [ ] Repeat with `overrideScanGuard: true` AND `organizerNotes: ""` → expect 400 (F7).
- [ ] Repeat with `overrideScanGuard: true` AND non-empty `organizerNotes` → expect 200.

**T11 — Attendee withdraws Pending request**
- [ ] Create Pending request, then `POST .../refund-requests/me/withdraw` → expect 200.
- [ ] Verify `Registration.Status == Confirmed`, request `Status == Withdrawn`.

**T12 — Withdraw after Approved → 400**
- [ ] After approval, attempt withdraw → expect 400.

**T13 — Concurrency: two organizers approve same request (F3)**
- [ ] Open two sessions, both call `/approve` simultaneously.
- [ ] Second returns 409 Conflict (DbUpdateConcurrencyException).

**T14 — Webhook idempotency (F4)**
- [ ] Manually re-fire `charge.refunded` from Stripe dashboard for a completed refund.
- [ ] Verify no duplicate email sent; line item stays in `Refunded`; no exceptions in logs.

**T15 — Single-pending guard (F1)**
- [ ] Create Pending request.
- [ ] Attempt to create another → expect 400.
- [ ] Approve the first request (now Processing).
- [ ] Attempt to create another while Processing → expect 400 (F1 covers Processing too).

**T16 — Currency mismatch (F8)**
- [ ] Send approval with `approvedAmount.currency` differing from request → expect 400.

**T17 — Email verification (per memory `feedback_post_deploy_api_test.md`)**
- [ ] Attendee request → verify organizer received review email + attendee received confirmation.
- [ ] Approval → verify attendee received approved email.
- [ ] Rejection → verify attendee received rejected email with reason.
- [ ] Withdrawal → verify both organizer + attendee notified.
- [ ] Completion → verify existing refund-completed email still fires (no regression).

**T18 — Feature flag OFF behaviour**
- [ ] Toggle `Refund:ApprovalWorkflow:Enabled = false` (via app config update).
- [ ] New endpoints → 404.
- [ ] Legacy `POST /rsvp/withdraw-refund` works again.
- [ ] Legacy `CancelRsvp` paid-refund branch works.
- [ ] Toggle back ON.

**T19 — Legacy reconciliation still works**
- [ ] Existing in-flight `RegistrationStatus.RefundRequested` row → `ForceCancelStuckRefund` still cleans it up.

**T20 — New reconciliation for stuck Approved (F11)**
- [ ] Manually insert a `RefundRequest` with `Status = Approved` and `Updated_At` > 10 min ago.
- [ ] Trigger `RefundReconciliationService` (or wait for cron).
- [ ] Verify the request is re-dispatched to Stripe.

#### 5.E Container log audit
- [ ] `az containerapp logs show --name lankaconnect-api-staging --tail 500`
- [ ] Grep for `[RefundService]`, `[RefundExecutionService]`, `RefundRequest`, `Stripe refund created`.
- [ ] Verify no exceptions outside expected validation paths.

#### 5.F Gate
- [ ] All 20 smoke tests pass with documented curl outputs.
- [ ] Email evidence (screenshots or message IDs) captured for T17.
- [ ] No errors in container logs.

---

### Phase 6 — Frontend layer

#### 6.A New TypeScript types
- [ ] `web/src/domain/types/refund-request.types.ts`:
  - `RefundRequestStatus`, `RefundLineItemType`, `RefundLineItemStatus` enums.
  - `RefundRequestLineItemDto`, `AttendeeRefundRequestDto`, `OrganizerRefundRequestDto`.

#### 6.B API repository
- [ ] Extend `web/src/infrastructure/api/repositories/events.repository.ts` with 7 new functions matching API surface.
- [ ] Add error mapping for 409 (concurrency), 400 (validation), 410 (legacy endpoint deprecated).

#### 6.C Attendee UI — `web/src/app/events/[id]/page.tsx`
- [ ] Locate refund area (lines 1339, 1407-1476).
- [ ] Rename CTA "Cancel & Refund" → "Request Refund."
- [ ] Replace bucket checkbox section with `<RequestRefundDialog />` modal.
- [ ] Add `<RefundRequestStatusBanner />` above registration details:
  - `Pending`: "Refund requested — awaiting organizer approval. You'll be notified by email." + Withdraw button.
  - `Approved`: "Refund approved — processing via Stripe (5-10 business days)."
  - `Processing`: same as Approved (no UX distinction).
  - `Completed`: existing "Refunded" UI.
  - `Rejected`: "Refund request declined: {reason}."
  - `Withdrawn`: revert to original Confirmed UI.

#### 6.D Organizer UI — `AttendeeManagementTab.tsx`
- [ ] Add "Refund Requests" sub-tab alongside existing Attendees view.
- [ ] Default filter: Pending. Toggles: Pending / Approved / All.
- [ ] Row display: requester name, requested at (with "Overdue >3 days" badge), total $, per-line breakdown, reason snippet.
- [ ] Row actions: Review (opens approval dialog), Reject (opens reject dialog).
- [ ] Existing "Force-cancel stuck refund" button preserved for rows in legacy `RegistrationStatus.RefundRequested` > 10 min.
- [ ] On main Attendees tab: add row-level "Initiate Refund" action that opens `<OrganizerInitiatedRefundDialog />`.

#### 6.E New components
- [ ] `web/src/presentation/components/features/events/RequestRefundDialog.tsx` — attendee request modal with per-bucket checkboxes + reason textarea.
- [ ] `web/src/presentation/components/features/events/RefundRequestStatusBanner.tsx` — status banner.
- [ ] `web/src/presentation/components/features/events/RefundRequestsTab.tsx` — organizer queue.
- [ ] `web/src/presentation/components/features/events/RefundApprovalDialog.tsx` — per-line approve dialog with amount inputs + notes textarea.
- [ ] `web/src/presentation/components/features/events/RefundRejectDialog.tsx` — reject with reason textarea.
- [ ] `web/src/presentation/components/features/events/OrganizerInitiatedRefundDialog.tsx` — initiate refund dialog with `overrideScanGuard` checkbox (only visible if any ticket scanned) + mandatory reason.

#### 6.F Loading + error states
- [ ] All async ops show loading indicators.
- [ ] Approve dialog: sum validation client-side; disable submit while in-flight.
- [ ] 409 Conflict on approve → toast: "Another organizer approved this request first. Refreshing..." + reload list.

#### 6.G Accessibility (project rule §3)
- [ ] All inputs have labels.
- [ ] Modals are keyboard-accessible (focus trap, Esc to close).
- [ ] Status banner uses `role="status"` for screen readers.

#### 6.H Type check + build
- [ ] `cd web; npm run type-check` → zero errors.
- [ ] `npm run build` → success.

#### 6.I Local UAT (dev server)
- [ ] Start `npm run dev`.
- [ ] Attendee golden path: request → see banner → withdraw → confirm Confirmed.
- [ ] Organizer golden path: see Pending queue → review → per-line approve → confirm `Refunded` reflects in UI.
- [ ] Reject path: reject with reason → attendee sees rejection banner.
- [ ] Organizer-initiated path: initiate from Attendees tab → bypass Pending → see Approved.
- [ ] Scan-guard override: validate ticket → attempt attendee request (blocked) → organizer initiates with override + notes → succeeds.

#### 6.J Gate
- [ ] No regressions on existing flows (verify with feature flag toggled OFF — UI falls back to existing Cancel/Refund).
- [ ] All 6 components render correctly in mobile breakpoints (320px, 768px, 1024px).

---

### Phase 7 — Frontend deployment & UAT

#### 7.A Commit + push
- [ ] Stage frontend files only.
- [ ] Commit with Co-Authored-By trailer.
- [ ] Push branch.

#### 7.B Verify deploy
- [ ] `deploy-ui-staging.yml` succeeds.
- [ ] Frontend container logs clean.

#### 7.C Staging UAT in browser
- [ ] Re-run smoke matrix T1 / T7 / T8 / T9 / T10 / T11 through the UI (not just curl).
- [ ] Verify mobile responsive (320px, 768px).
- [ ] Verify status banner transitions correctly when refreshing during organizer approval.
- [ ] Verify "Overdue >3 days" badge renders on aged Pending requests.

#### 7.D Gate
- [ ] All UAT scenarios green.
- [ ] No console errors in browser dev tools.
- [ ] No JS exceptions in frontend container logs.

---

### Phase 8 — Documentation + PR

#### 8.A Doc sync (project rule #7 + CLAUDE.md Part B Part 3)
- [ ] Add entry to `docs/PHASE_6A_MASTER_INDEX.md` at row 6A.148.
- [ ] Update `docs/PROGRESS_TRACKER.md` with Phase 6A.148 implementation entry (date, what shipped, smoke evidence).
- [ ] Update `docs/STREAMLINED_ACTION_PLAN.md` action item status.
- [ ] Update `docs/TASK_SYNCHRONIZATION_STRATEGY.md` phase overview if relevant.

#### 8.B Open PR
- [ ] `gh pr create` against `main` (or production branch — confirm with user).
- [ ] PR title: `feat(6A.148): refund approval workflow with organizer gate`.
- [ ] PR body includes:
  - Summary (3 bullets).
  - Test plan checklist (T1-T20 with curl output snippets).
  - Screenshots of attendee + organizer UI.
  - Link to plan file + master TODO doc.
  - Migration evidence (psql `\d` output).
- [ ] Request review from team.

#### 8.C Post-merge
- [ ] After PR merges, confirm `deploy-staging.yml` re-runs cleanly off main.
- [ ] Schedule follow-up PR to remove legacy `/rsvp/withdraw-refund` route + legacy `CancelRsvp` paid-refund branch (per F5 — only after frontend ships to all users).

---

## Critical files to modify (reference §9 of plan)

### Backend
- [src/LankaConnect.Domain/Events/Registration.cs](../src/LankaConnect.Domain/Events/Registration.cs) — add `_refundRequests`, `CreateRefundRequest()`, helpers
- [src/LankaConnect.Domain/Events/Enums/RegistrationStatus.cs](../src/LankaConnect.Domain/Events/Enums/RegistrationStatus.cs) — add `PendingRefundApproval = 10`
- New: `src/LankaConnect.Domain/Events/Entities/RefundRequest.cs`
- New: `src/LankaConnect.Domain/Events/Entities/RefundRequestLineItem.cs`
- New: 3 enum files + 5 domain event records
- New: 5 command handlers + 2 query handlers
- New: `src/LankaConnect.Application/Events/Services/RefundExecutionService.cs`
- [src/LankaConnect.Application/Events/Services/RegistrationRefundService.cs](../src/LankaConnect.Application/Events/Services/RegistrationRefundService.cs) — refactor into helper
- [src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs) — flag-gated paid-refund branch
- [src/LankaConnect.Application/Events/Services/RefundReconciliationService.cs](../src/LankaConnect.Application/Events/Services/RefundReconciliationService.cs) — extend for stuck Approved
- [src/LankaConnect.API/Controllers/EventsController.cs](../src/LankaConnect.API/Controllers/EventsController.cs) — 7 new endpoints
- 2 new entity configurations + Registration config update
- New: `IRefundRequestRepository` + EF implementation
- New: migration `Phase6A148_AddRefundApprovalWorkflow` (+ `[Migration]` attribute in Designer.cs)
- 4 webhook handlers — add idempotency guard + line-item completion logic
- `appsettings.json` + `appsettings.Staging.json` — add `Refund:ApprovalWorkflow:Enabled` flag

### Frontend
- [web/src/app/events/[id]/page.tsx](../web/src/app/events/[id]/page.tsx) — replace lines 1339-1476 area
- [web/src/presentation/components/features/events/AttendeeManagementTab.tsx](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx) — add Refund Requests sub-tab + Initiate Refund row action
- 6 new components in `web/src/presentation/components/features/events/`
- [web/src/infrastructure/api/repositories/events.repository.ts](../web/src/infrastructure/api/repositories/events.repository.ts) — 7 new functions
- New: `web/src/domain/types/refund-request.types.ts`

---

## Reuse / DO-NOT-DUPLICATE list (project rule #4)

| Use existing | Don't duplicate |
|---|---|
| `IRefundExecutionService` wraps `IStripePaymentService.CreateRefundAsync` (existing) | Don't add a new Stripe wrapper. |
| Existing `IRegistrationPaymentRepository` for multi-payment lookups (Add-Only Attendees) | Don't query Stripe directly. |
| Existing `AddOnRefundService` logic, refactored into per-line dispatch | Don't re-implement add-on refund accounting. |
| Existing `IEventAuthorizationService.CanManageEvent` for organizer auth | Don't add new role check. |
| Existing `ForceCancelStuckRefund` for legacy stuck rows | Don't merge with new reconciliation logic. |
| Existing `RefundCompletedEvent` + email job for completion email | Don't add a new completion email. |
| Existing `UseXminAsConcurrencyToken()` pattern (RegistrationConfiguration.cs:327) | Don't add manual `RowVersion` column. |
| Existing `[Migration]` attribute in `.Designer.cs` convention | Don't apply migrations without the attribute (will be silently ignored). |
| Existing domain-event → email-job dispatch infrastructure | Don't add new pub-sub mechanism. |

---

## Open follow-ups (out of scope, tracked here)

- **Per-ticket partial refund** within a single registration (Stripe partial against single PaymentIntent without per-ticket accounting). Org-initiated + partial `ApprovedAmount` is the v1 workaround.
- **Legacy code cleanup**: After this PR ships and frontend is fully migrated, follow-up PR removes legacy `/rsvp/withdraw-refund` route + legacy `CancelRsvp` paid-refund branch + legacy `Registration.RequestRefund()` / `WithdrawRefundRequest()` domain methods (currently `[Obsolete]`).
- **Auto-approve/auto-reject SLA**: not in v1; can be added if operator UX demands it.
- **Per-bucket organizer policy templates** (e.g., "auto-approve add-ons under $X"): future phase.

---

## Status (live — updated 2026-05-17)

**Branch**: `feat/phase-6a-148-refund-approval-workflow` (off `main`)
**5 commits**: `e5b0a566` → `1c9f7da8` → `0427bd0e` → `ac16b3eb` → `569e1e12`
**Staging deploy**: ✅ run `25981719368` succeeded
**Backend smoke**: ✅ verified — feature flag ON, migration applied, endpoints respond cleanly

### Phase progress

| Phase | Status | Detail |
|---|---|---|
| Pre-flight | ✅ | 6A.148 reserved in PHASE_6A_MASTER_INDEX.md; branch off main |
| 1. Domain | ✅ `e5b0a566` | 74 tests GREEN — RefundRequest, RefundRequestLineItem, Registration.CreateRefundRequest, all 12 architect must-fix items folded in |
| 2. Infrastructure | ✅ `1c9f7da8` | EF configs + IRefundRequestRepository + EF migration (xmin token, events schema, FK RESTRICT). Trap caught: `IgnoreUnconfiguredEntities` allowlist had to be extended |
| 3. Application | ✅ `0427bd0e` | 5 command handlers + 2 query handlers + RefundExecutionService + RefundReconciliationService extension. Feature flag config in appsettings |
| 4. API | ✅ `ac16b3eb` | 7 new endpoints on EventsController, all gated by feature flag |
| 5. Backend deploy + smoke | ✅ | Token via password `1qaz!QAZ` (NOT `12!@qwASzx` — plan-file discrepancy noted). T-A/T-B/T-C below pass. |
| 6. Frontend | ✅ `569e1e12`, `1709b2b1` | Types + 7 repository methods + 5 UI components (RefundRequestStatusBanner, RequestRefundDialog, RefundRejectDialog, RefundApprovalDialog, RefundRequestsTab) + page integrations (event detail page + AttendeeManagementTab). OrganizerInitiatedRefundDialog deferred to follow-up. Typecheck + production build clean. |
| 7. Frontend deploy | ✅ run `25992724203` succeeded on SHA `1709b2b1`; staging UI returns HTTP 200 | Operator UAT pending — requires a confirmed paid registration in staging |
| 8. Docs + PR | 🔧 in progress | PROGRESS_TRACKER + master index updated; PR open pending |

### Smoke test evidence (Phase 5 captured)

```
T-A GET /api/events/{guid.empty}/refund-requests/me  → HTTP 204 (null result, endpoint hit)
T-B GET /api/events/{guid.empty}/refund-requests     → HTTP 404 "Event not found"
T-C POST /api/events/{guid.empty}/refund-requests    → HTTP 404 "Registration not found for this event"
```

Proves: (a) feature flag is ON in staging, (b) new tables exist (no EF "table doesn't exist" exception), (c) handlers reach the validation cascade and return clean Problem Details. Container logs show `xmin` column being SELECTed against `registrations` (confirms `UseXminAsConcurrencyToken` is active).

Full E2E tests (T1, T7-T20 from plan §11.4) require a confirmed paid registration in staging with add-on/collection/sponsor purchases — operator setup, not blocked on code.

### Architect items deferred to follow-ups (non-blocking for current deploy)

| Item | Why deferred | Recovery path |
|---|---|---|
| **F4 — Webhook idempotency for line items** | Existing 4 webhook handlers don't know about `RefundRequestLineItem`. For now, the inline "succeeded" Stripe-status response path in `RefundExecutionService` immediately marks line items Refunded, so test-mode smoke passes without webhook changes. | Update IRegistrationWebhookHandler + 3 sibling handlers to find line items by StripeRefundId and call `MarkRefunded`. Idempotency guard already enforced inside the domain entity. |
| **F5 — Legacy CancelRsvp paid-refund branch gating** | Requires careful surgery on a large handler with many entangled paths. New flow is additive — users on the new "Request Refund" UI go to the new endpoint. Legacy callers (none today since frontend hasn't been updated) still work. | Add flag check at the paid-refund branch in `CancelRsvpCommandHandler.cs`; return 410 Gone pointing to new endpoint. |

### Next-session resumption checklist

1. Create the 6 React components per §7.3 of the plan
2. Update [page.tsx](web/src/app/events/[id]/page.tsx) lines 1339–1476 area to use the new flow
3. Add Refund Requests sub-tab + Initiate-Refund action to [AttendeeManagementTab.tsx](web/src/presentation/components/features/events/AttendeeManagementTab.tsx)
4. Deploy frontend via `deploy-ui-staging.yml`
5. Browser UAT for the golden paths (T1, T7-T9, T11 from plan §11.4)
6. Open PR with smoke evidence + screenshots in body
7. Address F4 + F5 follow-ups in a separate PR after the main flow has shipped

---

## Wave 3 — Email surface fix (D7 / D8 / D8b / D9)

**Opened:** 2026-05-18
**Trigger:** Operator UAT defects E1/E2/E3 — refund emails fire with misleading "Refund In Progress" header, no dedicated pending-review email, duplicate per-Sponsor email competes with consolidated decision email.
**Architect validation:** ✅ Paired-validated 2026-05-18 (Plan agent acting as system-architect, two passes).

### The 3 defects (operator-verbatim)

| ID | Defect | Root cause |
|---|---|---|
| E1 | "Refund email arrived BEFORE organizer approved." | [RefundRequestCreatedEventHandler.cs:112](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestCreatedEventHandler.cs#L112) reuses `template-refund-requested` → header reads "Refund In Progress" (legacy 6A.92 vocab). |
| E2 | "No 'request initiated, waiting for approval' email." | Same handler — no dedicated `template-refund-pending-review` exists; "Pending review" is smuggled into `refundReason` body, but visible subject + header stay legacy. |
| E3 | "$255 requested, $125 confirmation email." | After consolidated decision email fires, [SponsorWebhookHandler.cs:225](../src/LankaConnect.Infrastructure/Payments/Services/SponsorWebhookHandler.cs#L225) ALSO fires per-Sponsor "Sponsorship Refund Confirmation" (6A.137 legacy) → operator reads $125 standalone as authoritative. |

### Locked defaults (decided 2026-05-18 under user delegation — no further questions)

| # | Default |
|---|---|
| W3.D1 | **Subjects**: "Refund Request Received — Pending Organizer Review — {EventTitle}" / "Your Refund Decision — {EventTitle}" / "Refund Request Declined — {EventTitle}". |
| W3.D2 | **Organizer-initiated path**: SKIP pending email; send ONE decision email with body copy "Initiated by organizer on your behalf." |
| W3.D3 | **WhatsApp parallel**: defer to a separate ticket — out of scope for Wave 3. |
| W3.D4 | **Standalone (non-workflow) Sponsor refund**: keep existing per-Sponsor email behaviour unchanged. |
| W3.D5 | **D9 detection mechanism**: query `IRefundRequestRepository` (lowest coupling — no schema changes to `Sponsor`). Fail-OPEN on exception (default to sending standalone email if lookup throws). |

### D7 — Templates + contracts (migration MUST land first)

- [ ] Generate migration `Phase6A148d_AddRefundWorkflowEmailTemplates` — INSERTs three rows into `events.email_templates`:
  - `template-refund-pending-review` — subject `"Refund Request Received — Pending Organizer Review — {{EventTitle}}"`; header "Refund Request Received"; body lists per-line items as a structured table; CTA "View on LankaConnect" links to event details.
  - `template-refund-decision` — subject `"Your Refund Decision — {{EventTitle}}"`; header "Refund Decision"; body lists per-line decisions (approved $X / declined / processing) with totals.
  - `template-refund-rejected` — subject `"Refund Request Declined — {{EventTitle}}"`; header "Refund Request Declined"; dedicated `{{RejectionReason}}` placeholder (no body-stuffing).
  - Verify `[Migration("...")]` attribute present in `.Designer.cs` (project rule #8).
  - Test up + down locally on clean DB.
- [ ] Add 3 new constants to `EmailTemplateContract.TemplateNames` in [EmailTemplateContract.cs:104](../src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs#L104):
  - `RefundPendingReview = "template-refund-pending-review"`
  - `RefundDecision = "template-refund-decision"`
  - `RefundRejected = "template-refund-rejected"`
- [ ] New strongly-typed param classes under `src/LankaConnect.Shared/Email/Contracts/`:
  - `RefundPendingReviewEmailParams.cs` — `LineItems: IReadOnlyList<RefundLineItemView>`, `RequesterReason: string?`, `OrganizerContacts: IReadOnlyList<OrganizerContactInfo>?`.
  - `RefundDecisionEmailParams.cs` — `LineItems` (each row carries `Status`, `RequestedAmount`, `ApprovedAmount`), `ApprovedTotal`, `RequestedTotal`, `IsOrganizerInitiated: bool` (drives body copy variant).
  - `RefundRejectedEmailParams.cs` — `LineItems`, `RejectionReason` (mandatory, top-level field), `RequestedTotal`.
- [ ] **TDD list (D7):**
  - `Given_TemplateNamesRegistered_When_ResolvingPendingReview_Then_ReturnsDbRow`
  - `Given_RefundPendingReviewParams_When_ToDictionary_Then_AllPlaceholdersResolved`
  - `Given_RefundDecisionParams_When_LineItemsEmpty_Then_ConstructorThrows`
  - `Given_RefundDecisionParams_When_MixedApprovedRejected_Then_LineItemsHtmlContainsBothBadges`
  - `Given_RefundRejectedParams_When_RejectionReasonNull_Then_ConstructorThrows` (rejection reason is mandatory)
  - `Given_Migration_When_AppliedAndReverted_Then_Idempotent`
- [ ] **Gate:** D7 migration must be applied to staging BEFORE merging D8 — handler code referencing missing template rows fail-silent (handlers swallow exceptions per existing pattern), so attendees would get NO email.

### D8 — Rewire the three new handlers

- [ ] [RefundRequestCreatedEventHandler.cs:112](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestCreatedEventHandler.cs#L112) — replace `RefundEmailParams.CreateRequest(...)` with `RefundPendingReviewEmailParams.Create(...)`. Remove the prose-stuffed `reasonWithBreakdown`; pass structured `LineItems` instead.
- [ ] [RefundRequestApprovedEventHandler.cs:133](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestApprovedEventHandler.cs#L133) — replace with `RefundDecisionEmailParams.Create(...)`. `IsOrganizerInitiated = false` here (this handler only fires on attendee-initiated → organizer-approved path).
- [ ] [RefundRequestRejectedEventHandler.cs:94](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestRejectedEventHandler.cs#L94) — replace with `RefundRejectedEmailParams.Create(...)`. Reason becomes top-level field, not body smush.
- [ ] Legacy [RefundRequestedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequestedEventHandler.cs) — leave intact (only fires on 6A.148 flag OFF branch). Add comment: `// Legacy 6A.92 — only fires when Refund:ApprovalWorkflow:Enabled = false. Remove after 148 ramps to 100%.`
- [ ] **TDD list (D8):**
  - `Given_AttendeeInitiatedRequest_When_Created_Then_SendsPendingReviewTemplate_NotRefundRequested`
  - `Given_OrganizerInitiatedRequest_When_Created_Then_PendingReviewHandlerNotInvoked` (covered by MediatR routing — no shared event type)
  - `Given_ApprovedRequest_When_Handled_Then_SendsRefundDecisionTemplate_WithPerLineBreakdown`
  - `Given_RejectedRequest_When_Handled_Then_SendsRefundRejectedTemplate_WithReasonInBody`
  - `Given_TemplateMissing_When_HandlerRuns_Then_LogsErrorAndDoesNotThrow` (fail-silent guard)
  - `Given_OrganizerContactsPresent_When_PendingReviewEmailSent_Then_OrganizerContactsRenderedInBody`

### D8b — NEW: OrganizerInitiatedRefundCreatedEventHandler

**Why this exists:** `Registration.CreateRefundRequest` with `isOrganizerInitiated=true` raises `OrganizerInitiatedRefundCreatedEvent` ([Registration.cs:1117](../src/LankaConnect.Domain/Events/Registration.cs#L1117)), NOT `RefundRequestCreatedEvent`. Currently nothing subscribes → attendees on the organizer path get zero notification.

- [ ] Create [OrganizerInitiatedRefundCreatedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/OrganizerInitiatedRefundCreatedEventHandler.cs) — mirrors `RefundRequestApprovedEventHandler` structure (attendee resolution, event resolution, organizer-contact attachment).
- [ ] Uses `RefundDecisionEmailParams.Create(..., IsOrganizerInitiated: true, ...)` — body copy variant: "Your organizer has initiated a refund on your behalf. {Per-line breakdown}. Stripe is now processing; you'll receive another email when funds land."
- [ ] **TDD list (D8b):**
  - `Given_OrganizerInitiatedRequest_When_Handled_Then_SendsDecisionEmailWithInitiatedByOrganizerCopy`
  - `Given_OrganizerInitiatedRequest_When_AttendeeNotFound_Then_LogsWarningAndFailsSilent`
  - `Given_OrganizerInitiatedRequest_When_OrganizerContactsPresent_Then_AttachedToEmail`
  - `Given_OrganizerInitiatedRequest_When_NoApprovedLines_Then_DoesNotSendEmail` (defensive — shouldn't happen but assert)

### D9 — Suppress duplicate per-Sponsor email when workflow-owned

- [ ] Add method to [IRefundRequestRepository.cs:60](../src/LankaConnect.Domain/Events/Repositories/IRefundRequestRepository.cs#L60):
  ```csharp
  Task<bool> ExistsWorkflowLineItemForSponsorAsync(
      Guid sponsorId, string stripeRefundId, CancellationToken ct);
  ```
  Predicate: `Type == Sponsor && ReferenceId == sponsorId && StripeRefundId == stripeRefundId`. `AsNoTracking().AnyAsync()`.
- [ ] EF implementation in `RefundRequestRepository.cs`.
- [ ] Inject `IRefundRequestRepository` into [SponsorWebhookHandler.cs](../src/LankaConnect.Infrastructure/Payments/Services/SponsorWebhookHandler.cs#L225).
- [ ] At line 225 (fire-and-forget email block), add guard:
  ```csharp
  try
  {
      var isWorkflowOwned = await _refundRequestRepository
          .ExistsWorkflowLineItemForSponsorAsync(sponsor.Id, refundEvent.RefundId, ct);
      if (isWorkflowOwned)
      {
          _logger.LogInformation(
              "[6A.148.D9] Sponsor refund email suppressed — workflow-owned. SponsorId={SponsorId} RefundId={RefundId}",
              sponsor.Id, refundEvent.RefundId);
          return;
      }
  }
  catch (Exception ex)
  {
      // FAIL-OPEN: if lookup fails, default to sending the legacy email
      _logger.LogWarning(ex,
          "[6A.148.D9] Workflow-owned lookup threw; defaulting to send standalone email. SponsorId={SponsorId}",
          sponsor.Id);
  }
  // ... existing email-send block continues
  ```
- [ ] **TDD list (D9):**
  - `Given_SponsorRefund_WithMatchingWorkflowLineItem_When_WebhookHandled_Then_StandaloneEmailSkipped`
  - `Given_SponsorRefund_WithNoWorkflowLineItem_When_WebhookHandled_Then_StandaloneEmailSent` (legacy path preserved — REGRESSION GUARD)
  - `Given_SponsorRefund_WhenWorkflowLookupThrows_Then_DefaultsToSendingEmail_AndLogsWarning` (fail-open guardrail)
  - `Given_TwoSponsorsRefundedInOneWorkflow_When_BothWebhooksFire_Then_BothStandaloneEmailsSkipped`
  - `Given_SponsorRefund_WithDifferentStripeRefundId_Then_StandaloneEmailSent` (no false positive on cross-RR collision)
  - `Given_RepositoryReturnsTrue_When_SkipLogged_Then_LogIncludesRefundRequestId` (observability)

### API smoke matrix (post-staging-deploy verification)

Token via password `1qaz!QAZ` (NOT `12!@qwASzx`, per memory `reference_staging_creds.md`).

```bash
TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' \
  | jq -r '.token')
```

| ID | Action | Expected email | Must NOT receive |
|---|---|---|---|
| W3.T1 | Attendee `POST /api/events/{id}/refund-requests` (Ticket+Sponsor+2 AddOns) | Subject: **"Refund Request Received — Pending Organizer Review — {ET}"** | Any email with "Refund In Progress" |
| W3.T2 | Organizer `POST /api/events/{id}/refund-requests/{rrId}/approve` (mixed: 1 sponsor approved $125, 2 AddOns declined, 1 AddOn approved $15) | Subject: **"Your Refund Decision — {ET}"** (ONE email, per-line table shows $125+$15 approved, $14 declined) | Standalone "Sponsorship Refund $125" email (D9 must suppress) |
| W3.T3 | Organizer `POST /api/events/{id}/refund-requests/{rrId}/reject` | Subject: **"Refund Request Declined — {ET}"**, body has dedicated rejection-reason field | "Refund In Progress" / "Your Refund Decision" |
| W3.T4 | Organizer `POST /api/events/{id}/refund-requests/organizer-initiated` (full refund) | Subject: **"Your Refund Decision — {ET}"** (body copy: "Initiated by organizer on your behalf") | "Pending Organizer Review" |
| W3.T5 | Trigger a standalone sponsor refund via Stripe dashboard (sponsor NOT in any RefundRequest) | Subject: "Your Sponsorship Refund - {ET}" (legacy path preserved — REGRESSION GUARD) | Any Wave 3 workflow email |
| W3.T6 | Complete a workflow approval; Stripe webhook `charge.refunded` fires for sponsor line | Existing "Refund Completed" money-landed email (unchanged — fires once per `Processing → Completed` transition) | Duplicate "Sponsorship Refund $X" standalone |
| W3.T7 | `POST /approve` with all-zero approvedAmounts | (HTTP 400, no email) | Any refund email |
| W3.T8 | Attendee `POST .../refund-requests/me/withdraw` | (no email — confirm silence; current handler nonexistent) | "Refund Request Declined" |

For each test:
1. Fire the curl command.
2. Check operator inbox at `niroshhh@gmail.com` within 60s.
3. Capture subject + first 200 chars of body.
4. Document outcome in PR description.

### Container log audit (post-deploy)

```bash
az containerapp logs show --name lankaconnect-api-staging --tail 500 \
  | Select-String -Pattern '6A.148.c EMAIL|6A.148.D9|template-refund'
```

Expected log lines:
- `[6A.148.c EMAIL] RefundRequestCreated email sent: RrId={...} Email={...}` (D8 rewire confirmed)
- `[6A.148.D9] Sponsor refund email suppressed — workflow-owned. SponsorId={...}` (D9 suppression confirmed for W3.T2)
- No `EXCEPTION` lines outside expected validation paths.

### Risk register

| Risk | Probability | Guardrail |
|---|---|---|
| **D9 false-positive** (legacy standalone sponsor refund suppressed → sponsor receives no email at all) | Medium | Fail-OPEN on lookup exception + W3.T5 regression guard test. |
| **D7 migration fails to land on staging before D8 merges** → all 4 handlers fail-silent, attendees get zero emails. | Medium | Gate: D8 PR must check migration applied at top of CI; staging deploy of D7 must precede D8 merge. Document in PR description. |
| **Subject-line copy regression** if downstream email service caches the old `template-refund-requested` rendering | Low | Old template stays in DB; the 3 new rows are additive. No risk of overwriting. |
| **In-flight Pending rows at deploy time** receive new decision/rejected email on next state change — behaviour change for users mid-flight | Acceptable | Improvement, not regression. No data migration. |
| Test password drift between docs and reality | Low (already burned us once) | Master TODO explicitly says `1qaz!QAZ` per memory `reference_staging_creds.md`. |

### Files to touch — final list

**Backend (new + edit):**
- NEW: `src/LankaConnect.Infrastructure/Data/Migrations/2026MMDDhhmmss_Phase6A148d_AddRefundWorkflowEmailTemplates.cs` (+ `.Designer.cs`)
- NEW: `src/LankaConnect.Shared/Email/Contracts/RefundPendingReviewEmailParams.cs`
- NEW: `src/LankaConnect.Shared/Email/Contracts/RefundDecisionEmailParams.cs`
- NEW: `src/LankaConnect.Shared/Email/Contracts/RefundRejectedEmailParams.cs`
- NEW: `src/LankaConnect.Application/Events/EventHandlers/RefundRequests/OrganizerInitiatedRefundCreatedEventHandler.cs`
- EDIT: [src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs:104](../src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs#L104) — 3 new `TemplateNames` constants
- EDIT: [src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestCreatedEventHandler.cs:112](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestCreatedEventHandler.cs#L112)
- EDIT: [src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestApprovedEventHandler.cs:133](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestApprovedEventHandler.cs#L133)
- EDIT: [src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestRejectedEventHandler.cs:94](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestRejectedEventHandler.cs#L94)
- EDIT: [src/LankaConnect.Domain/Events/Repositories/IRefundRequestRepository.cs:60](../src/LankaConnect.Domain/Events/Repositories/IRefundRequestRepository.cs#L60) — add `ExistsWorkflowLineItemForSponsorAsync`
- EDIT: `src/LankaConnect.Infrastructure/Data/Repositories/RefundRequestRepository.cs` — implement new method
- EDIT: [src/LankaConnect.Infrastructure/Payments/Services/SponsorWebhookHandler.cs:225](../src/LankaConnect.Infrastructure/Payments/Services/SponsorWebhookHandler.cs#L225) — inject repo + add D9 guard

**Tests (new):**
- `tests/LankaConnect.Application.Tests/Events/EventHandlers/RefundRequests/RefundRequestCreatedEventHandlerTests.cs` (extend)
- `tests/LankaConnect.Application.Tests/Events/EventHandlers/RefundRequests/RefundRequestApprovedEventHandlerTests.cs` (extend)
- `tests/LankaConnect.Application.Tests/Events/EventHandlers/RefundRequests/RefundRequestRejectedEventHandlerTests.cs` (extend)
- NEW: `tests/LankaConnect.Application.Tests/Events/EventHandlers/RefundRequests/OrganizerInitiatedRefundCreatedEventHandlerTests.cs`
- NEW: `tests/LankaConnect.Application.Tests/Shared/Email/Contracts/RefundPendingReviewEmailParamsTests.cs`
- NEW: `tests/LankaConnect.Application.Tests/Shared/Email/Contracts/RefundDecisionEmailParamsTests.cs`
- NEW: `tests/LankaConnect.Application.Tests/Shared/Email/Contracts/RefundRejectedEmailParamsTests.cs`
- EXTEND: `tests/LankaConnect.IntegrationTests/Payments/SponsorWebhookHandlerTests.cs` (add D9 guard tests)

### Wave 3 phase gates

- [x] **G1 — Pre-code**: Master TODO Wave 3 section committed.
- [x] **G2 — D7 GREEN**: 26 D7 tests pass locally (3 new test files: RefundPendingReviewEmailParamsTests, RefundDecisionEmailParamsTests, RefundRejectedEmailParamsTests); Infrastructure builds clean; `[Migration]` attribute verified in Designer.cs.
- [x] **G3 — D7 staging**: D7 commit `2c06b62b` pushed; `deploy-staging.yml` run `26054943592` succeeded; container revision `0001678` healthy (`/api/health` → 200); `communications.email_templates` direct DB query confirms 3 new rows present with correct subjects:
  - `template-refund-pending-review` → "Refund Request Received — Pending Organizer Review — {{EventTitle}}"
  - `template-refund-decision` → "Your Refund Decision — {{EventTitle}}"
  - `template-refund-rejected` → "Refund Request Declined — {{EventTitle}}"
- [ ] **G4 — D8 + D8b GREEN**: 6+4 handler tests pass; full Application suite GREEN; build zero warnings.
- [ ] **G5 — D9 GREEN**: 6 D9 tests pass; SponsorWebhookHandler regression suite passes.
- [ ] **G6 — Staging deploy (D8 + D8b + D9)**: pushed; `deploy-staging.yml` succeeds; W3.T1–W3.T8 smoke matrix passes; email evidence captured.
- [ ] **G7 — PR**: open PR off `feat/phase-6a-148-refund-approval-workflow` with W3.T1–W3.T8 evidence + screenshots of new email headers.

### D7 ship status (2026-05-18)

**Status:** committed `2c06b62b`, pushed, staging deploy dispatched (run `26054943592`).

**What landed:**
- 3 `TemplateNames` constants in [EmailTemplateContract.cs](../src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs#L107) + 3 parameter regions.
- New email-only view record [RefundLineItemView.cs](../src/LankaConnect.Shared/Email/Contracts/RefundLineItemView.cs).
- New HTML builder [RefundLineItemsHtmlBuilder.cs](../src/LankaConnect.Shared/Email/Helpers/RefundLineItemsHtmlBuilder.cs) — `BuildRequestedListHtml` + `BuildDecisionListHtml` with status-coded badges.
- 3 strongly-typed `IEmailParameters` classes: [RefundPendingReviewEmailParams.cs](../src/LankaConnect.Shared/Email/Contracts/RefundPendingReviewEmailParams.cs), [RefundDecisionEmailParams.cs](../src/LankaConnect.Shared/Email/Contracts/RefundDecisionEmailParams.cs), [RefundRejectedEmailParams.cs](../src/LankaConnect.Shared/Email/Contracts/RefundRejectedEmailParams.cs).
- EF migration `Phase6A148D7_AddRefundWorkflowEmailTemplates` ([20260518185353_Phase6A148D7_AddRefundWorkflowEmailTemplates.cs](../src/LankaConnect.Infrastructure/Data/Migrations/20260518185353_Phase6A148D7_AddRefundWorkflowEmailTemplates.cs)) — idempotent `INSERT ... WHERE NOT EXISTS` for 3 template rows + `Down()` removes by name.

**Tests:** 26/26 GREEN. 5 pre-existing failures in `BaseParameterContractsTests.EventEmailParams_*DateCorrectly` are unrelated (timezone/culture date format issues; none of my files touch `EventEmailParams` or date helpers).

**Adaptations from architect's exact TDD list** (project-convention reasons):
- Architect: `Given_RefundDecisionParams_When_LineItemsEmpty_Then_ConstructorThrows`.
- Shipped: `Validate_ShouldFail_WhenLineItemsEmpty` (Validate() with errors list, not throwing constructor) — matches existing `RefundEmailParams` pattern. Same intent (catch empty line items at pre-send), different mechanism.
- Architect: `Given_RefundRejectedParams_When_RejectionReasonNull_Then_ConstructorThrows`.
- Shipped: `Validate_ShouldFail_WhenRejectionReasonEmpty` + `..._WhenRejectionReasonWhitespace` — same intent (RejectionReason mandatory), Validate-based.

**Staging verification (D7) — ALL GREEN:**
- [x] `deploy-staging.yml` run `26054943592` succeeded.
- [x] Container revision `lankaconnect-api-staging--0001678` provisioned + ready (system logs `RevisionReady` event 2026-05-18 19:22:50Z).
- [x] `GET /api/health` returns `HTTP 200 {"status":"Healthy"}` — if the D7 migration had failed, EF Core's `Database.Migrate()` would have thrown during startup and the container would never have become healthy.
- [x] Direct DB query (via `az postgres flexible-server execute`) confirms 3 new rows present in `communications.email_templates` with correct subject lines:

  ```sql
  SELECT name, subject_template FROM communications.email_templates
  WHERE name IN ('template-refund-pending-review', 'template-refund-decision', 'template-refund-rejected')
  ORDER BY name;
  ```

  ```
  template-refund-decision        | Your Refund Decision — {{EventTitle}}
  template-refund-pending-review  | Refund Request Received — Pending Organizer Review — {{EventTitle}}
  template-refund-rejected        | Refund Request Declined — {{EventTitle}}
  ```

- [x] No regression — health endpoint green; existing email surface unchanged (no handler rewires yet).

**Next:** G4/G5/G6 → D8 (rewire 3 existing handlers) + D8b (new OrganizerInitiatedRefundCreatedEventHandler) + D9 (suppress duplicate per-Sponsor email when workflow-owned).

### Order of operations

1. D7 (templates + contracts + params) → commit → deploy to staging → verify migration applied.
2. D8 + D8b in one commit (handler rewires share the same param contracts and ship together — testing simpler).
3. D9 in a separate commit (touches a different file; isolates risk).
4. Run W3.T1–W3.T8 against staging.
5. Update [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md), [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md), [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md).
6. Open PR.
