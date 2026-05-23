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
- [x] **G4 — D8 + D8b GREEN**: 6 tests in RefundLifecycleEmailHandlerTests pass — covers attendee-initiated → RefundPendingReviewEmailParams binding, organizer-approved → RefundDecisionEmailParams with IsOrganizerInitiated=false, rejected → RefundRejectedEmailParams with RejectionReason first-class field, NEW D8b → RefundDecisionEmailParams with IsOrganizerInitiated=true, and fail-silent guards on both. Full Application suite: 2709 passed, 0 failed, 6 skipped (pre-existing).
- [x] **G5 — D9 GREEN**: 6 D9 tests pass in `SponsorWebhookHandlerD9Tests` — covers WorkflowOwnedRefund suppresses, NonWorkflowRefund regression guard (legacy path preserved), WorkflowLookupThrows fail-OPEN, TwoSponsorsRefundedInOneWorkflow, DifferentStripeRefundId no false positive on cross-refund collision, SponsorNotFound returns early before guard.
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

---

### D8 + D8b ship status (2026-05-18)

**Status:** committed `a2cd233e`, pushed, staging deploy dispatched.

**What landed:**
- [RefundRequestCreatedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestCreatedEventHandler.cs) rewired from `RefundEmailParams.CreateRequest` (template-refund-requested, "Refund In Progress" header) → `RefundPendingReviewEmailParams.Create` (template-refund-pending-review, "Refund Request Received" header). Fixes E1+E2.
- [RefundRequestApprovedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestApprovedEventHandler.cs) rewired → `RefundDecisionEmailParams.Create(IsOrganizerInitiated: false, ...)`. Per-line decision badges now render from structured `RefundLineItemView` list instead of body-stuffed text.
- [RefundRequestRejectedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestRejectedEventHandler.cs) rewired → `RefundRejectedEmailParams.Create(...)`. `RejectionReason` is now a top-level mandatory field, not buried inside a prose `RefundReason`.
- NEW [OrganizerInitiatedRefundCreatedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/OrganizerInitiatedRefundCreatedEventHandler.cs) — D8b. Subscribes to `OrganizerInitiatedRefundCreatedEvent` which previously had ZERO subscribers (organizer-initiated refunds sent no attendee email). Reuses `RefundDecisionEmailParams` with `IsOrganizerInitiated: true` so the template renders the body-copy variant.
- NEW [RefundLineItemViewMapper.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundLineItemViewMapper.cs) — single source of truth for Domain `RefundRequestLineItem` → email-only `RefundLineItemView` mapping. Keeps type/status display strings aligned with the HTML builder's badge colour table.
- Legacy [RefundRequestedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequestedEventHandler.cs) gets a documented "DO NOT EXTEND" deprecation note — only fires on `Refund:ApprovalWorkflow:Enabled=false` rollback branch.

**Log prefix updates:**
- `[6A.148.c EMAIL]` → `[6A.148.D8 EMAIL]` on the 3 rewired handlers.
- `[6A.148.D8b EMAIL]` on the new handler.
- Each success log now includes `Lines={count} Template={name}` so post-deploy audit can verify the intended template is in use without firing a real email.

**Tests:** 6 new in RefundLifecycleEmailHandlerTests — full suite 2709/2715 GREEN, no regressions.

**Adaptations from architect's TDD list:**
- Combined 4 architect-suggested test classes into ONE file (RefundLifecycleEmailHandlerTests) covering all 4 handlers — keeps the shared test fixture (Setup, factory helpers) in one place. The load-bearing assertions (template-name binding + IsOrganizerInitiated flag) are pinned per handler.
- Architect's `Given_TemplateMissing_When_HandlerRuns_Then_LogsErrorAndDoesNotThrow` covered transitively by the fail-silent test (`Created_FailSilentOnException`) — both rely on the same `try/catch` pattern that swallows ANY exception during email send.

**Currency type fix found during build:** `Money.Currency` is the `Currency` enum, not `string` — added `.ToString()` before `?? "USD"` fallback across all 4 handlers.

**Pending verification (D8+D8b staging):**
- [ ] `deploy-staging.yml` run reaches success.
- [ ] Container healthy + no startup exceptions from `[6A.148.D8*]` handler files.
- [ ] When operator triggers a refund flow, container logs show `[6A.148.D8 EMAIL] RefundRequestCreated email sent: ... Template=template-refund-pending-review` (proves D7 templates resolve + D8 rewire is live).
- [ ] Operator inbox shows the new subjects on next refund: "Refund Request Received — Pending Organizer Review" / "Your Refund Decision" / "Refund Request Declined". NO more "Refund In Progress" header.

---

### D9 ship status (2026-05-18)

**Status:** committed `7119fce2`, pushed, staging deploy dispatched (run `26066296386`).

**What landed:**
- [IRefundRequestRepository.cs](../src/LankaConnect.Domain/Events/Repositories/IRefundRequestRepository.cs) gains `ExistsWorkflowLineItemForSponsorAsync(sponsorId, stripeRefundId, ct)` — predicate matches `Type == Sponsor && ReferenceId == sponsorId && StripeRefundId == stripeRefundId`. Defensive early-return false on empty stripeRefundId.
- [RefundRequestRepository.cs](../src/LankaConnect.Infrastructure/Data/Repositories/RefundRequestRepository.cs) — EF implementation: `AsNoTracking().AnyAsync(...)` against `RefundRequestLineItems` DbSet. Single index hit.
- [SponsorWebhookHandler.cs](../src/LankaConnect.Infrastructure/Payments/Services/SponsorWebhookHandler.cs#L225) — injected `IRefundRequestRepository`; new fail-OPEN guard right before the fire-and-forget email block. Suppresses standalone email + logs `[Phase 6A.148.D9] Sponsor refund standalone email SUPPRESSED — workflow-owned` when the lookup returns true; falls through (sends legacy email) + logs warning when the lookup throws.

**Tests:** [SponsorWebhookHandlerD9Tests.cs](../tests/LankaConnect.Infrastructure.Tests/Payments/SponsorWebhookHandlerD9Tests.cs) — 6/6 GREEN.
Load-bearing assertion: `IServiceScopeFactory.CreateScope()` invocation count — proves whether the fire-and-forget email path even started.

| Test | Expected | Verified |
|---|---|---|
| WorkflowOwnedRefund | CreateScope Times.Never | ✅ |
| NonWorkflowRefund (regression guard) | CreateScope Times.Once | ✅ |
| WorkflowLookupThrows (fail-OPEN) | CreateScope Times.Once | ✅ |
| TwoSponsorsInOneWorkflow | both suppressed | ✅ |
| DifferentStripeRefundId (no false positive) | predicate scoped, CreateScope Times.Once | ✅ |
| SponsorNotFound | early-return before guard | ✅ |

**Race-condition note:** the 3 tests that assert `CreateScope Times.Once` (legacy path) needed `await Task.Delay(100)` after the handler call to let the queued `Task.Run` actually execute before Verify — same race-mitigation pattern as the existing `WhatsAppEventHandlerTests` (their `Task.Delay(500)` for similar fire-and-forget).

**Regression check:** Application suite ran post-D9 — 2708 passed, 1 unrelated WhatsApp flake (`CommitmentUpdated_Handle_ValidData_SendsWhatsApp` — passes in isolation; pre-existing race with its own `Task.Delay(500)` occasionally too short on loaded CI). My code touches Domain interface + Infrastructure repo + Infrastructure webhook handler — none of which the WhatsApp test reaches.

**Pending verification (D9 staging):**
- [ ] `deploy-staging.yml` run `26066296386` reaches success.
- [ ] Container healthy + no startup exceptions (the new constructor parameter must be wired by DI, otherwise startup fails).
- [ ] When operator's next refund triggers a workflow-owned sponsor refund, container logs show `[Phase 6A.148.D9] Sponsor refund standalone email SUPPRESSED — workflow-owned` AND NO "Sponsorship Refund Confirmation" email lands in the attendee inbox.
- [ ] Standalone (non-workflow) sponsor refund still sends the legacy email — REGRESSION GUARD verified in tests, also confirmable in prod via any non-workflow refund path.

**Wave 3 complete after D9 verifies.** Ready for PR review.

### Order of operations

1. D7 (templates + contracts + params) → commit → deploy to staging → verify migration applied.
2. D8 + D8b in one commit (handler rewires share the same param contracts and ship together — testing simpler).
3. D9 in a separate commit (touches a different file; isolates risk).
4. Run W3.T1–W3.T8 against staging.
5. Update [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md), [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md), [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md).
6. Open PR.

---

## Wave 4 — End-to-End Architecture Review + Gap Closure

**Opened:** 2026-05-19
**Trigger:** Operator UAT after Wave 3 surfaced two new defects (F1: no decision/completion email after Approve; F2: AddOnPurchase entities still show Completed status after workflow refund completes). User asked for full end-to-end review of all 5 checkout + refund paths rather than narrow F1/F2 RCA.
**Architect validation:** Plan agent (system-architect role) reviewed full architectural map from Explore agent + live staging DB evidence + screenshots; 10 gaps identified.

### Empirical evidence (live staging, 2026-05-19)

Event `ad8903c4-e98e-49dd-b44e-d89f916c49dc`, RefundRequest `98712d40-ef05-42c5-9566-1bd2f82edff1` (operator scenario, $198 across 1 Ticket + 6 Add-Ons):
- `RefundRequest.status = 3 (Completed)`, `reviewed_at = 2026-05-19 01:40:49Z` — organizer DID approve
- All 7 `RefundRequestLineItem` rows: `status = 4 (Refunded)`, `stripe_refund_id` populated, `processed_at` set — Stripe DID process
- 6 underlying `AddOnPurchase` rows: `status = "Completed"`, `refunded_at = null` — **F2 EMPIRICALLY CONFIRMED**
- 2 May-14 AddOnPurchase rows refunded via legacy path: `status = "Refunded"`, `refunded_at` set — legacy path works; only new workflow path broken
- `communications.email_messages` empty for refund template names — email audit gap (cannot confirm/deny F1 from this table alone)

### ASCII flow diagrams

**Diagram A — Checkout (any of 5 categories):**

```
 USER (browser)                       API                          STRIPE
  |  POST /events/{id}/{category}     |                              |
  |---------------------------------->|                              |
  |                                   |  CreateCheckoutSession        |
  |                                   |  + metadata.payment_type      |
  |                                   |----------------------------->|
  |<--303 Redirect to session URL ----|                              |
  |---------------- pay --------------+----------------------------->|
  |<--302 success_url-----------------+----------------------------->|
  |                                                                  |
  |                          checkout.session.completed              |
  |<-----------------------------------+----------------------------|
                                       v
   +-----------------+ +------------------+ +-----------------+ +----------------+ +-----------------+
   | Registration WH | | AddOnPurchase WH | | Sponsor WH      | | Collection WH  | | Donation WH     |
   +--------+--------+ +---------+--------+ +--------+--------+ +-------+--------+ +-------+---------+
            v                    v                   v                  v                  v
       Registration.        AddOnPurchase.      Sponsor.          Collection.       Donation.
       MarkAsConfirmed()    CompletePayment()   CompletePayment() MarkAsPaid()      MarkAsCompleted()
            |                    |                   |                  |                  |
            v                    v                   v                  v                  v
       RegistrationConfirmed AddOnPurchase       SponsorPayment     Collection         Donation
       Event --> email       CompletedEvent      CompletedEvent     CompletedEvent     CompletedEvent
                             --> email           --> email          --> email          --> email
```

All 5 checkout paths healthy in production.

**Diagram B — Refund (6A.148 approval workflow + legacy):**

```
ATTENDEE or ORGANIZER                  API                            STRIPE
 | POST /events/{id}/refunds            |                                |
 |------------------------------------->|                                |
 |                                      | CreateRefundRequestCommand     |
 |                                      | RefundRequest created          |
 |                                      | raise RefundRequestCreatedEvent|
 |                                      |        (or OrganizerInitiated) |
 |                                      v                                |
 |                              D7 "pending-review" email                |
 |
 | (organizer side) POST /refund-requests/{id}/approve                   |
 |--------------------------------------|                                |
 |                                      | ApproveRefundRequestCommand    |
 |                                      | RefundRequest.Approve()        |
 |                                      | raise RefundRequestApprovedEvt |
 |                                      |   -> D8 decision email <-- F1 NOT ARRIVING (G3)
 |                                      |                                |
 |                                      | RefundExecutionService         |
 |                                      | per line: Stripe.CreateRefund  |
 |                                      |------------------------------->|
 |                                                  charge.refunded      |
 |                                      |<-------------------------------|
 |                                      v
 |                              PaymentsController.HandleChargeRefunded
 |                              switch(refund_type | payment_type):
 |          +----- "registration" -> RegistrationWebhookHandler  (no D9 dedupe — G4)
 |          +----- "sponsor" ------> SponsorWebhookHandler        (D9 dedupe shipped)
 |          +----- "collection" ---> CollectionWebhookHandler    (no D9 dedupe — G4)
 |          +----- "add_on_*" -----> ** NO-OP RETURN (PaymentsController:647-654) ** <-- F2 / G1
 |                   AddOnPurchase entity NEVER transitions Refunded
 |                   No domain event raised, no money-landed email
 |
 | (Reject) RefundRequest.Reject()   -> D7 rejected email
 | (Withdraw) RefundRequest.Withdraw() -> ** NO HANDLER ** <-- G2
 |
 | (legacy flag-OFF) AddOnRefundService DOES call MarkAsRefunded()
```

### Gap inventory (10 gaps)

| ID | Name | Class | Sev | Location | Root cause |
|----|------|-------|-----|----------|------------|
| G1 | AddOnPurchase `charge.refunded` NO-OP | Backend | CRITICAL | [PaymentsController.cs:647-654](../src/LankaConnect.API/Controllers/PaymentsController.cs#L647) | 6A.136 comment claims AddOnRefundService handles inline; 6A.148 RefundExecutionService bypasses it; webhook is the only signal and it's dropped |
| G2 | RefundRequestWithdrawnEvent has no handler | Email | HIGH | event raised in Domain, no handler file | Handler never authored in Wave 3 scope |
| G3 | F1: D8 decision email not arriving on Approve | Email | CRITICAL | [RefundRequestApprovedEventHandler.cs:145](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/RefundRequestApprovedEventHandler.cs#L145) | Possible: domain event dispatch issue, or email service Success=false swallowed, or Validate throws on edge case. Needs instrumentation. |
| G4 | D9-style dedupe missing on Registration / Collection / AddOn handlers | Email | HIGH | RegistrationWebhookHandler / CollectionWebhookHandler / new G1 handler | Wave 3 D9 was scoped to sponsor only |
| G5 | Per-category "money landed" template overlap | Email | MEDIUM | `template-refund-completed` (legacy) + `template-refund-decision` (D8) | D8 added in parallel, not replacing legacy |
| G6 | OrganizerInitiatedRefundCreatedEvent may also miss decision email | Email | MEDIUM | [OrganizerInitiatedRefundCreatedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RefundRequests/OrganizerInitiatedRefundCreatedEventHandler.cs) | Same dispatch path as G3 |
| G7 | ExistsWorkflowLineItemForSponsorAsync re-throws instead of fail-OPEN | Backend | LOW | [RefundRequestRepository.cs:181](../src/LankaConnect.Infrastructure/Data/Repositories/RefundRequestRepository.cs#L181) | Contract drift — caller catches, but defensive doubling |
| G8 | Idempotency of re-fired `charge.refunded` not asserted | Backend | MEDIUM | All four `HandleChargeRefundedAsync` methods | No `stripe_webhook_events` deduper table |
| G9 | UI ignores `RefundRequestLineItem.Status` when rendering add-ons tab | UI | HIGH | `web/src/app/events/[id]/manage/attendees/*` | Same root as G1 — UI reads entity status which never updates |
| G10 | RefundCompletedEvent aggregates `Registration.AddOnRefundAmount` not line items | Backend | MEDIUM | [RefundCompletedEventHandler.cs:93](../src/LankaConnect.Application/Events/EventHandlers/RefundCompletedEventHandler.cs#L93) | 6A.135 predates 6A.148 line-item model |

### Wave 4 fix plan (ordered — strict dependencies)

Each step ships behind feature-flag `RefundWorkflow_Wave4_<letter>` for instant rollback.

| Step | Effort | Title | Closes | Depends |
|---|---|---|---|---|
| W4.D10 | 1d | G3/G6 instrumentation — WARN on email-failure, ERROR on Validate-failure, event-count log on Approve commit | G3, G6 | — |
| W4.D11 | 2d | G1 AddOnPurchase webhook handler — new `HandleChargeRefundedAsync` + PaymentsController dispatch + `AddOnPurchaseRefundedEvent` | G1 | D10 |
| W4.D11b | 0.5d | G9 UI sync — defensive secondary check on `RefundRequestLineItem.Status` | G9 | D11 |
| W4.D12 | 2d | G4 generalised dedupe — rename to `ExistsWorkflowLineItemAsync(type, refId, refundId)`, apply at all 4 handlers | G4 | D11 |
| W4.D13 | 1d | G2 `RefundRequestWithdrawnEventHandler` + new `template-refund-withdrawn` | G2 | D12 |
| W4.D14 | 1d | G5 product decision — keep both emails with distinct subjects; suppress per-category duplicates via G4 | G5 | D12, Q1 user sign-off |
| W4.D15 | 1.5d | G8 webhook idempotency — new `stripe_webhook_events` deduper table + middleware | G8 | D14 |
| W4.D16 | 0.5d | G10 RefundCompletedEvent payload — sum from line items not Registration field | G10 | D15 |
| W4.D17 | 0.25d | G7 repo fail-OPEN — replace `throw` with `return false` | G7 | any time |

**Total effort:** ~10 working days.

### API test matrix (Wave 4 — W4.T1–W4.T15)

Token via password `1qaz!QAZ`. JSON body shapes are illustrative.

| # | Scenario | Expected DB / Log / Email |
|---|---|---|
| W4.T1 | Registration-only refund happy path | refund_requests.status=3, line=4, registration=Refunded; D8 decision + legacy completion email |
| W4.T2 | **AddOn-only refund (F2 verification)** | **add_on_purchases.status='Refunded', refunded_at NOT NULL** ← F2 fix verifier |
| W4.T3 | Collection-only refund | collections.status='Refunded'; one dedupe-passed email |
| W4.T4 | Sponsor-only refund (D9 regression guard) | sponsors.status='Refunded'; D9 SUPPRESSED log; exactly one email |
| W4.T5 | Mixed Ticket+6×AddOn (UAT scenario replay) | all line items + entities Refunded; exactly the expected emails |
| W4.T6 | Reject path | status=4 (Rejected); no Stripe call; "Refund Request Declined" email |
| W4.T7 | **Withdraw path (G2 new handler)** | status=5 (Withdrawn); "Your refund request was withdrawn" email |
| W4.T8 | Organizer-initiated refund | rr created in Approved; "Refund Decision" with IsOrganizerInitiated=true variant |
| W4.T9 | Idempotency — same stripe_event_id twice | second hit logs `[Webhook-Idempotent-Skip]`; exactly one email |
| W4.T10 | Distinct event-ids same charge | both process; one email (dedupe by stripe_refund_id) |
| W4.T11 | Approve with all-zero approved lines | flips to Rejected; no Stripe |
| W4.T12 | Partial approval (3 of 5 lines) | 3 Stripe calls; one D8 email summarising approved+declined |
| W4.T13 | Webhook race (refund A during dispatch of B) | no cross-talk; both refunds Completed |
| W4.T14 | Legacy `/rsvp/withdraw-refund` (flag-OFF regression) | inline `AddOnRefundService` runs; add_on_purchases Refunded via legacy path |
| W4.T15 | Lookup repo exception (fail-OPEN) | legacy email arrives; entity still transitions; warning log |

### Risk register

1. **Duplicate emails after G1 lands alone.** Must merge G1 (D11) and G4 dedupe (D12) under the same flag — never G1 without G4.
2. **Idempotency race during webhook retry mid-flight.** Wrap entity transition in `Result.Failure` tolerance for already-Refunded.
3. **Regressing legacy flag-OFF path during G4 rename.** Keep old method name as shim. T14 protects.
4. **Webhook ordering with inline `MarkCompletedIfAllSettled`.** RefundExecutionService may roll Completed BEFORE webhooks arrive; entity transitions still must process.
5. **F1 may be transport-layer (SendGrid/SMTP), not dispatch.** If D10 reveals `result.Success=false` with no exception → D10b spike on transport health.

### Open product questions

| # | Question | Default |
|---|---|---|
| Q1 | Email semantics: keep both "Refund Decision" + "Refund Completed" OR collapse? | Keep both — distinct facts, distinct timestamps |
| Q2 | Organizer notification on attendee Withdraw — courtesy note OR silent? | Silent |
| Q3 | Partial-approval UX — one consolidated email OR per-line? | Consolidated (current D8) |
| Q4 | G7 fail-OPEN contract — keep duplicates on lookup failure OR circuit breaker? | Keep fail-OPEN; alert on WARN frequency |

### Q1-Q4 — answers locked (user-confirmed 2026-05-19)

| # | Answer |
|---|---|
| Q1 | Both emails — "Your Refund Decision" at Approve + legacy "Refund Completed" at money-landed. Distinct facts, distinct timestamps. |
| Q2 | Silent withdraw notification to organizer. **Verified:** withdraw button exists in FE (`RefundRequestStatusBanner.tsx:106-114`) and is wired into [page.tsx](../web/src/app/events/[id]/page.tsx#L1264). Visible only during Pending state — operator missed it because their own 60-second approval closed the Pending window. Not a gap. |
| Q3 | ONE consolidated email with full per-line table (current D8). |
| Q4 | Fail-OPEN + alert on WARN frequency in Azure Application Insights. |

### Wave 4 phase gates

- [x] **G0** — User approval of plan + Q1-Q4 defaults
- [x] **G1** — D10 instrumentation deployed (commit `fbafe550`, deploy `26118274603` GREEN); next operator refund will produce diagnostic logs to pinpoint F1
- [x] **G2** — D11 AddOnPurchase webhook handler + D12 generalized dedupe shipped in one bundle (commit `296026d4`, deploy `26120329599` GREEN; container health 200; 9/9 D11+D12 tests pass); see W4.D11+D12 ship status below
- [ ] **G3** — D11+D12 staging-verified end-to-end: next operator refund involving AddOn rows → DB query confirms `add_on_purchases.status='Refunded', refunded_at NOT NULL`
- [x] **G4** — D13 RefundRequestWithdrawnEventHandler + template-refund-withdrawn shipped (commit `6fc376ef`, deploy `26126624330` dispatched; 8/8 params tests + 2/2 handler tests pass)

### W4.D13.5 — Registration aggregate re-raises lifecycle events with populated EventId (F1/G3)

**Root cause:** `RefundRequest` child entity raises Approved/Rejected/Withdrawn events with `EventId=Guid.Empty` (it doesn't know its parent's EventId). Downstream email handlers call `_eventRepository.GetByIdAsync(EventId)` → returns null for Guid.Empty → exit silently with `event not found EventId=00000000-0000-0000-0000-000000000000` WARN. Empirically proven during W4 T7 Withdraw smoke test.

**Fix:** Add 3 aggregate-level methods on `Registration` (`ApproveRefundRequest`, `RejectRefundRequest`, `WithdrawRefundRequestV2`) that proxy to the child entity AND re-raise the corresponding domain event from the Registration root with populated `EventId`. 3 command handlers refactored to invoke via the aggregate. Child entity still raises its own EventId=Empty event for backward compat with direct-entity unit tests (acceptable — one WARN line per fire; the root-level event with proper EventId is the one that dispatches the email).

8 new domain tests in `RegistrationApproveRejectWithdrawRefundRequestTests.cs` — 3 happy paths assert `EventId.Should().Be(_eventId).Should().NotBe(Guid.Empty)`, 5 failure modes. ALL GREEN (76/76 Domain tests, 2748/2754 Application tests).

### W4.D14 — Refund webhook routing fix (F2 follow-up — operator UAT escape)

**Root cause exposed by operator UAT (fresh refund `2fb9acbd` post-D11 deploy):** 5 AddOnPurchases + 1 Sponsor stayed `status=Completed, refunded_at=null` despite line items reaching `status=4 Refunded` with `stripe_refund_id` populated. D11+D12 fix was correct but the dispatcher never invoked the AddOn/Sponsor handlers.

**Why:** `RefundExecutionService` set Stripe Refund metadata `["line_type"] = "AddOn"|"Sponsor"|...` but `PaymentsController.HandleChargeRefundedAsync` routes by `refund.Metadata["refund_type"]` OR `charge.Metadata["payment_type"]`. Neither was set for these refunds, so `charge.refunded` fell through to the default `_registrationWebhookHandler.HandleChargeRefundedAsync` — which doesn't know how to transition AddOnPurchase or Sponsor entities. Sponsor handler is unconditional `MarkAsRefunded()`, so the fact that refunded_at stayed null is decisive proof the handler was never invoked.

**Fix:**
1. `RefundExecutionService.cs` now sets `["refund_type"] = "add_on_purchase" | "sponsor" | "collection" | "registration"` on every workflow refund — matches the strings PaymentsController switches on, guaranteeing correct routing regardless of original charge metadata state.
2. `PaymentsController.cs` `Webhook-Refund-Route` log now records which metadata key resolved the type and lists the available keys.
3. New `[Phase 6A.148.W4.D14] [Webhook-Refund-Default-Route]` WARN logs the available metadata keys when the default route is hit without an explicit type — surfaces future regressions immediately.

Build clean; 76/76 Domain tests, 2748/2754 Application tests GREEN.

- [ ] **G3** — D13.5 + D14 staging-verified: fresh refund (mixed Ticket+AddOn+Sponsor) → DB confirms all underlying entities transition to Refunded + D8 decision email received; container logs show `[Webhook-Refund-Route] ResolvedFrom: refund.refund_type` (not default-route WARN)
- [ ] **G2** — D11 AddOnPurchase handler GREEN + D11b UI sync; staged together behind flag
- [ ] **G3** — D11+D11b staging-verified: W4.T2 confirms `add_on_purchases.status='Refunded'` after workflow refund
- [ ] **G4** — D12 generalised dedupe GREEN; W4.T1, T3, T4 confirm exactly one email per category
- [ ] **G5** — D13 Withdrawn handler GREEN; W4.T7 verifies email
- [ ] **G6** — D14 product decision committed; W4.T5 mixed-line refund delivers exactly the expected emails
- [ ] **G7** — D15 idempotency table + middleware; W4.T9/T10 GREEN
- [ ] **G8** — D16+D17 cleanup
- [ ] **G9** — Operator browser UAT confirms F1+F2 closed
- [ ] **G10** — PR opened with full Wave 4 evidence

---

# Wave 5 — Post-UAT Hardening (2026-05-20)

**Trigger:** Operator UAT on event `ad8903c4-e98e-49dd-b44e-d89f916c49dc` (registration `4d030697`, refund request `624b07c5`) surfaced 4 defects that survived Wave 4. RCA reviewed with Plan agent; G0 product-owner approval received 2026-05-20 with Q1-Q3 clarified.

## RCA summary (verified against DB)

**Defect 2 — refund stuck Approved (CRITICAL):** RR `624b07c5` approved at 03:35:28.935 UTC. ALL 4 line items still status=1 (Approved) with NO `stripe_refund_id`, NO `processed_at`. Yet 3 of 4 underlying entities are `Refunded` (entity-level webhooks routed correctly via W4.D14). The $100 ticket portion never refunded. Root cause: `RefundExecutionService.DispatchAsync` runs Stripe successfully per line + sets `line.MarkProcessing(refundId)` / `MarkRefunded` in memory, then terminal `_uow.CommitAsync()` at [RefundExecutionService.cs:149](../src/LankaConnect.Application/Events/Services/RefundExecutionService.cs#L149) throws `DbUpdateConcurrencyException` on the Registration row (xmin clash with concurrent Cancel flow). All in-memory changes roll back. `ApproveRefundRequestCommandHandler.cs:159` silently swallows the exception with vacuous "reconciler will retry" comment — but `RefundReconciliationService` (Phase 7G) only scans `RegistrationStatus.RefundRequested`, never `RefundRequest.Status=Approved` workflow rows.

**Defect 1 — bundled-at-registration sponsor missing from public strip (Q1 clarified — user wants ALL sponsors uniform):** DB query confirms $120 sponsor `110ffdef` (bundled) has `image_url=NULL`; $150 sponsor `20f16aa8` (standalone) has image_url + blob_name populated. [GetPublicEventSponsorsQueryHandler.cs:75-87](../src/LankaConnect.Application/Events/Queries/GetPublicEventSponsors/GetPublicEventSponsorsQueryHandler.cs#L75) requires `ImageUrl IS NOT NULL`. Backend [RsvpToEventCommandHandler.cs:598](../src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs#L598) CAN attach an image via `SponsorStagingBlobUrl` but FE registration-time sponsor flow doesn't capture/pre-upload one. Per user (Q1): "Whichever the way, they are sponsorships and should display in all the highlighted locations (including a and b)" — fix is FE parity (add image upload to registration-time sponsor flow).

**Defect 3 — Add-Ons "active" despite refunded (Q3 clarified):** Surface = My Dashboard → event manage → Attendees and Finance → Add-Ons → [AddOnsManagementTab.tsx](../web/src/presentation/components/features/events/AddOnsManagementTab.tsx). The `getPurchaseStatusColor()` switch at line 31-45 DOES handle `'Refunded'`. Hypothesis: DTO mapping returns the enum integer (`4`) instead of the string `"Refunded"`, hitting the `default` neutral-gray case. To verify in W5.D9 by inspecting `AddOnPurchaseDto.Status` mapping + an actual API response payload.

**Defect 4 — Refund email templates lack brand parity:** DB confirms 4 workflow templates are 6-7KB each (no header/logo/CTA/footer) vs 67-109KB for established templates. Subject_template fields have UTF-8 mojibake (em-dash → `?`). Fix via EF migration that rewrites `html_template` + `subject_template` using `template-event-registration-cancellation` skeleton.

## Architect's RCA corrections to original engineer's plan

| Original proposal | Architect verdict | Why |
|---|---|---|
| Per-line **inner commit** mid-loop | REJECTED | Inner commits flush in-progress `BeginProcessing`/`MarkCompletedIfAllSettled` AND refresh xmin on Registration row → makes concurrency *worse* |
| Just extend reconciler | Incomplete | Reconciler today re-calls Stripe with NO idempotency key → duplicate refund risk. Foundation must be Stripe-native `IdempotencyKey` first |
| Webhook auto-marks Registration Refunded | Fragile | `CompleteRefund` requires `Status=RefundRequested`. After Cancel-then-Refund flow Registration is `Cancelled` — needs new domain transition `CompleteRefundFromCancelled` |
| One-off SQL backfill | Wrong shape | Should be versioned EF migration with `[Migration(...)]` attribute, idempotent, ops-traceable |
| FE per-attendee tab fix | Wrong surface | `AddOnsManagementTab.tsx` IS correct. Real bug is likely DTO mapping. Audit step needed |
| Just edit template HTML | Insufficient | Mojibake needs byte-level UTF-8 verification in migration source (use `—` literal) |

## Wave 5 deliverables (W5.D1–W5.D15)

| ID | Effort | Title | Closes | Depends |
|----|--------|-------|--------|---------|
| **W5.D1** | 0.5d | TDD: Stripe `IdempotencyKey = $"refund_line_{line.Id:N}_{attempt}"` on every `CreateRefundAsync`. Failing test → add key → test passes | D2 foundation | — |
| **W5.D2** | 1d | TDD: Per-line **fresh-scope** dispatch via new `IRefundLineDispatcher`. Per call: `IServiceScopeFactory.CreateScope()` → fresh UoW + new `IRefundRequestLineItemRepository` → load tracked line → Stripe (W5.D1 key) → MarkProcessing/MarkRefunded → commit. Touches ONE child row, no Registration write, no xmin clash. Concurrency test: two parallel dispatches → one commits, one no-ops via state guard | D2 | D1 |
| **W5.D3** | 0.5d | TDD: Request-level commit in own fresh scope. After loop, re-load tracked `RefundRequest`, evaluate `MarkCompletedIfAllSettled()` from freshly-committed line states, commit | D2 | D2 |
| **W5.D4** | 0.5d | TDD: Domain — new `Registration.CompleteRefundFromCancelled(refundId)` transition. Allowed from `{RefundRequested, Cancelled}` when `RefundCompletedAt IS NULL`. Idempotent | D2.D | — |
| **W5.D5** | 0.5d | Webhook — `RegistrationWebhookHandler.HandleChargeRefundedAsync` workflow-aware branch. When `refund_type=registration` metadata AND matching `RefundRequestLineItem` exists → route to `CompleteRefundFromCancelled`. Legacy path unchanged. Idempotency guard via W4.D12 lookup | D2.D | D4 |
| **W5.D6** | 0.5d | Reconciler hardening — remove nullable deps in `RefundReconciliationService`; add startup DI integration test; WARN log when re-dispatching a line with existing `stripe_refund_id` (defensive — W5.D1 idempotency makes it safe but logs the situation) | D2 defence | D2 |
| **W5.D7** | 1d | **EF migration** `Phase6A148W5_BackfillRefund624b07c5`. Idempotent. Pre-flight Stripe balance check on PI `pi_3TZ0fsLv...`; out-of-band refund the $100 ticket (or mark Failed with audit if insufficient balance). UPDATE lines to status=4 + processed_at + stripe_refund_id; UPDATE RR to status=3; UPDATE registration to status=Refunded + refund_completed_at. **Verify `[Migration]` attribute in `.Designer.cs`** | D2 data fix | D1, D5 |
| **W5.D8** | 0.5d | TDD + UI: `SponsorSection.tsx:527` — add explicit `'Refunded'` branch with red strikethrough badge. Unit test mocks `mySponsors` with `status='Refunded'` and asserts visible "Refunded" badge | D3 (sponsor) | — |
| **W5.D9** | 0.5d | TDD + Audit: capture actual API response from `useEventAddOnPurchases` (DTO `purchase.status` value). If it's enum int (`4`) instead of string `"Refunded"`, fix the DTO mapping in `AddOnPurchaseDto` projection. If string is correct, find the alternate surface that fails | D3 (addon) | — |
| **W5.D10** | 1.5d | **Sponsor parity (Q1 expanded)** — FE: add image upload UI to registration-time sponsor checkout flow with same component as standalone path (`EditSponsorModal`-style). Plumb staging blob URL/name through `RsvpToEventCommand` payload (already supported backend-side per [RsvpToEventCommandHandler.cs:598](../src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs#L598)). Verify `/api/events/{id}/sponsors/public` returns all sponsors uniformly regardless of creation path (it already does per [GetPublicEventSponsorsQueryHandler.cs:75-87](../src/LankaConnect.Application/Events/Queries/GetPublicEventSponsors/GetPublicEventSponsorsQueryHandler.cs#L75) — no creation-path filter). Add E2E test covering: create registration with bundled sponsor + image → assert sponsor appears in `/sponsors/public` response | D1 | — |
| **W5.D11** | 1.5d | **EF migration** `Phase6A148W5_RewriteRefundEmailTemplatesWithBrandParity` — rewrite 4 templates (refund-decision, refund-pending-review, refund-rejected, refund-withdrawn) using `template-event-registration-cancellation` skeleton (67k chars: header + logo + CTA + footer). Em-dash as `—` literal in C# source. Verify byte-level UTF-8 in generated SQL. **Verify `[Migration]` attribute**. Idempotent UPDATE (running twice produces same row). Roundtrip unit test asserts placeholders interpolate correctly | D4 | — |
| **W5.D12** | 0.25d | Doc updates — append Wave 5 summary to `docs/PROGRESS_TRACKER.md`, `docs/STREAMLINED_ACTION_PLAN.md`, `docs/TASK_SYNCHRONIZATION_STRATEGY.md` | governance | all D1-D11 |
| **W5.D13** | 1d | Azure staging deploy (`deploy-staging.yml` for backend, `deploy-ui-staging.yml` for FE). Monitor both runs. Run W5.T1-T9 API verification using token from rule 10 (`password: 1qaz!QAZ`) | governance | D1-D11 |
| **W5.D14** | 0.5d | Post-deploy DB verification queries via `az postgres flexible-server execute`. Confirm RR + line + entity + registration states converged for T1-T4 scenarios | governance | D13 |
| **W5.D15** | 0.5d | Email visual review — operator screenshots all 4 refund emails. Confirms header/logo/CTA/footer parity. Em-dash renders correctly | D4 | D11, D13 |

**Total Wave 5 effort:** ~9 working days

## API test matrix W5.T1–W5.T9

Token (rule 10):
```bash
TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' | jq -r '.token')
```

| # | Scenario | Pass criterion (DB / log / email) |
|---|---|---|
| **W5.T1** | Workflow approve happy path — fresh registration with Ticket+AddOn+Sponsor+Collection lines | RR.status=3 Completed; all 4 lines status=4 Refunded with non-null `stripe_refund_id` + `processed_at`; all 4 entities Refunded; registration Refunded with `refund_completed_at`. Logs: `[6A.148 EXEC] Line {id} dispatched` x4; `[Webhook-Refund-Route] ResolvedFrom: refund.refund_type` x4; no default-route WARN. Email: one decision + one completion |
| **W5.T2** | Approve where 1 line's Stripe call fails (force via $0.50 below min) | 3 lines Refunded, 1 Failed; RR=Processing; reconciler picks up after 10min via W5.D6 + W5.D1 idempotency key (no duplicate Stripe refund). One decision email sent once; reconciler does NOT re-email |
| **W5.T3** | Concurrent xmin clash (two parallel approves) | First wins; second returns 409 Conflict; Stripe dashboard shows ONE refund per line (no duplicates) |
| **W5.T4** | Backfill of `624b07c5` — run W5.D7 migration | RR=Completed; all 4 lines status=4; registration Refunded with refund_completed_at + stripe_refund_id. Second migration run = no-op |
| **W5.T5** | Bundled-sponsor + ticket shared-PI refund (regression guard for W5.D5) | Both lines (Ticket + bundled-Sponsor) have DIFFERENT stripe_refund_id; both webhooks route correctly; exactly one decision email + one completion email |
| **W5.T6** | `SponsorSection.tsx` Refunded badge visual | Refunded sponsor sees red strikethrough "Refunded" badge with explicit text (not neutral gray) |
| **W5.T7** | Dashboard Add-Ons Refunded badge visual (Q3 surface) | Refunded add-on shows Indigo "Refunded" badge in `AddOnsManagementTab` Purchase History |
| **W5.T8** | Email visual review — 4 emails | LankaConnect header + logo + CTA + footer; em-dash renders correctly in subject; placeholders interpolate |
| **W5.T9** | Reconciler startup DI integration test | `IServiceProvider.GetRequiredService<IRefundReconciliationService>()` returns non-null; both deps bound; integration suite GREEN |

**Post-deploy DB verification queries (W5.D14)** — run via `az postgres flexible-server execute`:

```sql
-- Per RR: lines + RR state
SELECT rr."Id", rr.status AS rr_status,
       COUNT(*) FILTER (WHERE li.status = 4) AS refunded_lines,
       COUNT(*) FILTER (WHERE li.status IN (1,3)) AS in_flight_lines,
       COUNT(*) FILTER (WHERE li.stripe_refund_id IS NULL AND li.status = 4) AS missing_refund_id
FROM events.refund_requests rr
JOIN events.refund_request_line_items li ON li.refund_request_id = rr."Id"
WHERE rr."Id" = '<RR_ID>'::uuid
GROUP BY rr."Id";

-- Registration converged
SELECT "Id", "Status", "RefundCompletedAt", "StripeRefundId" FROM events.registrations WHERE "Id" = '<REG_ID>'::uuid;

-- Stripe double-spend canary
SELECT stripe_refund_id, COUNT(*) FROM events.refund_request_line_items
WHERE stripe_refund_id IS NOT NULL
GROUP BY stripe_refund_id HAVING COUNT(*) > 1;
```

## Risks register (Wave 5)

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|------------|--------|------------|
| R1 | `IdempotencyKey` collision on operator-retry of Failed line | Low | High | Key format `refund_line_{lineId:N}_{attemptCounter}` — counter incremented on `MarkFailed` |
| R2 | Per-line fresh scopes inflate DB connections under burst | Medium | Medium | Each scope releases connection on dispose; pool sized for it. Prometheus counter on dispatcher scope checkouts; alert if exceeds expected |
| R3 | W5.D5 webhook change regresses non-workflow legacy refunds | Medium | High | New branch CONDITIONAL on `refund_type=registration` metadata + matching workflow line. Legacy path (no metadata) executes existing `CompleteRefund`. W4.T14 protects |
| R4 | W5.D7 backfill — Stripe charge insufficient refundable balance for $100 ticket | Low | Medium | Pre-flight inspects `charge.amount_refunded` vs `charge.amount`. If gap < $100, refund gap + mark line Failed with audit. Do NOT block migration |
| R5 | Email template HTML rewrite breaks variable interpolation | Medium | High | Placeholder names preserved verbatim; D11 includes roundtrip unit test asserting interpolated output contains expected substrings |
| R6 | Reconciler picks up RR in-flight (race) | Low | Medium | `ListStuckApprovedAsync` filters by `updated_at < now - 10min`. Lines being dispatched update within seconds. Belt-and-braces: `SELECT ... FOR UPDATE SKIP LOCKED` on the RR row |
| R7 | Sponsor parity (D10) FE changes break existing standalone sponsor flow | Low | Medium | Reuse existing `EditSponsorModal` image-picker component (proven in standalone). Standalone path unchanged. New unit test covers bundled-sponsor-with-image creation |
| R8 | Mojibake fix re-introduces on dev machine with CP-1252 editor | Medium | Medium | `.editorconfig` already enforces UTF-8 BOM for `*.cs`. PR review checklist item. Post-migration DB query asserts subject_template bytes contain `\xe2\x80\x94` (em-dash UTF-8) |
| R9 | `RegistrationWebhookHandler` invariant tests broken by W5.D4 new transition | Low | Medium | Domain tests for both `CompleteRefund` (existing) and `CompleteRefundFromCancelled` (new) — explicit allowed-from-state matrix |
| R10 | Cron reconciler runs against pre-W5 stuck rows with no prior idempotency keys | Medium | Medium | Keys are line-id-derived; Stripe treats first-attempt-via-reconciler as fresh refund. Pre-W5 stuck rows ARE the case we want re-dispatched. W5.D6 WARN catches the rare case where Stripe previously succeeded but DB hid it; ops triages |

## Phase gates (Wave 5) — status as of 2026-05-21

- [x] **G0** — Product owner approves Wave 5 plan + Q1-Q3 (received 2026-05-20)
- [x] **G1** — W5.D1+D2+D3 shipped: per-line fresh-scope dispatch with Stripe idempotency. Application test suite 2750 passed, 6 skipped, 0 failed. Commits c7094c97 (D1), c85ace0f (D2+D3). Deploy 26244864158 GREEN.
- [x] **G2** — W5.D4+D5 shipped: `CompleteRefundFromCancelled` domain transition + webhook workflow-aware branch. 9 new W5.D4 domain tests GREEN; 2750/2756 Application tests GREEN (no W4 regressions). Commits b6f153a5 (D4), f83e7164 (D5+D6). Deploy 26245570186 GREEN.
- [x] **G3** — W5.D6 shipped: reconciler nullable deps removed + defensive WARN for re-dispatch of lines with existing stripe_refund_id. Commit f83e7164 within same deploy.
- [x] **G4** — W5.D7 backfill applied + verified: RR `624b07c5` now status=Completed, all 4 lines Refunded with stripe_refund_id, Registration `4d030697` Status=Refunded with `RefundCompletedAt` populated + `AddOnRefundAmount=14.00`. Commit 38361215. Migration `20260521033926_Phase6A148W5D7_BackfillRefund624b07c5` in __EFMigrationsHistory.
- [x] **G5** — W5.D8 (sponsor badge) + W5.D9 (addons badge UX) shipped + UI deployed. Commits 38361215 (D8) + f64a1ed7 (D9). UI deploy 26204167876 + 26231906430 GREEN. W5.D10 (sponsor parity — show all confirmed sponsors with initials placeholder) committed f58d428c, deploys 26246240638 (BE) + 26246242214 (UI) in_progress.
- [~] **G6** — W5.D11.a shipped: subject_template em-dash mojibake fixed across 4 templates. Bytes verified UTF-8 (0xE2 0x80 0x94). Commit f64a1ed7. Migration `20260521141028_Phase6A148W5D11a_FixRefundEmailSubjectMojibake` applied. **W5.D11.b** (full HTML brand-parity body rewrite) deferred to next session (~1.5d).
- [x] **G7** — Staging deploys GREEN across 5 separate runs (26204166728, 26233144134, 26244864158, 26245570186, 26246240638). API health 200. Both new EF migrations (W5.D7 backfill + W5.D11.a mojibake) applied per __EFMigrationsHistory.
- [~] **G8** — Partial: T4 (backfill verification) GREEN via DB query; Auth + refund-requests list endpoint smoke tested (RR `624b07c5` correctly shows Completed). T1-T3, T5-T9 require fresh paid Stripe checkout flow (operator UAT) — defer until next operator session.
- [ ] **G9** — Operator browser UAT pending; Wave 5 changes ready for verification.
- [ ] **G10** — PR for Wave 5 evidence bundle: open after G9 confirmation.

### W5.D11.b deferral note

Full HTML rewrite of 4 templates (refund-decision, refund-pending-review, refund-rejected, refund-withdrawn) with brand parity to `template-event-registration-cancellation` (~67k chars master pattern) is genuine ~1.5d work requiring careful HTML authoring + visual review. Subject mojibake fix (W5.D11.a) ships the immediate visual win; full HTML body redesign is the larger Defect 4 remediation queued separately.

### Sessions ledger (Wave 5)

| Date | Commits | Deliverables |
|---|---|---|
| 2026-05-20 | 38361215, c7094c97, f64a1ed7, 52ee2463 | D7 backfill + verify, D8 sponsor badge, D1 IdempotencyKey + 2 tests, D9 addon badge UX, D11.a mojibake fix, CI Task.Delay flake fix |
| 2026-05-21 | c85ace0f, b6f153a5, f83e7164, f58d428c | D2+D3 per-line fresh-scope dispatch + request-level commit, D4 CompleteRefundFromCancelled + 9 tests, D5 webhook workflow branch, D6 reconciler hardening, D10 sponsor parity FE |

## Q1-Q3 clarifications locked (2026-05-20)

| # | Answer |
|---|--------|
| **Q1** | All sponsors — regardless of creation path (organizer-added, real-sponsor-via-event-section, real-sponsor-bundled-at-ticket-checkout) — treated uniformly. Display in BOTH (a) top public Sponsors strip AND (b) "Sponsor This Event" form-section logos. Backend `GetPublicEventSponsorsQueryHandler` already does NOT filter by creation path — fix is FE: add image upload to registration-time sponsor flow (parity with standalone) |
| **Q2** | Approved — run EF migration to backfill refund `624b07c5` including out-of-band Stripe API call for the $100 ticket against PI `pi_3TZ0fsLv...` with pre-flight balance check |
| **Q3** | Surface = "My Dashboard → Attendees and Finance → Add-Ons" = `AttendeesAndFinanceTab.tsx` → `AddOnsManagementTab.tsx`. Code handles `Refunded` correctly per Explore audit; likely DTO mapping issue (enum int vs string). Verify in W5.D9 |

---

# Wave 5.6.B — Email-completion race (4th-report regression)

**Opened**: 2026-05-23 (after operator UAT showed Refund Complete email displaying $94 when $204 was approved)
**Architect-locked RCA**: race between Stripe webhook arrival (ticket charge.refunded at 21:04:26.540 firing `Registration.CompleteRefundFromCancelled` → `RefundCompletedEvent`) and the serial per-line dispatcher (Sponsor line still in flight, committed 831ms later at 21:04:27.371). Calculator's `SUM(WHERE Status==Refunded)` excluded the in-flight Sponsor line.
**Status**: design locked with architect, code not yet started.

## RCA evidence (DB timestamps — reconstructable from `events.refund_request_line_items.processed_at` + `events.registrations.RefundCompletedAt`)

| Time UTC | Event | Sponsor line status at moment |
|---|---|---|
| 21:04:25.150 | Dispatcher commits AddOn line ($14) → Refunded | Approved (not started) |
| 21:04:26.388 | Dispatcher commits Ticket line ($80) → Refunded | Approved (not started) |
| **21:04:26.540** | **Ticket webhook arrives → `RefundCompletedEvent` raised → calculator returns $94** | **Approved/Processing — STILL IN FLIGHT INSIDE DISPATCHER** |
| 21:04:27.371 | Dispatcher commits Sponsor line ($110) → Refunded — TOO LATE | Refunded |
| 21:04:27.382 | RR.Status flips to Completed | n/a |

Architect emission audit confirmed: **only 2 emit sites for `RefundCompletedEvent`** (`Registration.CompleteRefund` line 932, `Registration.CompleteRefundFromCancelled` line 1010). Both reachable only via ticket webhook. The race is the only explanation.

## Hidden gaps surfaced during architect call-site audit

| Gap | What | Files |
|---|---|---|
| **G_2.a** | AddOn/Sponsor/Collection webhook handlers DON'T flip `RefundRequestLineItem.Status` to Refunded — only flip entity status. If Stripe ever returns "pending" inline, the line stays Processing indefinitely | `AddOnPurchaseWebhookHandler.cs`, `SponsorWebhookHandler.cs`, `CollectionWebhookHandler.cs` |
| **G_2.b** | Ticket webhook handler also doesn't invoke `MarkCompletedIfAllSettled` | `RegistrationWebhookHandler.cs` |
| **G_2.c** | Reconciler stuck-cancelled + 7G sweeps don't invoke `MarkCompletedIfAllSettled` | `RefundReconciliationService.cs` |
| **G_2.d** | Need to STOP raising `RefundCompletedEvent` from `Registration.CompleteRefundFromCancelled` for workflow refunds (else duplicate emails); KEEP raising for legacy `CompleteRefund` (no RR exists for legacy CancelRsvp path) | `Registration.cs` |

## Call-site completeness matrix (8 sites; architect-verified)

| # | Call site | File | Action |
|---|---|---|---|
| 1 | `RefundExecutionService.TransitionRequestInOwnScopeAsync` | `Application/Events/Services/RefundExecutionService.cs:196` | KEEP (already calls `MarkCompletedIfAllSettled`) |
| 2 | `RegistrationWebhookHandler.HandleChargeRefundedAsync` (ticket) | `Infrastructure/Payments/Services/RegistrationWebhookHandler.cs:737-854` | **ADD**: lookup workflow line, flip Status if Processing, then `MarkCompletedIfAllSettled` |
| 3 | `AddOnPurchaseWebhookHandler.HandleChargeRefundedAsync` | `.../AddOnPurchaseWebhookHandler.cs:202-309` | **ADD both**: line.Status flip + `MarkCompletedIfAllSettled` |
| 4 | `SponsorWebhookHandler.HandleChargeRefundedAsync` | `.../SponsorWebhookHandler.cs:181-` | **ADD both** (same as #3) |
| 5 | `CollectionWebhookHandler.HandleChargeRefundedAsync` | `.../CollectionWebhookHandler.cs:168-` | **ADD both** (same as #3) |
| 6 | `RefundReconciliationService.ReconcileStuckApprovedRefundRequestsAsync` | `Application/Events/Services/RefundReconciliationService.cs:58` | KEEP (indirect via #1) |
| 7 | `RefundReconciliationService.ReconcileStuckRefundsAsync` (7G legacy) | same file, line ~207 | **ADD**: after `CompleteRefund`, also `MarkCompletedIfAllSettled` if RR exists |
| 8 | `RefundReconciliationService.ReconcileStuckCancelledWithRefundedTicketAsync` | same file, line 138-205 | **ADD**: after `CompleteRefundFromCancelled`, also `MarkCompletedIfAllSettled` |

## Wave 5.6.B deliverables

| ID | Effort | Title | Closes-defect | Depends |
|---|---|---|---|---|
| **W5.6.B.G0** | 0.25d | Read each of the 8 call sites; confirm architect's audit; report any divergence before coding | — | — |
| **W5.6.B.G1.1** | 0.5d | Domain: new `RefundRequestCompletedEvent` record (carries EventId, RegistrationId, RefundRequestId, "primary" StripeRefundId from ticket line if present else first Refunded line, total = SUM(Refunded ApprovedAmount), CompletedAt) | RCA | G0 |
| **W5.6.B.G1.2** | 0.25d | `RefundRequest.MarkCompletedIfAllSettled` raises `RefundRequestCompletedEvent` from inside the method at Status-flip line. Idempotent via existing guard | RCA | G1.1 |
| **W5.6.B.G1.3** | 0.5d | Gate `Registration.CompleteRefundFromCancelled` and `Registration.CompleteRefund` to NOT raise `RefundCompletedEvent` when the registration's refund originated from a workflow path (lookup test). Legacy direct-Stripe path keeps existing event | G_2.d | G1.2 |
| **W5.6.B.G2.1** | 0.5d | New `RefundRequestCompletedEventHandler` (mirrors `RefundCompletedEventHandler` body but no fallback needed — total comes from the event payload, not calculator) | RCA | G1.1 |
| **W5.6.B.G2.2** | 0.25d | New `RefundRequestCompletedWhatsAppHandler` parallel | RCA | G2.1 |
| **W5.6.B.G2.3** | 0.5d | Add `MarkCompletedIfAllSettled` invocation (in fresh scope) to RegistrationWebhookHandler ticket path | G_2.b | G1.2 |
| **W5.6.B.G2.4** | 0.5d | Add line.Status flip + `MarkCompletedIfAllSettled` to AddOnPurchaseWebhookHandler | G_2.a | G1.2 |
| **W5.6.B.G2.5** | 0.5d | Add line.Status flip + `MarkCompletedIfAllSettled` to SponsorWebhookHandler | G_2.a | G1.2 |
| **W5.6.B.G2.6** | 0.5d | Add line.Status flip + `MarkCompletedIfAllSettled` to CollectionWebhookHandler | G_2.a | G1.2 |
| **W5.6.B.G2.7** | 0.5d | Add `MarkCompletedIfAllSettled` to reconciler sweeps #7 and #8 | G_2.c | G1.2 |
| **W5.6.B.G3** | 1d | Tier α tests: 6 domain unit tests on `RefundRequest.MarkCompletedIfAllSettled` event-raising | RCA verify | G1.2 |
| **W5.6.B.G4** | 2d | Tier β tests: Testcontainers Postgres + real DI + 50-iter randomised webhook race per scenario (6 orderings + duplicate + Failed-line + race) | RCA verify | G2.* |
| **W5.6.B.G5** | 0.5d | Build clean; all old + new tests GREEN; no regressions | quality | G3, G4 |
| **W5.6.B.G6** | 0.5d | Deploy to staging | ship | G5 |
| **W5.6.B.G7** | 0.5d | Tier δ synthetic smoke: internal admin endpoint that publishes `RefundRequestCompletedEvent` for an existing RR; verify email arrives at known mailbox with correct total | wiring/DI sanity on staging | G6 |
| **W5.6.B.G8** | 0.5d | Operator UAT confirmation (post-fix) — fresh paid registration with Ticket+AddOn+Sponsor, observe email amount matches Decision total | final sign-off | G7 |
| **W5.6.B.G9** | 0.25d | Docs: update PROGRESS_TRACKER.md, mark W5.6.B section in this master TODO with "shipped + verified" timestamps and commit SHAs | governance | G8 |

**Total effort estimate**: ~9 working days

## API + test matrix (W5.6.B.T1–T11)

Auth pattern (rule 10):
```bash
TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' | jq -r '.accessToken')
```

| # | Tier | Scenario | Expected pass criterion |
|---|---|---|---|
| **W5.6.B.T1** | α | `RefundRequest.MarkCompletedIfAllSettled` raises `RefundRequestCompletedEvent` exactly once after all lines terminal (3 lines, order [Sponsor, AddOn, Ticket]) | event raised once; Status=Completed; payload total = SUM(Refunded.ApprovedAmount) |
| **W5.6.B.T2** | α | Same as T1 with reverse order [Ticket, AddOn, Sponsor] | same — order-independent |
| **W5.6.B.T3** | α | Mixed terminal [Refunded, Failed, Rejected] — event raised, total includes Refunded only | total excludes Failed and Rejected lines |
| **W5.6.B.T4** | α | Idempotency: call `MarkCompletedIfAllSettled` twice — event raised once total | exactly one event |
| **W5.6.B.T5** | α | Partial: only 2 of 3 lines terminal — event NOT raised, Status unchanged | zero events; Status still Processing |
| **W5.6.B.T6** | α | Payload assertion: event's "primary StripeRefundId" = ticket line's SRI when ticket exists, else first Refunded line's SRI | correct SRI selected |
| **W5.6.B.T7** | β | Webhook race simulation — `IStripePaymentService` mock returns "pending" for all 3 lines; webhook handlers invoked via `Task.WhenAll` with microsleeps in randomised orderings; 50 iterations per ordering (6 orderings × 50 = 300 iters) | exactly 1 `RefundRequestCompletedEventHandler` invocation per RR per iteration; total = $204 every time; no duplicate emails |
| **W5.6.B.T8** | β | Duplicate webhook delivery (Stripe retries) — same webhook fired 2x for one line | exactly 1 event despite 2 webhook calls per line |
| **W5.6.B.T9** | β | Failed line — Stripe rejects sponsor; webhook arrives with refund.status='failed' | event raised once, total = $94 (Ticket+AddOn only); RR.Status = Completed (Failed counts as terminal) |
| **W5.6.B.T10** | β | Legacy direct-Stripe regression — pre-6A.148 CancelRsvp refund still receives email via legacy `RefundCompletedEventHandler` with legacy formula | byte-identical email behaviour to pre-W5.6.B |
| **W5.6.B.T11** | δ | Synthetic staging smoke — POST `/api/internal/refund-test-harness/replay/{rrId}` admin endpoint (gated by admin claim); publishes `RefundRequestCompletedEvent` for RR `86d0a7dc` via `IMediator.Publish` | email arrives at operator's mailbox showing $204 (not $94); container log shows `RefundRequestCompletedEventHandler` ran exactly once |

**Post-deploy DB verification queries**:

```sql
-- After T11 smoke runs on staging:
SELECT "Id"::text, "Status", total_price_amount::text
FROM events.registrations
WHERE "Id" = 'f0b408fb-2b3a-4ad3-a984-a5c5ff5520c9'::uuid;
-- expected: Status=Refunded (unchanged); total_price_amount unchanged (legacy column doesn't drive email anymore)

-- Regression scan: no RR should be in Processing for >10min after lines settled
SELECT rr."Id", rr.status, MAX(li.processed_at) as last_line_settled
FROM events.refund_requests rr
JOIN events.refund_request_line_items li ON li.refund_request_id = rr."Id"
WHERE rr.status = 2  -- Processing
GROUP BY rr."Id", rr.status
HAVING MAX(li.processed_at) < NOW() - INTERVAL '10 minutes';
-- expected: zero rows (all-terminal predicate has flipped them via the new gate)
```

## Risks register

| # | Risk | Mitigation |
|---|---|---|
| RR1 | Duplicate email if legacy handler also fires for workflow path | G1.3 gates `Registration.CompleteRefundFromCancelled` to NOT raise legacy event when an RR exists; T10 + T11 verify no-duplicate |
| RR2 | `MarkCompletedIfAllSettled` invocation from webhook handler clashes with the inline-success path's invocation from `TransitionRequestInOwnScopeAsync` | Method is already idempotent (existing `if (Status == Completed) return Success` guard at line ~351); multiple convergence paths is the intended design |
| RR3 | Webhook handler's line.Status flip races with the dispatcher's inline MarkRefunded | First-writer-wins via the state machine guard in MarkRefunded; second caller no-ops |
| RR4 | New domain event needs MediatR + DI wiring; misconfiguration → silent zero-emails | T11 (δ tier) explicitly smokes the wiring on staging |
| RR5 | Testcontainers Postgres setup adds CI runtime + flakiness | Existing harness — verify in G0 whether the project already uses Testcontainers; if not, the integration tests use in-memory provider (weaker but acceptable when paired with T11) |
| RR6 | The fix touches 8 call sites; any one missed leaves a regression window | G0 audit + G2 paired implementation + G4 50-iter β tests across all paths |

## Phase gates (W5.6.B)

- [ ] **G_α** — All 6 W5.6.B.T1-T6 domain tests green (RCA fix mathematically correct)
- [ ] **G_β** — All 4 W5.6.B.T7-T10 integration tests green across 50-iter race simulation
- [ ] **G_BUILD** — Build clean; full Application suite GREEN; zero regressions
- [ ] **G_DEPLOY** — Staging deploy successful; container health 200; migration applied (if any)
- [ ] **G_δ** — W5.6.B.T11 synthetic smoke: admin endpoint publishes event, email arrives with $204
- [ ] **G_UAT** — Operator UAT: fresh paid registration with multi-line refund, email matches Decision total
- [ ] **G_DOC** — PROGRESS_TRACKER.md updated; W5.6.B section here marked shipped with commit SHAs

## Honest accountability — what went wrong on W5.6.A (yesterday)

I shipped W5.6.A claiming verified success based on:
- 4 unit tests with mocked repo (didn't reproduce the race)
- 1 SQL query against an ALREADY-Completed RR (no race possible by query time)
- Deploy green + API health 200 (says nothing about email correctness)

The architect's "ticket fires last" premise was an assumption I anchored on without proof. The DB timing evidence above proves it false. **Wave 5.6.B mandates Tier β (50-iter race simulation) and Tier δ (synthetic admin endpoint smoke) as deploy-blocking gates so this class of false-success cannot recur.**

---

## Mid-flight scope expansion (2026-05-23, after operator-shared 3-email screenshot review)

**Trigger**: operator forwarded 3 emails for RR 86d0a7dc and asked whether "sponsor + ticket+addon refunded separately" is implemented. Architect re-audit:

1. **Screenshot 1 was NOT a refund email** — it was the original "Sponsorship Confirmed" onboarding email from 9:01 PM (3 min before refund), sent to sponsor LankaEvents at `niroshanaks@gmail.com`. Template audit confirms: glyph `✓` (vs refund's `↩`), verb "Thank you for sponsoring" (vs refund's "has been refunded"), timestamp matches `sponsor.payment_completed_at` (vs `refunded_at`). My pattern-match on the word "Sponsorship" failed to discriminate.

2. **D9 suppression worked correctly** for this refund (legacy `template-sponsor-refund` was correctly suppressed because the refund is workflow-owned). But that exposes a NEW correctness defect: **sponsor LankaEvents (`niroshanaks@gmail.com`) is a DIFFERENT person from registration user Niroshana Sinharage (`niroshhh@gmail.com`)**. D9 silently denies them refund notification — they sponsored $110, their card was charged, the refund happened, and nobody told them. Regulatory / chargeback risk.

3. **Observability gap proven** — I couldn't disambiguate screenshot 1 from logs because:
   - Container app stdout retention is ~25 min (gone for any post-mortem)
   - `events.email_messages` is **empty** (0 rows in every query this session — the typed-email path bypasses queue persistence)
   - No Log Analytics workspace wired for container stdout (verified: `ContainerAppConsoleLogs_CL` doesn't exist)
   - No outbound email audit log anywhere

   Every operator complaint becomes a forensic exercise via screenshots. Unsustainable.

## Wave 5.6.B EXPANDED — ship 3 things in ONE PR

**User mandate**: "Implementing enough logs for diagnosys and retain logs for a longer period is a must." So scope grows to include the observability primitives, not just the race fix.

### Phase 1 — Observability foundation (ships FIRST in the PR; everything else verifies against it)

| ID | Effort | Title |
|---|---|---|
| **W5.6.B.OBS1** | 1d | New EF migration `Phase6A148W56B_AddEmailDispatchLog` — schema for `communications.email_dispatch_log` table. Columns: `id uuid PK`, `correlation_id uuid`, `refund_request_id uuid NULL`, `entity_type varchar(40) NULL`, `entity_id uuid NULL`, `template_name varchar(80)`, `recipient_email varchar(255)`, `recipient_name varchar(255)`, `subject_rendered text`, `payload_json jsonb`, `suppressed bool`, `suppression_reason varchar(120) NULL`, `dispatched_at timestamptz`, `provider_message_id varchar(120) NULL`, `provider_status varchar(40) NULL`, `created_at timestamptz`. Indexes on (correlation_id), (refund_request_id), (recipient_email + dispatched_at), (template_name + dispatched_at). `[Migration]` attribute verified in `.Designer.cs` |
| **W5.6.B.OBS2** | 1d | `ITypedEmailService.SendEmailAsync` impl wraps the existing send with a SYNCHRONOUS dispatch-log write — `suppressed=false` for actual sends, `suppression_reason='...'` when the caller short-circuits (e.g., D9). Provider message id/status updated via callback later. Includes ALL existing typed-email use sites — refund, registration confirmation, donation, sponsor, etc. |
| **W5.6.B.OBS3** | 0.5d | Refactor D9 suppression branches in `SponsorWebhookHandler` / `CollectionWebhookHandler` to also write a `suppressed=true` row to `email_dispatch_log` (so a SQL query reveals which emails would have been sent and why suppressed) |
| **W5.6.B.OBS4** | 0.5d | **Container app diagnostic settings → Log Analytics workspace** (`lankaconnect-staging-logs`, customer id `b1d673c4-4467-4022-b666-807690c33729`). Wire `ConsoleLogs` + `SystemLogs` categories. **30-day retention minimum** (Azure default; can extend to 90+ via workspace settings). One-shot ops command via `az monitor diagnostic-settings create`; no app code change. Document the command in `docs/ops/AZURE_LOG_RETENTION.md` |
| **W5.6.B.OBS5** | 0.25d | Add structured Serilog enrichers in `RefundExecutionService`, `RefundLineDispatcher`, `RegistrationWebhookHandler`, all webhook handlers — push `RefundRequestId`, `RegistrationId`, `StripeRefundId`, `LineItemType`, `CorrelationId` via `LogContext.PushProperty`. Already partial; close the gaps so every refund-flow log line is queryable by RrId |

### Phase 2 — Race fix (the $94 → $204 bug; original W5.6.B scope)

Unchanged from prior section. G0 → G1 → G2 → G3 (α tests) → G4 (β tests).

### Phase 3 — D9 refinement (sponsor-as-separate-person)

| ID | Effort | Title |
|---|---|---|
| **W5.6.B.D9R1** | 0.25d | `SponsorWebhookHandler` — change suppression predicate from `isWorkflowOwnedRefund` to `isWorkflowOwnedRefund && SponsorEmailEqualsRegistrationUserEmail(sponsor, registration)` (case-insensitive trim compare). When sponsor's email differs from registration user's, ALWAYS send the per-sponsor refund email — sponsor is a third party and must know their money is coming back |
| **W5.6.B.D9R2** | 0.25d | TDD: new fact `WorkflowOwnedRefund_SponsorEmailDiffersFromRegistration_StillSendsEmail` in `SponsorWebhookHandlerD9Tests.cs`. Also assert that when emails match (sponsor IS the registration user), the existing suppress-behavior still fires |
| **W5.6.B.D9R3** | 0.25d | Equivalent refinement check on `CollectionWebhookHandler` — same predicate, same test. Collections likely always equal-emails but add the guard for defense-in-depth |

### Updated API/test matrix additions

| # | Tier | Scenario | Pass criterion |
|---|---|---|---|
| **W5.6.B.T12** | OBS | After T11 staging smoke runs, query `SELECT * FROM communications.email_dispatch_log WHERE refund_request_id='<test-rr-id>'`. Returns rows for every email that fired (or was suppressed) | Each row has recipient_email + template_name + suppressed flag. Can reconstruct the flow from SQL alone |
| **W5.6.B.T13** | OBS | Log Analytics query `ContainerAppConsoleLogs_CL | where TimeGenerated > ago(1h) and Log_s contains '<test-rr-id>'` returns log lines from the test refund | Container logs are queryable beyond 25-min container stdout window |
| **W5.6.B.T14** | D9R | Fresh refund where sponsor email == registration user email → no per-sponsor refund email fires (D9 still suppresses); registration user gets ONE consolidated refund-complete email | One row dispatched (refund-complete), one row suppressed (workflow-owned per-sponsor); registration user inbox = 1 refund email |
| **W5.6.B.T15** | D9R | Fresh refund where sponsor email differs from registration user → per-sponsor refund email FIRES to sponsor's distinct address; registration user STILL gets consolidated refund-complete email | Two distinct dispatched rows to two distinct recipient_emails |
| **W5.6.B.T16** | OBS | All existing refund-flow code paths now write to email_dispatch_log (RefundDecision, RefundCompleted (workflow), RefundCompleted (legacy), per-sponsor refund, per-AddOn-refund (none today — confirm null), per-Collection refund) | One dispatch row per outgoing email. Spot-check counts via SQL |

### Updated risks register

| # | Risk | Mitigation |
|---|---|---|
| RR7 | `email_dispatch_log` write throws → email never sent (synchronous coupling makes log a blocker) | Wrap log write in try/catch with WARN log; failing to log MUST NOT block send. Email send is still primary; log is best-effort observability not strict audit |
| RR8 | Container app diagnostic settings to Log Analytics — wrong workspace selected → logs go nowhere | OBS4 explicitly names workspace `lankaconnect-staging-logs` (customerId `b1d673c4-4467-4022-b666-807690c33729`); verify via `az monitor diagnostic-settings show` post-config |
| RR9 | OBS scope inflation delays the $94 → $204 race fix the user is angry about | Mitigated by single-PR atomic ship: race fix CANNOT regress because OBS gives us SQL verification post-deploy (operator no longer needs to send screenshots to confirm) |

### Updated phase gates (W5.6.B)

- [ ] **G_OBS1** — `email_dispatch_log` table created + EF migration applied to staging; verified via `__EFMigrationsHistory`
- [ ] **G_OBS2** — `ITypedEmailService` wired to write dispatch rows; smoke test on staging shows one dispatch row per sent email + suppressed flag where applicable
- [ ] **G_OBS3** — Container app diagnostic setting exported to Log Analytics; `ContainerAppConsoleLogs_CL` returns rows for a test log line
- [ ] **G_α** — All 6 W5.6.B.T1-T6 domain tests green (race fix mathematically correct)
- [ ] **G_β** — W5.6.B.T7-T10 integration tests green across 50-iter race simulation
- [ ] **G_D9R** — W5.6.B.T14 + T15 prove the differing-email path sends to both recipients
- [ ] **G_BUILD** — Build clean; full Application suite GREEN; zero regressions
- [ ] **G_DEPLOY** — Staging deploy successful; container health 200; both migrations applied
- [ ] **G_δ** — W5.6.B.T11 + T12 + T13: synthetic smoke + SQL verification of dispatch rows + Log Analytics query
- [ ] **G_UAT** — Operator UAT: fresh paid registration with multi-line refund including sponsor-with-different-email; verify (a) refund-complete email matches Decision total, (b) sponsor gets their own refund email at the differing address, (c) all 3 emails visible in `email_dispatch_log` via SQL
- [ ] **G_DOC** — `PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, and `docs/ops/AZURE_LOG_RETENTION.md` updated
