# Email System Implementation Plan - Remaining Work

**Document Created**: 2025-12-31
**Last Updated**: 2025-12-31
**Purpose**: Comprehensive plan for remaining email system features to prevent requirement loss
**Reference**: [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Original Requirements Status](#original-requirements-status)
3. [Phase 6A.60: Signup Commitment Emails](#phase-6a60-signup-commitment-emails)
4. [Phase 6A.61: Manual Event Email Sending](#phase-6a61-manual-event-email-sending)
5. [Implementation Order](#implementation-order)
6. [Master TODO List](#master-todo-list)
7. [Testing Strategy](#testing-strategy)
8. [Deployment Verification](#deployment-verification)
9. [Phase Number Management](#phase-number-management)

---

## Executive Summary

### Purpose
This document serves as the **single source of truth** for remaining email system work. It prevents the "Phase 6A problem" where requirements discussed in conversation were never documented in primary tracking documents.

### Remaining Features
- **Phase 6A.60**: Signup Commitment Emails (3-4 hours) - Ready for implementation
- **Phase 6A.61**: Manual Event Email Sending (15-17 hours) - Architecture complete

### Completed Features
- ✅ **Requirement 3**: Registration Cancellation Emails (Phase 6A.52)
- ✅ **Requirement 4**: Member Email Verification (Phase 6A.53)
- ✅ **Requirement 5**: Email Template Consolidation (Phase 6A.54)

### Phase Number Verification
- ✅ Phase 6A.60: **AVAILABLE** (verified against master index)
- ✅ Phase 6A.61: **AVAILABLE** (verified against master index)
- Next available: 6A.62+

---

## Original Requirements Status

### User's 5 Original Email System Requirements

| # | Requirement | Status | Phase | Notes |
|---|-------------|--------|-------|-------|
| 1 | Event publication email with manual send option | ❌ Not Started | 6A.61 | Architecture complete |
| 2 | Signup commitment confirmation emails | ❌ Not Started | 6A.60 | Architecture complete |
| 3a | Registration cancellation emails (user cancels) | 🔧 **BUG FIX IN PROGRESS** | 6A.62 | Template name mismatch - using wrong name |
| 3b | Event cancellation emails (organizer cancels) | ⚠️ **NEEDS TEMPLATE** | 6A.63 | Handler exists but uses inline HTML |
| 4 | Member email verification | ✅ **COMPLETE** | 6A.53 | Token-based verification |
| 5 | Consistent email templates | ✅ **COMPLETE** | 6A.54 | Orange/rose gradient branding |

### Implementation Evidence

**Requirement 3 - Registration Cancellation**:
- File: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\RegistrationCancelledEventHandler.cs`
- Template: `RsvpCancellation`
- Verification: Deployed to staging, tested

**Requirement 4 - Email Verification**:
- File: `c:\Work\LankaConnect\src\LankaConnect.Application\Communications\Commands\VerifyEmail\VerifyEmailCommand.cs`
- Template: `member-email-verification`
- Verification: Deployed to staging, 8/8 issues resolved

**Requirement 5 - Template Consistency**:
- Migration: `20251227232000_Phase6A54_SeedNewEmailTemplates.cs`
- Templates: All use orange (#f97316) to rose (#e11d48) gradient
- Verification: Needs staging email preview test

---

## Phase 6A.62: Fix Registration Cancellation Email (URGENT BUG FIX)

### Overview
Fix template name mismatch preventing registration cancellation emails from being sent.

### Bug Description
**Issue**: When users cancel their event registration, no email is sent.

**Root Cause**: Template name mismatch
- **Database template name**: `registration-cancellation`
- **Code uses**: `RsvpCancellation`

**File**: [src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs:70](src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs#L70)

### Fix Applied
Changed line 70 from:
```csharp
"RsvpCancellation"  // ❌ WRONG
```
to:
```csharp
"registration-cancellation"  // ✅ CORRECT
```

### Testing Required
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Deploy to staging
- [ ] Cancel a test registration
- [ ] Verify email received with correct template
- [ ] Check staging logs for success message
- [ ] Deploy to production

### Estimated Time
**15 minutes** (simple one-line fix + deployment verification)

---

## Phase 6A.63: Migrate Event Cancellation to Template System

### Overview
Migrate `EventCancelledEventHandler` from inline HTML generation to templated email system for consistency.

### Current Implementation
**File**: [src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs](src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs)

**Problem**:
- Line 107: Uses inline `GenerateEventCancelledHtml()` method
- Line 142-160: Hardcoded HTML in code
- NOT using templated email system
- Inconsistent with other emails

### Required Changes

#### 1. Create Email Template in Database
**Migration**: `20260101000001_Phase6A63_EventCancellationTemplate.cs`

**Template Details**:
- Name: `event-cancelled-notification`
- Type: `EventCancellationNotification`
- Category: `EventManagement`
- Branding: Orange/rose gradient matching other templates

**Template Variables**:
- `{{UserName}}` - User's full name
- `{{EventTitle}}` - Event name
- `{{EventStartDate}}` - Original event date
- `{{EventStartTime}}` - Original event time
- `{{Reason}}` - Cancellation reason from organizer
- `{{CancelledAt}}` - When event was cancelled

#### 2. Update Event Handler
**File**: `EventCancelledEventHandler.cs`

**Changes**:
- Replace `SendBulkEmailAsync()` with loop of `SendTemplatedEmailAsync()`
- Remove `GenerateEventCancelledHtml()` method (lines 142-160)
- Update template name to `event-cancelled-notification`
- Add comprehensive logging with `[Phase 6A.63]` prefix

**Code Change** (lines 100-112):
```csharp
// OLD: Inline HTML generation
var emailMessage = new EmailMessageDto
{
    ToEmail = user.Email.Value,
    ToName = $"{user.FirstName} {user.LastName}",
    Subject = $"Event Cancelled: {@event.Title.Value}",
    HtmlBody = GenerateEventCancelledHtml(parameters),
    Priority = 1
};

// NEW: Templated email
var result = await _emailService.SendTemplatedEmailAsync(
    "event-cancelled-notification",
    user.Email.Value,
    parameters,
    cancellationToken);
```

### Testing Strategy
- [ ] Create migration and template
- [ ] Update handler code
- [ ] Build succeeds
- [ ] Deploy to staging
- [ ] Create test event with registrations
- [ ] Cancel event as organizer
- [ ] Verify all registrants receive email
- [ ] Verify email uses template with orange/rose gradient
- [ ] Check staging logs
- [ ] Deploy to production

### Estimated Time
**2-3 hours** (template creation + handler migration + testing)

---

## Phase 6A.60: Signup Commitment Emails

### Overview
Send confirmation emails when users commit to bringing items via the SignUp system.

### Architecture Design
**Source**: System-architect agent (Task ID: a79cc1d)
**Design Document**: Section in this plan below
**Estimated Time**: 3-4 hours

### Component Summary

#### 1. Domain Event
**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\DomainEvents\SignupCommitmentConfirmedEvent.cs`

```csharp
public record SignupCommitmentConfirmedEvent(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    Guid UserId,
    string ItemDescription,
    int Quantity,
    DateTime CommittedAt
) : DomainEvent;
```

**Purpose**: Immutable record capturing commitment confirmation
**Properties**: All data needed for email without extra queries

#### 2. Domain Modification
**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Entities\SignUpItem.cs`
**Method**: `AddCommitment()` (lines 127-167)

**Change**: Add domain event dispatch after successful commitment

```csharp
// At line 165 (before return Result.Success())
RaiseDomainEvent(new DomainEvents.SignupCommitmentConfirmedEvent(
    eventId,
    signUpListId,
    Id,
    userId,
    ItemDescription,
    commitQuantity,
    DateTime.UtcNow));
```

**Signature Change**: Add `eventId` and `signUpListId` parameters

```csharp
public Result AddCommitment(
    Guid eventId,           // NEW
    Guid signUpListId,      // NEW
    Guid userId,
    int commitQuantity,
    // ... existing parameters
)
```

#### 3. Event Handler
**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\SignupCommitmentConfirmedEventHandler.cs`

**Dependencies**:
- `IEmailService` - Send templated email
- `IUserRepository` - Fetch user details
- `IEventRepository` - Fetch event details
- `ILogger<>` - Comprehensive logging with [Phase 6A.60] prefix

**Key Features**:
- Fail-silent pattern (email failures don't block transactions)
- Defensive null checking (user, event, list, item)
- Rich email parameters (14 template variables)
- Helper methods: `GetEventLocationString()`, `FormatEventDateTimeRange()`

**Email Template**: `signup-commitment-confirmation` (already in database)

**Email Parameters**:
| Variable | Source | Example |
|----------|--------|---------|
| UserName | `User.FirstName + LastName` | "Sarah Smith" |
| EventTitle | `Event.Title.Value` | "Community Potluck" |
| ItemDescription | `DomainEvent.ItemDescription` | "Vegetarian Lasagna" |
| Quantity | `DomainEvent.Quantity` | 2 |
| EventDateTime | `FormatEventDateTimeRange()` | "Dec 24, 2025 5:00 PM to 10:00 PM" |
| EventLocation | `GetEventLocationString()` | "Main St, San Francisco" |
| SignUpCategory | `SignUpList.Category` | "Main Dishes" |
| CommittedAt | `DomainEvent.CommittedAt` | "December 20, 2025 3:45 PM" |
| ContactName | `SignUpCommitment.ContactName` | "Sarah Smith" |
| ContactEmail | `SignUpCommitment.ContactEmail` | "sarah@example.com" |
| ContactPhone | `SignUpCommitment.ContactPhone` | "+1-555-1234" |
| HasContactInfo | `!string.IsNullOrEmpty(ContactName)` | true |
| Notes | `SignUpCommitment.Notes` | "Will bring spicy version" |
| HasNotes | `!string.IsNullOrEmpty(Notes)` | true |

#### 4. Command Handler Update
**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\CommitToSignUpItem\CommitToSignUpItemCommandHandler.cs`

**Changes**: Pass `EventId` and `SignUpListId` to domain methods

```csharp
// Line 45-52 (Update existing commitment)
commitResult = signUpItem.UpdateCommitment(
    request.EventId,        // NEW
    request.SignUpListId,   // NEW
    // ... existing parameters
);

// Line 56-63 (Add new commitment)
commitResult = signUpItem.AddCommitment(
    request.EventId,        // NEW
    request.SignUpListId,   // NEW
    // ... existing parameters
);
```

### Testing Strategy

#### Unit Tests (Domain)
**File**: `c:\Work\LankaConnect\tests\LankaConnect.Domain.Tests\Events\Entities\SignUpItemTests.cs`

**Test Cases**:
1. `AddCommitment_WithValidData_RaisesSignupCommitmentConfirmedEvent`
2. `AddCommitment_WithValidData_EventContainsCorrectProperties`
3. `AddCommitment_ValidationFailure_DoesNotRaiseEvent`
4. `AddCommitment_DuplicateCommitment_DoesNotRaiseEvent`

#### Unit Tests (Handler)
**File**: `c:\Work\LankaConnect\tests\LankaConnect.Application.Tests\Events\EventHandlers\SignupCommitmentConfirmedEventHandlerTests.cs`

**Test Cases**:
1. `Handle_WithValidCommitment_SendsEmailToUser`
2. `Handle_UserNotFound_DoesNotSendEmail`
3. `Handle_EventNotFound_DoesNotSendEmail`
4. `Handle_SignUpListNotFound_DoesNotSendEmail`
5. `Handle_SignUpItemNotFound_DoesNotSendEmail`
6. `Handle_EmailServiceFailure_DoesNotThrow`
7. `Handle_ExceptionDuringProcessing_DoesNotThrow`
8. `Handle_WithValidCommitment_IncludesCorrectEmailParameters`
9. `Handle_WithContactInfo_IncludesContactInEmail`
10. `Handle_WithoutContactInfo_HandlesGracefully`

#### Integration Tests
**File**: `c:\Work\LankaConnect\tests\LankaConnect.IntegrationTests\Events\Commands\CommitToSignUpItemCommandHandlerIntegrationTests.cs`

**Test Cases**:
1. `CommitToSignUpItem_WithValidData_SendsConfirmationEmail`
2. `CommitToSignUpItem_EmailFailure_DoesNotRollbackTransaction`
3. `CommitToSignUpItem_WithContactInfo_EmailContainsContactDetails`

### Migration Requirements
**NO MIGRATION REQUIRED**
Rationale: Email template already exists, domain event is in-memory only

### Deployment Checklist

**Pre-Deployment**:
- [ ] All unit tests pass (90%+ coverage)
- [ ] All integration tests pass
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Code review completed
- [ ] Email template verified in staging DB

**Deployment**:
1. [ ] Deploy code to staging
2. [ ] Verify template: `SELECT * FROM EmailTemplates WHERE TemplateName = 'signup-commitment-confirmation'`
3. [ ] Create test event with sign-up list
4. [ ] Test commitment flow end-to-end
5. [ ] Verify email received with correct data
6. [ ] Check logs for `[Phase 6A.60]` entries
7. [ ] Deploy to production
8. [ ] Monitor logs for 24 hours

### Implementation Plan (TDD Red-Green-Refactor)

**Phase 1: Red (Write Failing Tests)** - 30 minutes
1. Create `SignupCommitmentConfirmedEvent` stub
2. Write 4 domain tests - FAILING
3. Write 10 event handler tests - FAILING
4. Write 2 integration tests - FAILING

**Phase 2: Green (Make Tests Pass)** - 2 hours
1. Implement `SignupCommitmentConfirmedEvent` domain event (10 min)
2. Modify `SignUpItem.AddCommitment()` to raise event (15 min)
3. Update `CommitToSignUpItemCommandHandler` with new parameters (15 min)
4. Implement `SignupCommitmentConfirmedEventHandler` (60 min)
5. Register handler in DI container (5 min)
6. Run all tests, fix failures (30 min)

**Phase 3: Refactor** - 30 minutes
1. Extract common email formatting logic
2. Add XML documentation comments
3. Review error messages for clarity
4. Optimize logging statements
5. Final code review

**Phase 4: Integration Testing** - 1 hour
1. Manual test in local environment
2. Verify email template rendering
3. Test edge cases (missing data, email failures)
4. Verify logging output

**Total Estimated Time**: **4 hours**

### Risk Analysis

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Email template not in database | Low | High | Pre-deployment verification query |
| Email service quota exceeded | Low | Medium | Monitor send rates |
| User receives duplicate emails | Low | Medium | Idempotency via domain event deduplication |
| Missing EventId/SignUpListId | Medium | High | Comprehensive unit tests for parameters |

---

## Phase 6A.61: Manual Event Email Sending

### Overview
Allow event organizers to send custom emails to event registrants with flexible recipient filtering.

### Architecture Design
**Source**: System-architect agent (Task ID: a952954)
**Design Document**: Comprehensive 600+ line architecture below
**Estimated Time**: 15-17 hours

### Key Features

1. **Recipient Filtering**:
   - All registrants
   - Paid registrations only
   - Free registrations only
   - Specific users (via email list)

2. **Email Preview**: Organizers can preview email before sending

3. **Batch Processing**:
   - 50 emails per chunk
   - 4 concurrent batches
   - Scalable to 1000+ recipients

4. **Idempotency**: SHA256 hash prevents duplicate sends

5. **Audit Trail**: Complete history with per-recipient delivery status

6. **Authorization**: Only event organizer can send emails

### Database Schema

#### Table 1: event_email_history

```sql
CREATE TABLE event_email_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    sent_by_user_id UUID NOT NULL REFERENCES users(id),
    template_name VARCHAR(100) NOT NULL,
    subject VARCHAR(500) NOT NULL,
    message_body TEXT NOT NULL,
    recipient_filter VARCHAR(50) NOT NULL, -- 'All', 'PaidOnly', 'FreeOnly', 'Specific'
    total_recipients INT NOT NULL,
    successful_sends INT NOT NULL DEFAULT 0,
    failed_sends INT NOT NULL DEFAULT 0,
    send_status VARCHAR(50) NOT NULL, -- 'Pending', 'InProgress', 'Completed', 'Failed'
    idempotency_key VARCHAR(64) NOT NULL UNIQUE, -- SHA256 hash
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP,
    error_message TEXT,

    INDEX idx_event_email_history_event_id (event_id),
    INDEX idx_event_email_history_sent_by (sent_by_user_id),
    INDEX idx_event_email_history_status (send_status),
    INDEX idx_event_email_history_idempotency (idempotency_key)
);
```

#### Table 2: event_email_recipients

```sql
CREATE TABLE event_email_recipients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email_history_id UUID NOT NULL REFERENCES event_email_history(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    recipient_email VARCHAR(255) NOT NULL,
    recipient_name VARCHAR(255),
    delivery_status VARCHAR(50) NOT NULL, -- 'Pending', 'Sent', 'Failed'
    sent_at TIMESTAMP,
    error_message TEXT,

    INDEX idx_event_email_recipients_history (email_history_id),
    INDEX idx_event_email_recipients_status (delivery_status)
);
```

### Domain Model

#### Entities

**EventEmailHistory.cs**:
```csharp
public class EventEmailHistory : Entity<Guid>
{
    public Guid EventId { get; private set; }
    public Guid SentByUserId { get; private set; }
    public string TemplateName { get; private set; }
    public string Subject { get; private set; }
    public string MessageBody { get; private set; }
    public RecipientFilterType RecipientFilter { get; private set; }
    public int TotalRecipients { get; private set; }
    public int SuccessfulSends { get; private set; }
    public int FailedSends { get; private set; }
    public EmailSendStatus SendStatus { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private readonly List<EventEmailRecipient> _recipients = new();
    public IReadOnlyCollection<EventEmailRecipient> Recipients => _recipients.AsReadOnly();

    // Factory method
    public static Result<EventEmailHistory> Create(/*params*/) { }

    // State transitions
    public Result MarkAsInProgress() { }
    public Result MarkAsCompleted() { }
    public Result MarkAsFailed(string error) { }
    public Result UpdateProgress(int successful, int failed) { }
}
```

**EventEmailRecipient.cs**:
```csharp
public class EventEmailRecipient : Entity<Guid>
{
    public Guid EmailHistoryId { get; private set; }
    public Guid? UserId { get; private set; }
    public string RecipientEmail { get; private set; }
    public string RecipientName { get; private set; }
    public EmailDeliveryStatus DeliveryStatus { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static Result<EventEmailRecipient> Create(/*params*/) { }
    public Result MarkAsSent() { }
    public Result MarkAsFailed(string error) { }
}
```

#### Enums

```csharp
public enum RecipientFilterType
{
    All = 1,
    PaidOnly = 2,
    FreeOnly = 3,
    Specific = 4
}

public enum EmailSendStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

public enum EmailDeliveryStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}
```

### Application Layer

#### Commands

**SendEventEmailCommand.cs**:
```csharp
public record SendEventEmailCommand(
    Guid EventId,
    Guid UserId,
    string Subject,
    string MessageBody,
    RecipientFilterType RecipientFilter,
    List<string>? SpecificRecipients = null
) : ICommand<SendEventEmailResponse>;

public record SendEventEmailResponse(
    Guid EmailHistoryId,
    int TotalRecipients,
    EmailSendStatus Status
);
```

**Command Validator**:
```csharp
public class SendEventEmailCommandValidator : AbstractValidator<SendEventEmailCommand>
{
    public SendEventEmailCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(500)
            .Must(NotContainMaliciousContent);
        RuleFor(x => x.MessageBody)
            .NotEmpty()
            .MaximumLength(50000)
            .Must(NotContainMaliciousContent);
        RuleFor(x => x.RecipientFilter).IsInEnum();
        RuleFor(x => x.SpecificRecipients)
            .NotNull()
            .When(x => x.RecipientFilter == RecipientFilterType.Specific)
            .Must(list => list!.Count > 0 && list.Count <= 500);
    }
}
```

**Command Handler**:
```csharp
public class SendEventEmailCommandHandler
    : ICommandHandler<SendEventEmailCommand, SendEventEmailResponse>
{
    public async Task<Result<SendEventEmailResponse>> Handle(
        SendEventEmailCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Authorization: Verify user is event organizer
        // 2. Fetch event and registrations
        // 3. Filter recipients based on RecipientFilterType
        // 4. Generate idempotency key (SHA256 hash)
        // 5. Check for duplicate send (idempotency)
        // 6. Create EventEmailHistory entity
        // 7. Create EventEmailRecipient entities
        // 8. Save to database
        // 9. Trigger batch email sending (background job)
        // 10. Return response
    }
}
```

**PreviewEventEmailCommand.cs**:
```csharp
public record PreviewEventEmailCommand(
    Guid EventId,
    Guid UserId,
    string Subject,
    string MessageBody,
    RecipientFilterType RecipientFilter,
    List<string>? SpecificRecipients = null
) : ICommand<PreviewEventEmailResponse>;

public record PreviewEventEmailResponse(
    string RenderedHtml,
    int RecipientCount,
    List<string> SampleRecipients
);
```

#### Queries

**GetEventEmailHistoryQuery.cs**:
```csharp
public record GetEventEmailHistoryQuery(
    Guid EventId,
    Guid UserId
) : IQuery<List<EventEmailHistoryDto>>;

public record EventEmailHistoryDto(
    Guid Id,
    string Subject,
    RecipientFilterType RecipientFilter,
    int TotalRecipients,
    int SuccessfulSends,
    int FailedSends,
    EmailSendStatus SendStatus,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
```

#### Services

**EventEmailBatchProcessor.cs**:
```csharp
public interface IEventEmailBatchProcessor
{
    Task ProcessEmailBatchAsync(Guid emailHistoryId, CancellationToken cancellationToken);
}

public class EventEmailBatchProcessor : IEventEmailBatchProcessor
{
    private const int CHUNK_SIZE = 50;
    private const int MAX_PARALLEL_BATCHES = 4;

    public async Task ProcessEmailBatchAsync(
        Guid emailHistoryId,
        CancellationToken cancellationToken)
    {
        // 1. Fetch email history and recipients
        // 2. Mark history as InProgress
        // 3. Split recipients into chunks of 50
        // 4. Process chunks in parallel (4 at a time)
        // 5. For each recipient:
        //    - Send templated email
        //    - Update recipient delivery status
        //    - Update history progress counters
        // 6. Mark history as Completed or Failed
        // 7. Log comprehensive metrics
    }
}
```

### API Endpoints

#### POST /api/events/{eventId}/emails/send

**Request**:
```json
{
  "subject": "Important Update: Event Venue Changed",
  "messageBody": "Dear attendees, we wanted to inform you...",
  "recipientFilter": "All",
  "specificRecipients": null
}
```

**Response** (200 OK):
```json
{
  "emailHistoryId": "abc123...",
  "totalRecipients": 247,
  "status": "Pending"
}
```

**Response** (400 Bad Request):
```json
{
  "errors": {
    "Subject": ["Subject is required"],
    "MessageBody": ["Message body cannot be empty"]
  }
}
```

**Response** (403 Forbidden):
```json
{
  "error": "Only the event organizer can send emails to attendees"
}
```

**Response** (409 Conflict):
```json
{
  "error": "This email has already been sent. Please modify the subject or message to send a new email."
}
```

#### POST /api/events/{eventId}/emails/preview

**Request**:
```json
{
  "subject": "Event Reminder",
  "messageBody": "Don't forget about our event tomorrow!",
  "recipientFilter": "PaidOnly"
}
```

**Response** (200 OK):
```json
{
  "renderedHtml": "<html>...</html>",
  "recipientCount": 142,
  "sampleRecipients": [
    "john.doe@example.com",
    "jane.smith@example.com",
    "bob.wilson@example.com"
  ]
}
```

#### GET /api/events/{eventId}/emails/history

**Response** (200 OK):
```json
{
  "emails": [
    {
      "id": "abc123...",
      "subject": "Venue Change Notification",
      "recipientFilter": "All",
      "totalRecipients": 247,
      "successfulSends": 247,
      "failedSends": 0,
      "sendStatus": "Completed",
      "createdAt": "2025-12-20T10:30:00Z",
      "completedAt": "2025-12-20T10:35:22Z"
    }
  ]
}
```

### Batch Processing Strategy

#### Scalability Analysis

| Recipients | Chunks | Batches | Time (est) | Notes |
|------------|--------|---------|------------|-------|
| 50 | 1 | 1 | 5-10 sec | Single chunk |
| 200 | 4 | 1 | 10-15 sec | 4 parallel chunks |
| 500 | 10 | 3 | 25-40 sec | 3 rounds of 4 chunks |
| 1000 | 20 | 5 | 50-80 sec | 5 rounds of 4 chunks |

**Chunk Size**: 50 emails (Azure SendGrid limit)
**Parallel Batches**: 4 concurrent chunks (balance throughput vs. API throttling)
**Error Handling**: Continue on error (partial success supported)

### Authorization & Security

#### Authorization Rules

1. **User must be authenticated** - Check JWT token validity
2. **User must be event organizer** - Verify `event.OrganizerId == userId`
3. **Event must be in valid state** - Published or ongoing (not draft/cancelled)

#### Security Measures

| Threat | Mitigation |
|--------|------------|
| SQL Injection | Parameterized queries, EF Core ORM |
| XSS | HTML sanitization in MessageBody |
| CSRF | Anti-forgery tokens on POST endpoints |
| Email Bombing | Rate limiting (10 emails per event per hour) |
| Malicious Content | FluentValidation with content checking |
| Unauthorized Access | Ownership verification in handler |

### Testing Strategy

#### Unit Tests (Domain)

**File**: `EventEmailHistoryTests.cs`

1. `Create_WithValidData_ReturnsSuccess`
2. `MarkAsInProgress_FromPending_TransitionsCorrectly`
3. `MarkAsCompleted_WithoutInProgress_ReturnsFailure`
4. `UpdateProgress_IncrementCounters_UpdatesCorrectly`

**File**: `EventEmailRecipientTests.cs`

1. `Create_WithValidEmail_ReturnsSuccess`
2. `MarkAsSent_UpdatesStatusAndTimestamp`
3. `MarkAsFailed_StoresErrorMessage`

#### Unit Tests (Application)

**File**: `SendEventEmailCommandHandlerTests.cs`

1. `Handle_ValidCommand_CreatesEmailHistory`
2. `Handle_UnauthorizedUser_ReturnsFailure`
3. `Handle_EventNotFound_ReturnsFailure`
4. `Handle_DuplicateIdempotencyKey_ReturnsConflict`
5. `Handle_InvalidRecipientFilter_ReturnsValidationError`

**File**: `EventEmailBatchProcessorTests.cs`

1. `ProcessEmailBatchAsync_50Recipients_SendsAllEmails`
2. `ProcessEmailBatchAsync_500Recipients_ProcessesInChunks`
3. `ProcessEmailBatchAsync_EmailFailure_ContinuesProcessing`
4. `ProcessEmailBatchAsync_AllFailures_MarksHistoryAsFailed`

#### Integration Tests

**File**: `SendEventEmailCommandIntegrationTests.cs`

1. `SendEventEmail_AsOrganizer_CreatesHistoryAndSendsEmails`
2. `SendEventEmail_AsNonOrganizer_ReturnsForbidden`
3. `SendEventEmail_DuplicateSend_ReturnsConflict`
4. `SendEventEmail_With500Recipients_CompletesSuccessfully`

### Migration Plan

**Migration**: `20260101000000_Phase6A61_EventEmailHistory.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "event_email_history",
        columns: table => new
        {
            id = table.Column<Guid>(nullable: false),
            event_id = table.Column<Guid>(nullable: false),
            sent_by_user_id = table.Column<Guid>(nullable: false),
            template_name = table.Column<string>(maxLength: 100, nullable: false),
            subject = table.Column<string>(maxLength: 500, nullable: false),
            message_body = table.Column<string>(nullable: false),
            recipient_filter = table.Column<string>(maxLength: 50, nullable: false),
            total_recipients = table.Column<int>(nullable: false),
            successful_sends = table.Column<int>(nullable: false, defaultValue: 0),
            failed_sends = table.Column<int>(nullable: false, defaultValue: 0),
            send_status = table.Column<string>(maxLength: 50, nullable: false),
            idempotency_key = table.Column<string>(maxLength: 64, nullable: false),
            created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
            completed_at = table.Column<DateTime>(nullable: true),
            error_message = table.Column<string>(nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_event_email_history", x => x.id);
            table.ForeignKey(
                name: "fk_event_email_history_events_event_id",
                column: x => x.event_id,
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "fk_event_email_history_users_sent_by_user_id",
                column: x => x.sent_by_user_id,
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        });

    migrationBuilder.CreateTable(
        name: "event_email_recipients",
        columns: table => new
        {
            id = table.Column<Guid>(nullable: false),
            email_history_id = table.Column<Guid>(nullable: false),
            user_id = table.Column<Guid>(nullable: true),
            recipient_email = table.Column<string>(maxLength: 255, nullable: false),
            recipient_name = table.Column<string>(maxLength: 255, nullable: true),
            delivery_status = table.Column<string>(maxLength: 50, nullable: false),
            sent_at = table.Column<DateTime>(nullable: true),
            error_message = table.Column<string>(nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_event_email_recipients", x => x.id);
            table.ForeignKey(
                name: "fk_event_email_recipients_event_email_history_email_history_id",
                column: x => x.email_history_id,
                principalTable: "event_email_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        });

    // Create indexes
    migrationBuilder.CreateIndex(
        name: "ix_event_email_history_event_id",
        table: "event_email_history",
        column: "event_id");

    migrationBuilder.CreateIndex(
        name: "ix_event_email_history_idempotency_key",
        table: "event_email_history",
        column: "idempotency_key",
        unique: true);

    migrationBuilder.CreateIndex(
        name: "ix_event_email_recipients_email_history_id",
        table: "event_email_recipients",
        column: "email_history_id");
}
```

**Verification Queries**:
```sql
-- Verify tables created
SELECT table_name
FROM information_schema.tables
WHERE table_name IN ('event_email_history', 'event_email_recipients');

-- Verify indexes
SELECT indexname
FROM pg_indexes
WHERE tablename IN ('event_email_history', 'event_email_recipients');
```

### Deployment Checklist

**Pre-Deployment**:
- [ ] All unit tests pass (90%+ coverage)
- [ ] All integration tests pass
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Code review completed
- [ ] Migration script reviewed
- [ ] Security review completed

**Deployment Steps**:
1. [ ] Deploy migration to staging database
2. [ ] Verify tables and indexes created
3. [ ] Deploy application code to staging
4. [ ] Create test event with 50+ registrants
5. [ ] Test email preview endpoint
6. [ ] Test email send (All filter)
7. [ ] Test email send (PaidOnly filter)
8. [ ] Verify idempotency (duplicate send blocked)
9. [ ] Check logs for `[Phase 6A.61]` entries
10. [ ] Verify email history stored correctly
11. [ ] Test with 500+ recipients
12. [ ] Deploy to production
13. [ ] Monitor logs for 24-48 hours

**Post-Deployment Verification**:
```sql
-- Check email history
SELECT COUNT(*) FROM event_email_history;

-- Check recipients
SELECT COUNT(*) FROM event_email_recipients;

-- Check recent sends
SELECT e.id, e.subject, e.total_recipients, e.successful_sends, e.send_status
FROM event_email_history e
ORDER BY e.created_at DESC
LIMIT 10;
```

### Implementation Plan (TDD)

**Phase 1: Domain Layer** (4 hours)
1. Create enums (30 min)
2. Create `EventEmailHistory` entity with tests (90 min)
3. Create `EventEmailRecipient` entity with tests (60 min)
4. EF Core configuration (30 min)

**Phase 2: Database Migration** (1 hour)
1. Create migration script (30 min)
2. Test migration up/down (30 min)

**Phase 3: Application Layer** (6 hours)
1. Create commands/queries with DTOs (60 min)
2. Create validators with tests (60 min)
3. Implement `PreviewEventEmailCommand` handler with tests (90 min)
4. Implement `SendEventEmailCommand` handler with tests (120 min)
5. Implement `GetEventEmailHistoryQuery` handler with tests (60 min)

**Phase 4: Batch Processor** (3 hours)
1. Create `IEventEmailBatchProcessor` interface (15 min)
2. Implement batch processor with tests (120 min)
3. Background job integration (45 min)

**Phase 5: API Endpoints** (2 hours)
1. Create controller with endpoints (60 min)
2. Add authorization attributes (30 min)
3. Integration tests (30 min)

**Phase 6: Integration Testing** (2 hours)
1. End-to-end test with 50 recipients (30 min)
2. End-to-end test with 500 recipients (30 min)
3. Test failure scenarios (30 min)
4. Test authorization (30 min)

**Total Estimated Time**: **17 hours**

### Architecture Decision Records

**ADR #1: Email History Storage (Global vs. Per-Event)**
**Decision**: Global `event_email_history` table with `event_id` FK
**Rationale**: Enables cross-event analytics, simpler queries, normalized schema

**ADR #2: Batch Processing (Sequential vs. Parallel)**
**Decision**: Parallel processing with 4 concurrent batches
**Rationale**: Balance throughput (4x faster) vs. API throttling risk

**ADR #3: Idempotency Strategy**
**Decision**: SHA256 hash of `(eventId + subject + messageBody + recipientFilter)`
**Rationale**: Prevents accidental duplicate sends, unique constraint ensures enforcement

**ADR #4: Authorization Model**
**Decision**: Ownership-based (event organizer only)
**Rationale**: Simplest model, aligns with business rules, no role complexity

**ADR #5: Recipient Filtering**
**Decision**: Enum-based with 4 types (All, PaidOnly, FreeOnly, Specific)
**Rationale**: Covers 95% of use cases, simple to implement, extensible

---

## Implementation Order

### Recommended Sequence

1. **Phase 6A.62: Fix Registration Cancellation Email** (15 minutes) ⚡ **URGENT**
   - One-line fix (already applied)
   - Deploy to staging immediately
   - Test with real cancellation
   - Deploy to production

2. **Phase 6A.63: Event Cancellation Email Template** (2-3 hours)
   - Create database template with orange/rose gradient
   - Migrate handler from inline HTML to template
   - Test with event cancellation flow
   - Completes cancellation email consistency

3. **Phase 6A.60: Signup Commitment Emails** (3-4 hours)
   - Simpler feature
   - No database changes
   - Builds confidence with TDD workflow
   - Validates email template system

4. **Phase 6A.61: Manual Event Email Sending** (15-17 hours)
   - Most complex feature
   - Database migrations required
   - Batch processing implementation
   - Can leverage learnings from 6A.60

### Total Implementation Time

- Phase 6A.62: 15 minutes (urgent bug fix)
- Phase 6A.63: 2-3 hours (template migration)
- Phase 6A.60: 3-4 hours (new feature)
- Phase 6A.61: 15-17 hours (complex feature)
- **Total**: **21-24 hours**

### Dependencies

**Phase 6A.60 Dependencies**:
- ✅ SignUpItem entity exists
- ✅ Email template `signup-commitment-confirmation` created
- ✅ IEmailService interface implemented
- ✅ Domain event infrastructure ready

**Phase 6A.61 Dependencies**:
- ✅ Event entity with registrations
- ✅ Email template `organizer-custom-message` created
- ✅ IEmailService interface implemented
- ⚠️ Background job infrastructure (Hangfire or similar) - may need setup

---

## Master TODO List

### Phase 6A.60: Signup Commitment Emails

#### Domain Layer
- [ ] Create `SignupCommitmentConfirmedEvent.cs` domain event
- [ ] Write 4 domain unit tests (RED)
- [ ] Modify `SignUpItem.AddCommitment()` to raise event
- [ ] Update `SignUpItem.AddCommitment()` signature (add eventId, signUpListId)
- [ ] Run domain tests (GREEN)
- [ ] Add XML documentation to domain event

#### Application Layer
- [ ] Create `SignupCommitmentConfirmedEventHandler.cs`
- [ ] Write 10 event handler unit tests (RED)
- [ ] Implement event handler logic
- [ ] Implement `GetEventLocationString()` helper
- [ ] Implement `FormatEventDateTimeRange()` helper
- [ ] Run handler tests (GREEN)
- [ ] Update `CommitToSignUpItemCommandHandler` to pass new parameters
- [ ] Refactor: Extract email formatting logic if reusable

#### Testing
- [ ] Create 2 integration tests (RED)
- [ ] Run integration tests (GREEN)
- [ ] Manual test in local environment
- [ ] Verify email template in local database
- [ ] Test edge cases (missing user, event, list, item)
- [ ] Verify logging output format

#### Deployment
- [ ] Code review
- [ ] Verify build (0 errors, 0 warnings)
- [ ] Deploy to staging
- [ ] Verify email template in staging DB
- [ ] End-to-end test in staging
- [ ] Verify email content and formatting
- [ ] Check staging logs for `[Phase 6A.60]` entries
- [ ] Deploy to production
- [ ] Monitor production logs for 24 hours

#### Documentation
- [ ] Create `PHASE_6A60_SIGNUP_COMMITMENT_EMAIL_SUMMARY.md`
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Update TASK_SYNCHRONIZATION_STRATEGY.md

### Phase 6A.61: Manual Event Email Sending

#### Domain Layer
- [ ] Create `RecipientFilterType` enum
- [ ] Create `EmailSendStatus` enum
- [ ] Create `EmailDeliveryStatus` enum
- [ ] Create `EventEmailHistory` entity with 8 unit tests (RED)
- [ ] Create `EventEmailRecipient` entity with 3 unit tests (RED)
- [ ] Implement entity logic (GREEN)
- [ ] Create EF Core entity configurations
- [ ] Add XML documentation to all entities

#### Database Migration
- [ ] Create migration `20260101000000_Phase6A61_EventEmailHistory.cs`
- [ ] Test migration up/down locally
- [ ] Verify indexes created
- [ ] Write verification SQL queries

#### Application Layer - Commands
- [ ] Create `SendEventEmailCommand` and response DTO
- [ ] Create `SendEventEmailCommandValidator` with 6 validation rules
- [ ] Write 5 validator unit tests (RED)
- [ ] Create `SendEventEmailCommandHandler`
- [ ] Write 5 handler unit tests (RED)
- [ ] Implement handler logic (authorization, idempotency, persistence)
- [ ] Run handler tests (GREEN)

#### Application Layer - Queries
- [ ] Create `PreviewEventEmailCommand` and response DTO
- [ ] Create `PreviewEventEmailCommandHandler`
- [ ] Write 3 preview handler tests (RED)
- [ ] Implement preview logic (GREEN)
- [ ] Create `GetEventEmailHistoryQuery` and DTO
- [ ] Create `GetEventEmailHistoryQueryHandler`
- [ ] Write 2 query handler tests (RED)
- [ ] Implement query logic (GREEN)

#### Application Layer - Services
- [ ] Create `IEventEmailBatchProcessor` interface
- [ ] Create `EventEmailBatchProcessor` implementation
- [ ] Write 4 batch processor unit tests (RED)
- [ ] Implement batch processing with chunking
- [ ] Implement parallel processing (4 concurrent batches)
- [ ] Implement error handling (partial success)
- [ ] Run batch processor tests (GREEN)
- [ ] Add comprehensive logging with `[Phase 6A.61]` prefix

#### Presentation Layer
- [ ] Create `EventEmailsController`
- [ ] Add POST `/api/events/{eventId}/emails/send` endpoint
- [ ] Add POST `/api/events/{eventId}/emails/preview` endpoint
- [ ] Add GET `/api/events/{eventId}/emails/history` endpoint
- [ ] Add authorization attributes
- [ ] Write 4 controller integration tests (RED)
- [ ] Run integration tests (GREEN)

#### Testing
- [ ] End-to-end test with 50 recipients
- [ ] End-to-end test with 500 recipients
- [ ] Test authorization (non-organizer blocked)
- [ ] Test idempotency (duplicate send blocked)
- [ ] Test recipient filtering (All, PaidOnly, FreeOnly, Specific)
- [ ] Test email preview rendering
- [ ] Test batch failure scenarios
- [ ] Verify logging comprehensiveness

#### Deployment
- [ ] Code review
- [ ] Security review (XSS, SQL injection, authorization)
- [ ] Verify build (0 errors, 0 warnings)
- [ ] Deploy migration to staging database
- [ ] Verify tables and indexes created in staging
- [ ] Deploy application code to staging
- [ ] Create test event with 50+ registrants
- [ ] Test preview endpoint
- [ ] Test send endpoint (All filter)
- [ ] Test send endpoint (PaidOnly filter)
- [ ] Test idempotency (duplicate blocked)
- [ ] Verify email history stored correctly
- [ ] Test with 500+ recipients
- [ ] Check staging logs for `[Phase 6A.61]` entries
- [ ] Deploy to production
- [ ] Monitor production logs for 48 hours
- [ ] Monitor email delivery rates

#### Documentation
- [ ] Create `PHASE_6A61_MANUAL_EVENT_EMAIL_SUMMARY.md`
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Update TASK_SYNCHRONIZATION_STRATEGY.md
- [ ] Document ADRs in summary document

---

## Testing Strategy

### Test Coverage Requirements

**Target**: 90% code coverage minimum

**Coverage Breakdown**:
- Domain entities: 95%+ (critical business logic)
- Application handlers: 90%+
- Controllers: 80%+
- Services: 85%+

### Test Types

#### 1. Unit Tests
**Purpose**: Verify individual components in isolation

**Tools**: xUnit, FluentAssertions, Moq

**Patterns**:
- AAA (Arrange-Act-Assert)
- One assertion per test (where possible)
- Descriptive test names: `Method_Scenario_ExpectedBehavior`

**Example**:
```csharp
[Fact]
public void AddCommitment_WithValidData_RaisesSignupCommitmentConfirmedEvent()
{
    // Arrange
    var item = CreateTestSignUpItem();

    // Act
    var result = item.AddCommitment(eventId, listId, userId, 2);

    // Assert
    result.IsSuccess.Should().BeTrue();
    item.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<SignupCommitmentConfirmedEvent>();
}
```

#### 2. Integration Tests
**Purpose**: Verify components work together correctly

**Tools**: WebApplicationFactory, Testcontainers (PostgreSQL)

**Patterns**:
- Use real database (Testcontainers)
- Reset database between tests
- Test full request/response cycle

**Example**:
```csharp
[Fact]
public async Task CommitToSignUpItem_WithValidData_SendsConfirmationEmail()
{
    // Arrange
    var client = _factory.CreateClient();
    var command = CreateTestCommitCommand();

    // Act
    var response = await client.PostAsJsonAsync("/api/events/commit", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    _emailServiceMock.Verify(x => x.SendTemplatedEmailAsync(
        "signup-commitment-confirmation",
        It.IsAny<string>(),
        It.IsAny<Dictionary<string, object>>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

#### 3. Manual Testing
**Purpose**: Verify user experience and edge cases

**Checklist**:
- [ ] Email received within 30 seconds
- [ ] Email content matches template
- [ ] All template variables populated correctly
- [ ] Email displays correctly in Gmail, Outlook, Apple Mail
- [ ] No decorative elements (stars, emojis) in email
- [ ] Logging comprehensive and formatted correctly

### TDD Workflow

**Red-Green-Refactor Cycle**:

1. **RED**: Write failing test
   - Think about requirements
   - Design interface/API
   - Write test that fails

2. **GREEN**: Make test pass
   - Write minimal code
   - Get test passing
   - Don't optimize yet

3. **REFACTOR**: Improve code
   - Extract duplicates
   - Improve naming
   - Add documentation
   - All tests still pass

**Example Workflow** (Phase 6A.60):

```
1. RED: Write test `AddCommitment_WithValidData_RaisesEvent` (fails - event doesn't exist)
2. GREEN: Create SignupCommitmentConfirmedEvent stub (test passes)
3. REFACTOR: Add XML docs to event (test still passes)

4. RED: Write test `Handle_WithValidCommitment_SendsEmail` (fails - handler doesn't exist)
5. GREEN: Create SignupCommitmentConfirmedEventHandler skeleton (test passes)
6. REFACTOR: Extract GetEventLocationString() helper (tests still pass)

... continue cycle ...
```

---

## Deployment Verification

### Pre-Deployment Checklist

**Code Quality**:
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Test coverage ≥ 90%
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Code review approved
- [ ] Security review approved (Phase 6A.61 only)

**Documentation**:
- [ ] XML documentation on public APIs
- [ ] README updated (if needed)
- [ ] Phase summary document created
- [ ] All tracking documents updated

**Database**:
- [ ] Migration script reviewed
- [ ] Migration tested locally (up/down)
- [ ] Rollback plan documented

### Deployment Process

#### Staging Deployment

1. **Database Migration** (Phase 6A.61 only):
   ```bash
   # Connect to staging database
   dotnet ef database update --context AppDbContext --connection "<staging-connection>"

   # Verify tables created
   SELECT table_name FROM information_schema.tables
   WHERE table_name IN ('event_email_history', 'event_email_recipients');
   ```

2. **Application Deployment**:
   ```bash
   # Build and deploy to Azure Container Apps (staging)
   az containerapp update --name lankaconnect-api-staging --resource-group LankaConnect-RG --image <image-tag>
   ```

3. **Verification Queries**:
   ```sql
   -- Phase 6A.60: Verify email template exists
   SELECT * FROM EmailTemplates
   WHERE TemplateName = 'signup-commitment-confirmation';

   -- Phase 6A.61: Verify tables and indexes
   SELECT indexname FROM pg_indexes
   WHERE tablename IN ('event_email_history', 'event_email_recipients');
   ```

4. **Functional Testing**:
   - [ ] Create test event
   - [ ] Add sign-up list with items
   - [ ] Test commitment flow (6A.60)
   - [ ] Verify email received
   - [ ] Check email content and formatting
   - [ ] Test email send flow (6A.61)
   - [ ] Verify batch processing
   - [ ] Test idempotency

5. **Log Verification**:
   ```bash
   # Check staging logs for phase-specific entries
   az containerapp logs show --name lankaconnect-api-staging --resource-group LankaConnect-RG --follow

   # Look for:
   # [Phase 6A.60] entries
   # [Phase 6A.61] entries
   # Error messages
   # Performance metrics
   ```

#### Production Deployment

1. **Final Checks**:
   - [ ] Staging verification complete
   - [ ] All tests passed in staging
   - [ ] No critical logs in staging
   - [ ] User acceptance testing complete (if applicable)

2. **Deploy Migration** (Phase 6A.61 only):
   ```bash
   dotnet ef database update --context AppDbContext --connection "<production-connection>"
   ```

3. **Deploy Application**:
   ```bash
   az containerapp update --name lankaconnect-api-prod --resource-group LankaConnect-RG --image <image-tag>
   ```

4. **Post-Deployment Verification**:
   - [ ] Health check endpoint responds
   - [ ] Application logs show startup
   - [ ] Database migration applied
   - [ ] Email templates verified
   - [ ] Smoke test (create event, commit to item, send email)

5. **Monitoring**:
   - [ ] Monitor logs for 24-48 hours
   - [ ] Monitor email delivery rates
   - [ ] Check error rates
   - [ ] Verify performance metrics

### Rollback Plan

**If critical bugs discovered**:

1. **Phase 6A.60** (No migration):
   - Disable event handler via feature flag
   - Redeploy previous version
   - No database rollback needed

2. **Phase 6A.61** (With migration):
   - Disable endpoints via feature flag
   - Redeploy previous version
   - Rollback migration if needed:
     ```bash
     dotnet ef database update <previous-migration> --context AppDbContext
     ```

**Rollback Criteria**:
- Email delivery failure rate > 10%
- Application errors > 5% of requests
- Database performance degradation
- Security vulnerability discovered

---

## Phase Number Management

### Phase Number Verification

**Phase 6A.60: Signup Commitment Emails**
✅ **VERIFIED AVAILABLE** as of 2025-12-31

**Phase 6A.61: Manual Event Email Sending**
✅ **VERIFIED AVAILABLE** as of 2025-12-31

### How Phase Numbers Were Verified

1. ✅ Checked [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)
   - Last used: 6A.59 (Landing Page Unified Search)
   - 6A.60 and 6A.61: Not in index

2. ✅ Searched PROGRESS_TRACKER.md
   - No references to 6A.60 or 6A.61

3. ✅ Searched STREAMLINED_ACTION_PLAN.md
   - No references to 6A.60 or 6A.61

4. ✅ Searched TASK_SYNCHRONIZATION_STRATEGY.md
   - No references to 6A.60 or 6A.61

### Master Index Update Required

After each phase completion, update PHASE_6A_MASTER_INDEX.md:

```markdown
| 6A.60 | Signup Commitment Emails | ✅ Complete | [PHASE_6A60_SIGNUP_COMMITMENT_EMAIL_SUMMARY.md](./PHASE_6A60_SIGNUP_COMMITMENT_EMAIL_SUMMARY.md) | 2026-01-XX |
| 6A.61 | Manual Event Email Sending | ✅ Complete | [PHASE_6A61_MANUAL_EVENT_EMAIL_SUMMARY.md](./PHASE_6A61_MANUAL_EVENT_EMAIL_SUMMARY.md) | 2026-01-XX |
```

### Related Phase Numbers (Historical Context)

**Original Email System Plan** (Before Renumbering):
- 6A.58: Event Publication Email
- 6A.59: Manual Email Sending

**Conflict Discovered**:
- 6A.58 was already used by "Dashboard Event Filtration"

**Resolution**:
- Renumbered to 6A.60 (Signup Commitment Emails)
- Renumbered to 6A.61 (Manual Event Email Sending)

**Deferred Features** (Original 6A.8/6A.9):
- 6A.10: Subscription Expiry Notifications (Deferred)
- 6A.11: Subscription Management UI (Deferred)

**Previously Blocked** (Now Complete or Reassigned):
- 6A.50: Manual Organizer Email Sending → Became 6A.61
- 6A.51: Signup Commitment Emails → Became 6A.60
- 6A.52: Registration Cancellation Emails → ✅ Complete

---

## Document Change History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.0 | Initial document created with comprehensive plans for Phase 6A.60 and 6A.61 |

---

## References

**Primary Tracking Documents**:
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) - Phase number registry
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Session history and status
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) - Action items
- [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md) - Documentation protocol

**Requirement Documents**:
- [Master Requirements Specification.md](./Master Requirements Specification.md) - User-facing features
- [PROJECT_CONTENT.md](./PROJECT_CONTENT.md) - Project overview

**Development Guidelines**:
- [CLAUDE.md](../CLAUDE.md) - Requirement documentation prevention system

**System-Architect Design Documents**:
- Phase 6A.60 Design: Agent Task ID a79cc1d
- Phase 6A.61 Design: Agent Task ID a952954

---

**END OF DOCUMENT**
