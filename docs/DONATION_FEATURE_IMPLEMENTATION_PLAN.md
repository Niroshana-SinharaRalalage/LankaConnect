# Donation Feature — Complete Implementation Plan (v3)

## Context

LankaConnect needs a **standalone Donation system** for events. Donations are a **first-class entity** — completely separate from ticket payments/registrations with their own database table. Key requirements:

- **Separate `events.donations` table** — not fields on Registration
- **Capture donor info**: name, email, phone, notes
- **Donate anytime** — from event details page at any time, AND optionally during registration
- **Organizer management** — enable/configure donations, view/export separately
- **Platform + Stripe fees** apply on combined total (same rates as ticket payments)
- **Combined checkout when donating during registration** — one Stripe session for ticket+donation, but stored separately

The architecture follows the `RegistrationAddition` pattern for standalone donations, and extends `RsvpToEventCommandHandler` for registration-bundled donations.

---

## Design Decisions

1. **`Donation` is a standalone entity** with own table `events.donations` (follows `RegistrationAddition` pattern)
2. **`DonationConfiguration`** is a JSONB value object on `Event` (follows `TicketPricing` pattern) — organizer toggles and configures
3. **Two checkout scenarios**:
   - **During registration (paid event)**: Ticket + donation combined into ONE Stripe Checkout (2 line items). Fees calculated on the combined total. Webhook confirms BOTH Registration AND Donation. The Donation row has `RegistrationId` linking it to the registration.
   - **During registration (free event)** or **Anytime from event page**: Standalone Stripe Checkout with donation-only line item. `payment_type: "donation"` metadata for webhook routing.
4. **Fee calculation on combined total**: When bundled, Stripe charges one fee (2.9% + $0.30) on the total (ticket + donation). Platform commission (2%) also on the total. Fees allocated proportionally between Registration and Donation records for reporting.
5. **Anonymous donations** supported — `[AllowAnonymous]` endpoint, `DonorUserId` nullable
6. **Separate `DonationsController`** for standalone donation endpoints — keeps EventsController manageable

---

## Implementation Phases (TDD Order)

### Phase 1: Domain Layer

**New files:**
- [DonationStatus.cs](src/LankaConnect.Domain/Events/Enums/DonationStatus.cs) — Enum: `Pending`, `Completed`, `Failed`, `Abandoned`, `Refunded`
- [DonationConfiguration.cs](src/LankaConnect.Domain/Events/ValueObjects/DonationConfiguration.cs) — Value object: `IsEnabled`, `SuggestedAmounts` (up to 3), `AllowCustomAmount`, `MinAmount`, `MaxAmount`, `DonationMessage`. Factory `Create()` with validation, `Disabled()` for default state
- [Donation.cs](src/LankaConnect.Domain/Events/Donation.cs) — Entity (follows `RegistrationAddition` pattern): `EventId`, `RegistrationId?` (links to registration when bundled, null for standalone), `DonorUserId?`, `DonorName`, `DonorEmail`, `DonorPhone?`, `DonorNotes?`, `Amount` (Money), `Status`, Stripe fields (`StripeCheckoutSessionId`, `StripePaymentIntentId`, `CheckoutExpiresAt`), revenue breakdown fields (`StripeFeeAmount`, `PlatformCommissionAmount`, `OrganizerPayoutAmount` as Money), lifecycle timestamps. Methods: `Create()`, `CreateBundledWithRegistration()` (for combined checkout), `SetStripeCheckoutSession()`, `CompletePayment()`, `MarkAsFailed()`, `MarkAsAbandoned()`, `MarkAsRefunded()`, `SetRevenueBreakdown()`
- [DonationCompletedEvent.cs](src/LankaConnect.Domain/Events/DomainEvents/DonationCompletedEvent.cs) — Domain event for email trigger
- [IDonationRepository.cs](src/LankaConnect.Domain/Events/Repositories/IDonationRepository.cs) — Interface: `GetByCheckoutSessionIdAsync`, `GetByRegistrationIdAsync`, `GetByEventIdAsync`, `GetCompletedByEventIdAsync`, `GetTotalDonationsForEventAsync`, `GetExpiredPendingDonationsAsync`

**Modified files:**
- [Event.cs](src/LankaConnect.Domain/Events/Event.cs) — Add `DonationConfig` (DonationConfiguration?) property + methods: `SetDonationConfiguration()`, `DisableDonations()`, `AreDonationsEnabled()`, `ValidateDonationAmount()`

**Tests (write FIRST):**
- `DonationTests.cs` — 12 tests: Create valid/invalid, status transitions (Pending→Completed, Pending→Failed, Pending→Abandoned, Completed→Refunded), prevent invalid transitions, SetStripeCheckoutSession, CompletePayment raises DonationCompletedEvent
- `DonationConfigurationTests.cs` — 8 tests: Create valid, max 3 suggested amounts, min/max validation, allow custom toggle, disabled state, message length limit
- `EventDonationTests.cs` — 4 tests: SetDonationConfiguration, DisableDonations, AreDonationsEnabled, ValidateDonationAmount against min/max

### Phase 2: Infrastructure — Database

**New files:**
- [DonationEntityConfiguration.cs](src/LankaConnect.Infrastructure/Data/Configurations/DonationEntityConfiguration.cs) — EF Core config: table `events.donations`, Money owned entities with separate columns (`amount`/`amount_currency`, `stripe_fee_amount`/`stripe_fee_currency`, etc.), indexes on `event_id`, `donor_user_id`, `stripe_payment_intent_id`, `status`. FK to Event with `Restrict` delete
- [DonationRepository.cs](src/LankaConnect.Infrastructure/Data/Repositories/DonationRepository.cs) — Implementation with LogContext, Stopwatch, structured logging (follows `RegistrationAdditionRepository` pattern)
- EF Core migration: `AddDonationsTable` — Creates `events.donations` table + `donation_config` JSONB on events

**Modified files:**
- [EventConfiguration.cs](src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs) — Add `OwnsOne(e => e.DonationConfig)` with `ToJson("donation_config")`
- [ApplicationDbContext.cs](src/LankaConnect.Infrastructure/Data/ApplicationDbContext.cs) — Add `DbSet<Donation> Donations`
- [DependencyInjection.cs](src/LankaConnect.Infrastructure/DependencyInjection.cs) — Register `IDonationRepository` → `DonationRepository`

**Command:** `dotnet ef migrations add AddDonationsTable --project src/LankaConnect.Infrastructure`

### Phase 3: Application Layer — Commands

**New files:**
- [CreateDonationCommand.cs](src/LankaConnect.Application/Events/Commands/CreateDonation/CreateDonationCommand.cs) — Record: `EventId`, `DonorName`, `DonorEmail`, `DonorPhone?`, `DonorNotes?`, `Amount`, `Currency`, `SuccessUrl`, `CancelUrl`, `UserId?`. This is the **standalone donation** command (event details page).
- [CreateDonationCommandHandler.cs](src/LankaConnect.Application/Events/Commands/CreateDonation/CreateDonationCommandHandler.cs) — Flow: validate event exists + published + donations enabled → validate amount vs config → `Donation.Create()` → `CreateDonationCheckoutSessionAsync()` → set Stripe session → calculate revenue via `IRevenueCalculatorService` → set breakdown → save → return checkout URL

**Modified files:**
- [RsvpToEventCommand.cs](src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommand.cs) — Add optional donation fields: `DonationAmount?`, `DonorName?`, `DonorEmail?`, `DonorPhone?`, `DonorNotes?`
- [RsvpToEventCommandHandler.cs](src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs) — **Key changes for combined checkout**:
  1. If donation provided + donations enabled: validate amount vs config
  2. Create `Donation.CreateBundledWithRegistration()` with `RegistrationId`
  3. For **paid event**: combine ticket + donation into total → one Stripe Checkout with 2 line items. Metadata includes both `registration_id` and `donation_id`. Calculate fees on combined total, allocate proportionally.
  4. For **free event + donation**: create Stripe Checkout with donation-only line item, `payment_type: "donation"`. Registration stays Preliminary until payment.
  5. For **free event + no donation**: no change to existing flow
- [RegisterAnonymousAttendeeCommand.cs](src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/) — Same donation field additions + combined checkout logic
- [CreateEventCommand.cs](src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs) — Add donation config parameters (IsEnabled, SuggestedAmounts, AllowCustomAmount, MinAmount, MaxAmount, Message)
- [CreateEventCommandHandler.cs](src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs) — Build `DonationConfiguration` and call `event.SetDonationConfiguration()`
- UpdateEventCommand/Handler — Same donation config updates

**Fee allocation for combined checkout (proportional)**:
```
Total = TicketPrice + DonationAmount
StripeFee = Total * 2.9% + $0.30
PlatformFee = Total * 2%
TicketPortion = TicketPrice / Total
DonationPortion = DonationAmount / Total
Registration.StripeFee = StripeFee * TicketPortion
Registration.PlatformCommission = PlatformFee * TicketPortion
Donation.StripeFee = StripeFee * DonationPortion
Donation.PlatformCommission = PlatformFee * DonationPortion
```

**Tests:**
- CreateDonationCommandHandler — 7 tests: happy path, donations disabled, amount below min, amount above max, anonymous donation, event not found, Stripe failure
- RsvpWithDonationTests — 6 tests: paid+donation combined checkout, free+donation standalone checkout, free+no donation unchanged, proportional fee allocation, invalid donation amount, donation disabled

### Phase 4: Application Layer — Queries

**New files:**
- [GetEventDonationsQuery.cs](src/LankaConnect.Application/Events/Queries/GetEventDonations/GetEventDonationsQuery.cs) — Returns `EventDonationsResponse` with `List<DonationDto>` + `DonationSummaryDto` (totals, counts, payout)
- [GetEventDonationsQueryHandler.cs](src/LankaConnect.Application/Events/Queries/GetEventDonations/GetEventDonationsQueryHandler.cs) — Organizer-only, loads completed donations, calculates summary
- [ExportDonationsQuery.cs](src/LankaConnect.Application/Events/Queries/ExportDonations/ExportDonationsQuery.cs) — Excel/CSV export (follows `ExportEventAttendeesQuery` pattern)
- [ExportDonationsQueryHandler.cs](src/LankaConnect.Application/Events/Queries/ExportDonations/ExportDonationsQueryHandler.cs) — Returns `ExportResult { FileContent, FileName, ContentType }`
- DTOs in [Events/Common/](src/LankaConnect.Application/Events/Common/): `DonationDto`, `DonationSummaryDto`, `DonationConfigurationDto`

**Modified files:**
- [EventDto.cs](src/LankaConnect.Application/Events/Common/EventDto.cs) — Add `DonationConfigurationDto? DonationConfig`
- [IExcelExportService.cs](src/LankaConnect.Application/Common/Interfaces/IExcelExportService.cs) — Add `ExportDonations()` method
- [ICsvExportService.cs](src/LankaConnect.Application/Common/Interfaces/ICsvExportService.cs) — Add `ExportDonations()` method
- [ExcelExportService.cs](src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs) — Implement donation export
- [CsvExportService.cs](src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs) — Implement donation export

### Phase 5: Stripe Integration + Webhook

**Modified files:**
- [IStripePaymentService.cs](src/LankaConnect.Application/Common/Interfaces/IStripePaymentService.cs) — Add `CreateDonationCheckoutSessionAsync()` + `CreateDonationCheckoutSessionRequest` + `DonationCheckoutResult`. Also add `LineItems` list to `CreateEventCheckoutSessionRequest` for multi-line-item support.
- [StripePaymentService.cs](src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs):
  - New `CreateDonationCheckoutSessionAsync()` method for standalone donations (follows `CreateAdditionCheckoutSessionAsync` pattern): single line item "Donation: {EventTitle}", metadata `payment_type: "donation"`, `donation_id`, 24h expiration
  - Modify `CreateEventCheckoutSessionAsync()` to support optional `LineItems` list. When provided, renders multiple line items (ticket + donation). When null, backward-compatible single item. Metadata includes `donation_id` when donation is bundled.
- [PaymentsController.cs](src/LankaConnect.API/Controllers/PaymentsController.cs):
  - Add `payment_type: "donation"` routing → `HandleDonationCheckoutCompletedAsync()` for standalone donations
  - Modify existing `HandleCheckoutSessionCompletedAsync()`: after confirming Registration, check if `donation_id` exists in metadata. If yes, also load and confirm the linked Donation record. This handles the **combined checkout** scenario where one Stripe session confirms both entities.
  - Handle donation expiry in `HandleCheckoutSessionExpiredAsync()` — mark both Registration (Abandoned) and linked Donation (Abandoned) if present.

**Webhook flow for combined checkout (paid event + donation)**:
```
1. checkout.session.completed received
2. No payment_type metadata (or payment_type = "registration") → existing handler
3. Confirm Registration (Preliminary → Confirmed) as normal
4. Check metadata for "donation_id" → if present:
   a. Load Donation by donation_id
   b. Call donation.CompletePayment(paymentIntentId)
   c. Set revenue breakdown (proportional allocation)
   d. Save both
5. Domain events dispatched: PaymentCompletedEvent + DonationCompletedEvent
```

**Webhook flow for standalone donation (event details page / free event)**:
```
1. checkout.session.completed received
2. payment_type = "donation" → HandleDonationCheckoutCompletedAsync()
3. Load Donation by donation_id
4. Call donation.CompletePayment(paymentIntentId)
5. Set revenue breakdown (full fee on donation)
6. Save. Domain event: DonationCompletedEvent
```

**Tests:** 6 tests: standalone donation checkout creation, standalone webhook completion, combined checkout with both registration+donation confirmed, expired checkout → both Abandoned, idempotency, free event donation checkout

### Phase 6: Email + API

**New files:**
- [DonationReceiptEmailParams.cs](src/LankaConnect.Shared/Email/Contracts/DonationReceiptEmailParams.cs) — Uses `EmailTemplateContract` constants. Fields: DonorName, DonorEmail, EventTitle, Amount, PaymentIntentId, PaymentDate, EventDetailsUrl
- [DonationCompletedEventHandler.cs](src/LankaConnect.Application/Events/EventHandlers/DonationCompletedEventHandler.cs) — Fire-and-forget email via `Task.Run()` (follows `UserCommittedToSignUpEventHandler` pattern)
- [DonationsController.cs](src/LankaConnect.API/Controllers/DonationsController.cs) — Route: `api/events/{eventId}/donations`
  - `POST /` — `[AllowAnonymous]` Create donation → return checkout URL
  - `GET /` — `[Authorize]` List donations (organizer only)
  - `GET /summary` — `[Authorize]` Donation summary (organizer only)
  - `GET /export` — `[Authorize]` Export donations Excel/CSV (organizer only)

**Modified files:**
- [EmailTemplateContract.cs](src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs) — Add `DonationReceipt` template name + parameter constants
- Database migration for email template insert into `communications.email_templates`

### Phase 7: Frontend

**New files:**
- [DonationSection.tsx](web/src/presentation/components/features/events/DonationSection.tsx) — **Public standalone donation UI** on event details page. Shows when `donationConfig?.isEnabled`. Contains: organizer message, suggested amount buttons (pill-shaped), custom amount input, donor info form (name*, email*, phone, notes), "Donate" button → creates Stripe checkout → redirects. Visible to all visitors at any time.
- [DonationOptionInForm.tsx](web/src/presentation/components/features/events/DonationOptionInForm.tsx) — **Donation add-on within registration form**. Lightweight version of DonationSection shown inside EventRegistrationForm when donations are enabled. Shows suggested amounts + custom input. Donor info auto-populated from registration contact fields. Donation amount passed along with RSVP data for combined checkout.
- [DonationConfigForm.tsx](web/src/presentation/components/features/events/DonationConfigForm.tsx) — **Organizer config section** in event create/edit form. Toggle + suggested amounts (up to 3) + allow custom amount + min/max + message textarea. Follows `PublishOrganizerContact` toggle pattern
- [DonationsManagementTab.tsx](web/src/presentation/components/features/events/DonationsManagementTab.tsx) — **Organizer management tab** on manage page. Summary cards (total, count, avg, payout), donations table (name, email, phone, amount, status, date, notes), export button (Excel/CSV). Follows `AttendeeManagementTab` pattern
- [useDonations.ts](web/src/presentation/hooks/useDonations.ts) — React Query hooks: `useEventDonations()`, `useDonationSummary()`, `useCreateDonation()`

**Modified files:**
- [events.types.ts](web/src/infrastructure/api/types/events.types.ts) — Add `DonationConfigurationDto`, `DonationDto`, `DonationSummaryDto`, `CreateDonationRequest`, `CreateDonationResponse`, `EventDonationsResponse`. Add `donationAmount?`, `donorName?`, `donorPhone?`, `donorNotes?` to `RsvpRequest` and `AnonymousRegistrationRequest`
- [events.repository.ts](web/src/infrastructure/api/repositories/events.repository.ts) — Add `createDonation()`, `getEventDonations()`, `getDonationSummary()`, `exportDonations()`
- [page.tsx (event details)](web/src/app/events/[id]/page.tsx) — Add `<DonationSection>` standalone component (visible to all visitors at any time)
- [page.tsx (event manage)](web/src/app/events/[id]/manage/page.tsx) — Add "Donations" tab with `<DonationsManagementTab>`
- [EventCreationForm.tsx](web/src/presentation/components/features/events/EventCreationForm.tsx) — Add `<DonationConfigForm>` section
- [EventEditForm.tsx](web/src/presentation/components/features/events/EventEditForm.tsx) — Add `<DonationConfigForm>` section
- [EventRegistrationForm.tsx](web/src/presentation/components/features/events/EventRegistrationForm.tsx) — Add `<DonationOptionInForm>` component when `donationConfig?.isEnabled`. Include `donationAmount` in RSVP submission data for combined checkout
- [EventDto type](web/src/infrastructure/api/types/events.types.ts) — Add `donationConfig?: DonationConfigurationDto`

### Phase 8: E2E Testing & Deployment

1. `dotnet test` — all tests pass (90%+ coverage)
2. `dotnet ef database update` — migration applies, `events.donations` table created
3. Push to develop → GitHub Actions deploys backend + frontend to staging
4. Test API:
   ```bash
   # Login
   curl -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
     -H 'Content-Type: application/json' \
     -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true}'

   # Create event with donation config enabled
   # Create donation → verify Stripe checkout URL returned
   # Complete payment → verify webhook confirms donation
   # GET /donations → verify donation appears in list
   # GET /donations/summary → verify totals
   # GET /donations/export → verify Excel download
   ```
5. Frontend: DonationSection visible on event page, config works in create/edit, management tab shows data
6. Update PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md, TASK_SYNCHRONIZATION_STRATEGY.md

---

## Key Patterns Reused

| Pattern | Source File | Reuse |
|---------|------------|-------|
| Standalone payment entity with lifecycle | `RegistrationAddition.cs` | `Donation.cs` (Pending→Completed→Refunded) |
| `payment_type` metadata webhook routing | `PaymentsController.cs` ("addition") | New `"donation"` route |
| Separate Stripe checkout session | `StripePaymentService.CreateAdditionCheckoutSessionAsync()` | `CreateDonationCheckoutSessionAsync()` |
| Revenue breakdown via service | `IRevenueCalculatorService.CalculateBreakdownAsync()` | Donation fee calculation |
| JSONB value object on Event | `TicketPricing` + `EventConfiguration.cs` | `DonationConfiguration` as JSONB |
| Feature toggle with config | `Event.PublishOrganizerContact` | `Event.DonationConfig` |
| EF Core Money owned entity | `RegistrationAdditionConfiguration.cs` | Donation Money columns |
| Specialized repository | `IRegistrationAdditionRepository` | `IDonationRepository` |
| Fire-and-forget email handler | `UserCommittedToSignUpEventHandler.cs` | `DonationCompletedEventHandler` |
| Export service (Excel/CSV) | `ExportEventAttendeesQuery` | `ExportDonationsQuery` |
| Tabbed organizer management | `AttendeeManagementTab.tsx` | `DonationsManagementTab.tsx` |
| Anonymous endpoint | `RegisterAnonymousAttendeeCommand` | `CreateDonation` with `[AllowAnonymous]` |

---

## Architect Review: Backward Compatibility Safeguards

### CRITICAL PRINCIPLE: Donation logic must NEVER block or break registration processing

### Critical Issues Found (must address during implementation)

**C1: StripePaymentService hardcoded single LineItem**
- Current code builds `LineItems` inline (lines 93-114). Adding optional `LineItems` to request DTO requires the service to check: `if (request.LineItems != null && request.LineItems.Any()) { use them } else { build legacy single item }`.
- **Guard**: Null/empty `LineItems` → falls back to existing behavior. All existing callers unaffected.

**C2: Webhook donation exception must NOT kill registration confirmation**
- If donation lookup throws after registration `CommitAsync`, HTTP 500 goes to Stripe → retry skips due to idempotency → donation stuck in Pending.
- **Guard**: Donation processing in webhook must be wrapped in its OWN isolated try-catch. Registration confirmation commits FIRST, then donation confirmation in separate try-catch. If donation fails, log error but return 200 OK to Stripe.

**C3: DonationAmount: 0 creates invalid Stripe session**
- Stripe rejects $0.00 line items (minimum $0.50). If `DonationAmount` is `0m` (not null), checkout creation fails entirely.
- **Guard**: Always check `request.DonationAmount.HasValue && request.DonationAmount.Value > 0` — never just `HasValue`. Treat `0` same as `null`.

**C4: checkout.session.expired must clean up donations**
- Combined checkout expiry must mark BOTH Registration (Abandoned) AND linked Donation (Abandoned).
- **Guard**: In `HandleCheckoutSessionExpiredAsync`, after marking registration abandoned, check metadata for `donation_id` and mark donation abandoned too (in separate try-catch).

**C5: EF Core OwnsOne(ToJson) requires nested VO configuration**
- `DonationConfiguration` value object must NOT contain nested `Money` types. Use plain `decimal?` for amounts + `string` for currency to avoid nested owned entity issues.
- **Guard**: Keep `DonationConfiguration` flat — only primitive types. No nested Money value objects.

### Implementation Guards (apply to every modified file)

| Modified File | Guard Required |
|--------------|----------------|
| `RsvpToEventCommandHandler` | Donation logic in separate private method. All donation fields checked with `> 0` not just `!= null`. Revenue breakdown calculated on ticket-only amount, then donation breakdown calculated separately. If donation processing fails, registration still succeeds. |
| `PaymentsController` webhook | Donation lookup via `metadata.TryGetValue("donation_id", out var donationIdStr)`. Donation confirmation in isolated try-catch AFTER registration commits. Never throw from donation code. |
| `StripePaymentService` | `BuildLineItems()` checks `request.LineItems?.Any() == true`. Null or empty → legacy single-item path. New `CreateDonationCheckoutSessionAsync()` is entirely new code, no existing code touched. |
| `Event.cs` | `DonationConfig` is nullable. All new methods are additive. No existing methods modified. `AreDonationsEnabled()` returns `false` when `DonationConfig` is null. |
| `EventConfiguration.cs` | `OwnsOne(e => e.DonationConfig, ...)` with `ToJson("donation_config")`. Column is nullable. Existing events load with `DonationConfig = null` — no migration of existing data needed. |
| `CreateEventCommand` | All donation params have `= null` defaults. Existing validators won't reject them. FluentValidation rules for donation fields added as separate rule set, conditional on `DonationsEnabled == true`. |
| `EventRegistrationForm.tsx` | `{event.donationConfig?.isEnabled === true && <DonationOptionInForm />}`. Triple check: optional chain + strict equality to `true`. Undefined/null/false all skip rendering. |
| `EventCreationForm.tsx` | Zod schema: donation fields added with `.optional().nullable()`. react-hook-form `defaultValues` include donation fields as `null`. No breakage to existing form fields. |
| Event detail page | `{event.donationConfig?.isEnabled === true && <DonationSection />}`. Existing events without config show nothing. |

### Regression Tests Required (verify existing behavior unchanged)

**RsvpToEventCommandHandler (11 tests):**
1. Paid event RSVP without donation → same checkout URL, same revenue breakdown (MUST match current behavior exactly)
2. Free event RSVP without donation → null checkout URL, Confirmed status (unchanged)
3. Paid event with dual pricing, no donation → correct price calculation unchanged
4. Paid event with group pricing, no donation → correct tier calculation unchanged
5. Duplicate RSVP check still works
6. Capacity check still works
7. Paid event + donation → combined checkout with 2 line items
8. Free event + donation → standalone donation checkout, registration Preliminary
9. DonationAmount = 0 → treated as no donation
10. DonationAmount = null → treated as no donation
11. Invalid donation amount → error returned, registration NOT created

**PaymentsController webhook (10 tests):**
1. Existing registration payment completed (no donation_id metadata) → works exactly as before
2. Existing addition payment completed → routes to addition handler unchanged
3. Registration + donation combined → both confirmed in order
4. Donation confirmation failure → registration still confirmed, donation logged as error
5. Standalone donation completed → donation confirmed
6. Duplicate webhook (idempotency) → skipped
7. Expired registration checkout (no donation) → abandoned
8. Expired combined checkout → both registration and donation abandoned
9. Expired donation-only checkout → donation abandoned
10. Unknown payment_type → ignored, falls through to registration handler

**StripePaymentService (5 tests):**
1. `CreateEventCheckoutSessionAsync` with null LineItems → existing single-item behavior
2. `CreateEventCheckoutSessionAsync` with empty LineItems → existing single-item behavior
3. `CreateEventCheckoutSessionAsync` with LineItems → multi-item session
4. `CreateDonationCheckoutSessionAsync` → new standalone session
5. Existing `CreateAdditionCheckoutSessionAsync` → unchanged

### Deployment Safety Plan

**Phase 1: Database only** (zero risk)
- Run migration: `events.donations` table + `donation_config` JSONB column
- Verify: existing events load correctly, `donation_config` is NULL for all
- Rollback: `dotnet ef database update <previous_migration>`

**Phase 2: Backend API** (low risk with guards)
- Deploy with all safeguards above
- Verify: existing RSVP, existing payment webhooks, existing event creation all work identically
- Test: create event WITHOUT donation config, RSVP, complete payment → unchanged behavior
- Then: create event WITH donation config, test all 3 scenarios

**Phase 3: Frontend** (medium risk — UI changes)
- Deploy with conditional rendering guards
- Verify: existing event pages, create/edit forms, registration forms all render correctly
- Then: create donation-enabled event, test all UI flows

**Phase 4: Feature activation**
- Enable donations on a test event, test full flow end-to-end
- Monitor Azure logs for any errors

## Risk Mitigation (original + architect additions)

| Risk | Mitigation |
|------|-----------|
| Breaking existing RSVP flow | All donation fields optional with null defaults. Guard: `> 0` check. Separate processing method. |
| Breaking existing webhook | `TryGetValue` for all metadata. Donation in isolated try-catch AFTER registration commits. |
| Breaking existing Stripe checkout | `LineItems` null/empty → legacy single-item path. No existing code path changed. |
| Breaking existing event loading | `DonationConfig` is nullable. Null → donations disabled. No migration of existing data. |
| EF Core JSONB nested types | Keep `DonationConfiguration` flat (decimals, not Money). No nested owned entities. |
| Frontend rendering | Triple guard: `?.isEnabled === true`. Undefined/null/false all safe. |
| Combined checkout partial failure | Registration commits first. Donation in separate try-catch. Never lose registration. |
| Fee calculation | Ticket revenue calculated on ticket amount ONLY (unchanged). Donation revenue calculated separately. |
| In-flight webhooks during deploy | No `donation_id` metadata → donation code skipped entirely via `TryGetValue`. |
| Stripe $0 donation | Guard: `> 0` check. Treat `0` as no donation. |

---

## Complete File Summary

**New files: ~20**
| Layer | File | Purpose |
|-------|------|---------|
| Domain | `DonationStatus.cs` | Status enum |
| Domain | `DonationConfiguration.cs` (VO) | Event donation config |
| Domain | `Donation.cs` | Entity with full lifecycle |
| Domain | `DonationCompletedEvent.cs` | Domain event |
| Domain | `IDonationRepository.cs` | Repository interface |
| Infra | `DonationEntityConfiguration.cs` | EF Core config |
| Infra | `DonationRepository.cs` | Repository impl |
| Infra | Migration | `events.donations` table |
| App | `CreateDonationCommand.cs` | Command + handler |
| App | `GetEventDonationsQuery.cs` | Query + handler |
| App | `ExportDonationsQuery.cs` | Export query + handler |
| App | `DonationCompletedEventHandler.cs` | Email sender |
| App | `DonationDto.cs`, `DonationSummaryDto.cs` | DTOs |
| Shared | `DonationReceiptEmailParams.cs` | Email params |
| API | `DonationsController.cs` | REST endpoints |
| Frontend | `DonationSection.tsx` | Public standalone donation UI |
| Frontend | `DonationOptionInForm.tsx` | Donation add-on in registration form |
| Frontend | `DonationConfigForm.tsx` | Organizer config |
| Frontend | `DonationsManagementTab.tsx` | Organizer management |
| Frontend | `useDonations.ts` | React hooks |

**Modified files: ~16**
| Layer | File | Change |
|-------|------|--------|
| Domain | `Event.cs` | Add `DonationConfig` + methods |
| Infra | `EventConfiguration.cs` | JSONB for `DonationConfig` |
| Infra | `ApplicationDbContext.cs` | Add `DbSet<Donation>` |
| Infra | `DependencyInjection.cs` | Register repository |
| Infra | `StripePaymentService.cs` | Add donation checkout method |
| App | `IStripePaymentService.cs` | Add interface + DTOs |
| App | `RsvpToEventCommand.cs` + Handler | Donation fields + combined checkout |
| App | `RegisterAnonymousAttendeeCommand` + Handler | Same donation additions |
| App | `CreateEventCommand.cs` + Handler | Donation config params |
| App | `EventDto.cs` | Add `DonationConfigurationDto` |
| App | Export service interfaces + impls | Donation export methods |
| Shared | `EmailTemplateContract.cs` | Donation template constants |
| API | `PaymentsController.cs` | Webhook routing for donations |
| Frontend | `events.types.ts` | Donation types |
| Frontend | `events.repository.ts` | Donation API methods |
| Frontend | Event details page | Add DonationSection |
| Frontend | Event manage page | Add Donations tab |
| Frontend | Event create/edit forms | Add DonationConfigForm |
| Frontend | `EventRegistrationForm.tsx` | Add DonationOptionInForm |
