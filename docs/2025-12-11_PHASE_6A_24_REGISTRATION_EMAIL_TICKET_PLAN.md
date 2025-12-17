# Phase 6A.24: Registration Email Enhancement & Ticket Generation

**Created**: 2025-12-11
**Status**: In Progress
**Priority**: HIGH

---

## Executive Summary

This phase implements enhanced registration emails with attendee details and ticket generation with QR codes for paid events. The email infrastructure (Azure Communication Services) is already complete and tested.

---

## Current State Analysis

### What's WORKING:
| Component | Status | Details |
|-----------|--------|---------|
| Attendee Collection | Done | `EventRegistrationForm.tsx` - multi-attendee with name/age |
| Payment Redirect | Done | Stripe checkout session creation |
| Payment Webhook | Done | `PaymentsController.HandleCheckoutSessionCompletedAsync()` |
| Registration Confirmation | Done | `CompletePayment()` sets status to Confirmed |
| Attendee Storage | Done | `Registration.Attendees` list in database |
| Attendee Display in UI | Done | Event details page shows all attendees |
| Azure Email Service | Done | `AzureEmailService.cs` - fully configured & tested |
| Email Infrastructure | Done | Azure Communication Services deployed & working |

### What's BROKEN/MISSING:
| Issue | Severity | Details |
|-------|----------|---------|
| Email timing | CRITICAL | Email fires BEFORE payment for paid events |
| Email content | HIGH | Missing attendee names/ages, only shows quantity |
| Anonymous email | HIGH | No handler for `AnonymousRegistrationConfirmedEvent` |
| Ticket generation | HIGH | No QR/PDF infrastructure exists |
| Ticket display | MEDIUM | No ticket component in event details |

---

## Requirements

### FREE Event Flow:
1. Collect attendee details from user
2. Complete registration (store in database)
3. Display attendees in event details page
4. **Send email with registration details including all attendee names/ages**

### PAID Event Flow:
1. Collect attendee details from user
2. Direct to payment page and collect payment
3. If payment succeeds, complete registration
4. Display attendees in event details page
5. **Generate Ticket with QR code**
6. **Send email with registration details AND tickets**
7. **Display Ticket in event details page (view, download, resend email)**

---

## Decision Flow Diagram

```
User Submits Registration
         |
         v
+-----------------------------+
|  Is Event FREE or PAID?     |
+-----------------------------+
         |
    +----+----+
    |         |
  FREE      PAID
    |         |
    v         v
+---------+  +------------------+
| Create  |  | Create           |
| Registr.|  | Registration     |
| (Conf.) |  | (Pending)        |
+---------+  +------------------+
     |                |
     v                v
+---------+  +------------------+
| Send    |  | Redirect to      |
| Email   |  | Stripe Checkout  |
| NOW     |  +------------------+
+---------+           |
     |                v
     |       +------------------+
     |       | Payment Success  |
     |       | Webhook          |
     |       +------------------+
     |                |
     |                v
     |       +------------------+
     |       | Update Status    |
     |       | to Confirmed     |
     |       +------------------+
     |                |
     |                v
     |       +------------------+
     |       | Generate         |
     |       | Ticket + QR      |
     |       +------------------+
     |                |
     |                v
     |       +------------------+
     |       | Send Email       |
     |       | WITH Ticket      |
     |       +------------------+
     |                |
     +-------+--------+
             |
             v
     +------------------+
     | Display in       |
     | Event Details    |
     +------------------+
```

---

## Implementation Plan

### Phase 1A: Fix FREE Event Email Content
**Problem**: Email fires correctly but missing attendee names/ages.

**Solution**: Enhance `RegistrationConfirmedEventHandler` to include attendee details.

**Current Email Parameters**:
```
UserName, EventTitle, EventStartDate, Quantity  <- Only has count!
```

**Enhanced Email Parameters**:
```
UserName, EventTitle, EventStartDate, EventStartTime, EventEndDate,
EventLocation, RegistrationDate,
Attendees: [{ Name, Age }],  <- NEW: Individual attendee details
ContactEmail, ContactPhone   <- NEW: Contact info
```

**Files to Modify**:
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
  - Add IRegistrationRepository dependency
  - Fetch registration to get attendees
  - Pass attendee list to email template
  - Skip email for paid events (handled by PaymentCompletedEventHandler)

### Phase 1B: Create Anonymous Registration Email Handler
**Problem**: `AnonymousRegistrationConfirmedEvent` fires but NO handler exists.

**Files to Create**:
- `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`

### Phase 1C: Fix PAID Event Email Timing
**Problem**: `RegistrationConfirmedEvent` fires when registration is created (BEFORE payment).

**Solution**:
1. Modify `RegistrationConfirmedEventHandler` to SKIP paid events
2. Create `PaymentCompletedEvent` that fires AFTER payment webhook
3. Create `PaymentCompletedEventHandler` that sends email + generates ticket

**Files to Create**:
- `src/LankaConnect.Domain/Events/DomainEvents/PaymentCompletedEvent.cs`
- `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`

**Files to Modify**:
- `src/LankaConnect.Domain/Events/Registration.cs` - Raise PaymentCompletedEvent in CompletePayment()

### Phase 2: Create Email Templates
**Files to Create**:
- `src/LankaConnect.Infrastructure/Email/Templates/RegistrationConfirmation/subject.txt`
- `src/LankaConnect.Infrastructure/Email/Templates/RegistrationConfirmation/body.html`
- `src/LankaConnect.Infrastructure/Email/Templates/RegistrationConfirmation/body.txt`

### Phase 3: Add NuGet Packages
**Packages to Add** to `LankaConnect.Infrastructure.csproj`:
- `QRCoder` (v1.6.0) - QR code generation
- `QuestPDF` (v2024.10.2) - PDF generation

### Phase 4: Create Ticket Domain Entity
**Files to Create**:
- `src/LankaConnect.Domain/Events/Entities/Ticket.cs`
- `src/LankaConnect.Domain/Events/Repositories/ITicketRepository.cs`

**Ticket Entity**:
```csharp
public class Ticket {
    Guid Id
    Guid RegistrationId
    Guid EventId
    Guid? UserId
    string TicketCode      // Unique reference (e.g., "LC-2024-ABC123")
    string QrCodeData      // Encoded validation data
    string? PdfBlobUrl     // Azure Blob Storage URL
    bool IsValid           // For check-in validation
    DateTime? ValidatedAt  // When ticket was scanned
    DateTime CreatedAt
    DateTime ExpiresAt
}
```

### Phase 5: Implement Ticket Services
**Files to Create**:
- `src/LankaConnect.Application/Common/Interfaces/IQrCodeService.cs`
- `src/LankaConnect.Application/Common/Interfaces/IPdfTicketService.cs`
- `src/LankaConnect.Application/Common/Interfaces/ITicketService.cs`
- `src/LankaConnect.Infrastructure/Services/Tickets/QrCodeService.cs`
- `src/LankaConnect.Infrastructure/Services/Tickets/PdfTicketService.cs`
- `src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs`
- `src/LankaConnect.Infrastructure/Persistence/Repositories/TicketRepository.cs`

### Phase 6: Database Migration
**New Table**: `Tickets`
```sql
CREATE TABLE "Tickets" (
    "Id" UUID PRIMARY KEY,
    "RegistrationId" UUID NOT NULL REFERENCES "Registrations"("Id"),
    "EventId" UUID NOT NULL REFERENCES "Events"("Id"),
    "UserId" UUID REFERENCES "Users"("Id"),
    "TicketCode" VARCHAR(50) UNIQUE NOT NULL,
    "QrCodeData" TEXT NOT NULL,
    "PdfBlobUrl" VARCHAR(500),
    "IsValid" BOOLEAN DEFAULT TRUE,
    "ValidatedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ExpiresAt" TIMESTAMP NOT NULL
);
CREATE INDEX "IX_Tickets_RegistrationId" ON "Tickets"("RegistrationId");
CREATE INDEX "IX_Tickets_TicketCode" ON "Tickets"("TicketCode");
```

### Phase 7: Integrate Ticket Generation in PaymentCompletedEventHandler
**Workflow**:
1. Payment webhook confirms success
2. `PaymentCompletedEventHandler` triggers
3. Generate Ticket entity with unique code
4. Generate QR code (embed ticket code + event ID)
5. Generate PDF ticket with QR code
6. Upload PDF to Azure Blob Storage
7. Send email with ticket attached/linked

### Phase 8: API Endpoints for Tickets
**New Endpoints**:
```
GET  /api/events/{eventId}/registrations/{registrationId}/ticket
     -> Returns TicketDto with ticket details

GET  /api/events/{eventId}/registrations/{registrationId}/ticket/pdf
     -> Returns PDF file download

POST /api/events/{eventId}/registrations/{registrationId}/ticket/resend-email
     -> Resends ticket email to registration contact
```

**Files to Create**:
- `src/LankaConnect.Application/Events/Queries/GetTicketQuery.cs`
- `src/LankaConnect.Application/Events/Queries/GetTicketPdfQuery.cs`
- `src/LankaConnect.Application/Events/Commands/ResendTicketEmailCommand.cs`
- `src/LankaConnect.Application/Events/DTOs/TicketDto.cs`

**Files to Modify**:
- `src/LankaConnect.API/Controllers/RegistrationsController.cs`

### Phase 9: Frontend Ticket Display
**Files to Create**:
- `web/src/presentation/components/features/events/TicketSection.tsx`

**Files to Modify**:
- `web/src/app/events/[id]/page.tsx` - Add TicketSection for paid registrations
- `web/src/infrastructure/api/repositories/events.repository.ts` - Add ticket API methods
- `web/src/infrastructure/api/types/events.types.ts` - Add TicketDto type

---

## File Summary

### Files to Create (17):

**Domain Layer (2)**:
1. `src/LankaConnect.Domain/Events/DomainEvents/PaymentCompletedEvent.cs`
2. `src/LankaConnect.Domain/Events/Entities/Ticket.cs`

**Application Layer - Event Handlers (2)**:
3. `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
4. `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`

**Application Layer - Interfaces (4)**:
5. `src/LankaConnect.Domain/Events/Repositories/ITicketRepository.cs`
6. `src/LankaConnect.Application/Common/Interfaces/IQrCodeService.cs`
7. `src/LankaConnect.Application/Common/Interfaces/IPdfTicketService.cs`
8. `src/LankaConnect.Application/Common/Interfaces/ITicketService.cs`

**Application Layer - Queries/Commands (4)**:
9. `src/LankaConnect.Application/Events/Queries/GetTicketQuery.cs`
10. `src/LankaConnect.Application/Events/Queries/GetTicketPdfQuery.cs`
11. `src/LankaConnect.Application/Events/Commands/ResendTicketEmailCommand.cs`
12. `src/LankaConnect.Application/Events/DTOs/TicketDto.cs`

**Infrastructure Layer (4)**:
13. `src/LankaConnect.Infrastructure/Services/Tickets/QrCodeService.cs`
14. `src/LankaConnect.Infrastructure/Services/Tickets/PdfTicketService.cs`
15. `src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs`
16. `src/LankaConnect.Infrastructure/Persistence/Repositories/TicketRepository.cs`

**Frontend (1)**:
17. `web/src/presentation/components/features/events/TicketSection.tsx`

### Existing Files to Modify (8):
1. `src/LankaConnect.Domain/Events/Registration.cs` - Raise PaymentCompletedEvent
2. `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs` - Add attendee details, skip paid events
3. `src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj` - Add QRCoder & QuestPDF
4. `src/LankaConnect.Infrastructure/Persistence/ApplicationDbContext.cs` - Add Tickets DbSet
5. `src/LankaConnect.API/Controllers/RegistrationsController.cs` - Add ticket endpoints
6. `web/src/app/events/[id]/page.tsx` - Add TicketSection
7. `web/src/infrastructure/api/repositories/events.repository.ts` - Add ticket methods
8. `web/src/infrastructure/api/types/events.types.ts` - Add TicketDto

---

## Testing Strategy

### Unit Tests:
- Ticket entity validation
- QrCodeService generation
- PdfTicketService generation
- RegistrationConfirmedEventHandler (with attendee details)
- PaymentCompletedEventHandler
- AnonymousRegistrationConfirmedEventHandler

### Integration Tests:
- Ticket API endpoints
- Email sending with attendee details

### E2E Test Flow:
1. Register for FREE event -> Receive email with attendee details
2. Register for PAID event -> Pay -> Receive email with ticket -> View/download ticket

---

## Best Practices Compliance

- [x] TDD approach with tests first
- [x] Clean Architecture (Domain -> Application -> Infrastructure -> API)
- [x] No breaking changes to existing APIs
- [x] EF Core migrations for database changes
- [x] Azure staging deployment
- [x] Proper error handling and validation
- [x] Authorization (only registration owner can access ticket)
- [x] Leverage existing `AzureEmailService.cs` - no duplicate email code

---

## Dependencies

### Already Completed:
- Azure Communication Services: `lankaconnect-communication`
- Email Service: `lankaconnect-email`
- Azure Managed Domain configured
- `Azure.Communication.Email` NuGet package (v1.1.0)
- `AzureEmailService.cs` implementation
- Stripe payment integration

### To Be Added:
- `QRCoder` NuGet package
- `QuestPDF` NuGet package

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Email template rendering issues | Medium | Medium | Test templates locally first |
| QR code library compatibility | Low | Medium | Use well-maintained QRCoder |
| PDF generation performance | Medium | Low | Generate async, cache results |
| Azure Blob Storage upload failures | Low | Medium | Implement retry logic |

---

## Success Criteria

1. FREE event registration sends email with all attendee names and ages
2. Anonymous users receive registration confirmation email
3. PAID event email only sends AFTER successful payment
4. PAID event registration generates ticket with QR code
5. Users can view, download, and resend ticket from event details page
6. All existing functionality remains intact
7. Build passes with zero errors
8. Deployed to Azure staging successfully