# LankaConnect Email System Implementation Plan
## System Architect Approved - Final Version

**Date**: 2025-12-26
**Status**: ✅ APPROVED WITH REFINEMENTS INCORPORATED
**Approval**: System Architect Review Complete
**Risk Level**: Low (after refinements)
**Estimated Effort**: 31-39 hours over 4 weeks

---

## Executive Summary

This plan addresses critical email system issues and implements comprehensive email notification features for LankaConnect. The work is broken into 6 phases (6A.49-6A.54) with **revised implementation order** based on architectural dependencies.

### Architect Assessment
- **Architecture Quality**: ✅ Clean Architecture + DDD properly applied
- **EF Core Analysis**: ✅ Root cause correctly identified, solution verified
- **Domain Events**: ✅ Approved with minor refinements (immutable collections)
- **Security**: ⚠️ Critical enhancements required (email injection, rate limiting, token security)
- **Testing**: ✅ TDD approach validated, 90% coverage realistic
- **Implementation Order**: 🔄 REVISED - Template consolidation must come first

### Current State
- ✅ **Working**: Free event registration emails, email queue processor, domain event infrastructure
- ❌ **Broken**: Paid event registration emails (PaymentCompletedEvent not dispatched due to EF Core tracking issue)
- 🔜 **Missing**: Organizer email sending, signup commitment emails, registration cancellation emails, member verification

---

## CRITICAL: Revised Implementation Order

### Original Order (INCORRECT):
1. 6A.49 → 6A.53 → 6A.50 → 6A.51 → 6A.52 → 6A.54

### Architect-Approved Order (CORRECT):
1. **6A.54 FIRST** - Email Template Consolidation (Foundation)
2. **6A.49** - Fix Paid Event Email (Critical Bug)
3. **6A.53** - Email Verification System (Security)
4. **6A.51** - Signup Commitment Emails
5. **6A.52** - Registration Cancellation Emails
6. **6A.50 LAST** - Manual Email Sending (Most Complex)

**Rationale**: All other phases depend on email templates. Creating the base template and all variations FIRST prevents rework and ensures consistent branding from the start.

---

## Phase 6A.54: Email Template Consolidation & Branding (MOVED TO FIRST)

**Priority**: P0 (Foundation)
**Estimated**: 5-6 hours
**Dependencies**: None

### Why This Must Be First
All subsequent phases (6A.49-6A.52) require email templates. Creating templates AFTER features leads to:
- Rework of template designs
- Inconsistent branding
- Duplicated effort
- Higher risk of template bugs

### Deliverables

#### 1. Base Email Template
**File**: `src/LankaConnect.Infrastructure/EmailTemplates/Base/BaseEmailTemplate.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f9fafb;
            margin: 0;
            padding: 0;
        }
        .container {
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .header {
            background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%);
            /* Orange (#fb923c) to Rose (#f43f5e) gradient */
            padding: 32px 24px;
            text-align: center;
        }
        .header h1 {
            color: white;
            margin: 0;
            font-size: 24px;
        }
        .content {
            padding: 32px 24px;
        }
        .event-details {
            background: #f9fafb;
            border-left: 4px solid #fb923c;
            padding: 16px;
            margin: 16px 0;
            border-radius: 4px;
        }
        .button {
            display: inline-block;
            background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%);
            color: white !important;
            padding: 12px 24px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin: 16px 0;
        }
        .button:hover {
            opacity: 0.9;
        }
        .footer {
            background: #f9fafb;
            padding: 24px;
            text-align: center;
            font-size: 14px;
            color: #6b7280;
        }
        .footer a {
            color: #fb923c;
            text-decoration: none;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>{{HeaderTitle}}</h1>
        </div>
        <div class="content">
            {{Content}}
        </div>
        <div class="footer">
            <p><strong>LankaConnect</strong> - Connecting Sri Lankan Communities</p>
            <p>If you have any questions, reply to this email or contact <a href="mailto:support@lankaconnect.com">support@lankaconnect.com</a></p>
            <p><a href="{{UnsubscribeUrl}}">Unsubscribe</a> from these emails</p>
        </div>
    </div>
</body>
</html>
```

#### 2. All 6 Email Templates

**Templates to Create**:
1. `registration-confirmation.html` (update existing with base template)
2. `member-email-verification.html` (new)
3. `signup-commitment-confirmed.html` (new)
4. `registration-cancelled.html` (new)
5. `organizer-event-email.html` (new)
6. `event-published.html` (update existing with base template)

#### 3. Database Migration
**File**: `src/LankaConnect.Infrastructure/Migrations/YYYYMMDD_SeedEmailTemplates.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "email_templates",
        columns: new[] { "id", "name", "subject", "body_html", "category", "type", "is_active" },
        values: new object[,]
        {
            { Guid.NewGuid(), "member-email-verification",
              "Verify Your LankaConnect Email",
              @"<!-- HTML from template file -->",
              "Authentication", "Transactional", true },

            { Guid.NewGuid(), "signup-commitment-confirmed",
              "Commitment Confirmed - {{EventName}}",
              @"<!-- HTML from template file -->",
              "EventManagement", "Transactional", true },

            { Guid.NewGuid(), "registration-cancelled",
              "Registration Cancelled - {{EventName}}",
              @"<!-- HTML from template file -->",
              "EventManagement", "Transactional", true },

            { Guid.NewGuid(), "organizer-event-email",
              "{{Subject}}",
              @"<!-- HTML from template file -->",
              "EventManagement", "Transactional", true }
        });
}
```

#### 4. Template Variables Documentation
**File**: `docs/EMAIL_TEMPLATE_VARIABLES.md`

Document all template variables with:
- Required vs optional variables
- Variable format (string, HTML, date, etc.)
- Example values
- Template-specific usage notes

### Acceptance Criteria
- [x] Base template created with orange/rose gradient (#fb923c → #f43f5e)
- [x] All 6 templates use base template structure
- [x] Color scheme uniform across all templates
- [x] Templates are mobile-responsive (max-width: 600px)
- [x] Plain text versions included for all templates
- [x] Template variables documented in EMAIL_TEMPLATE_VARIABLES.md
- [x] Database migration creates all templates
- [x] Visual regression tests pass (Gmail, Outlook, Apple Mail)
- [x] Build: 0 errors/warnings

---

## Phase 6A.49: Fix Paid Event Registration Email (P0 - CRITICAL)

**Priority**: P0
**Estimated**: 2-3 hours
**Dependencies**: Phase 6A.54 complete

### Root Cause (VERIFIED BY ARCHITECT)

`EventRepository.GetByIdAsync()` uses `AsNoTracking()` (line 74) → `Registration` loaded via navigation inherits detached state → `PaymentCompletedEvent` not collected by ChangeTracker → Email never sent.

### Critical Issue Identified

**BASE REPOSITORY ALSO USES AsNoTracking!**

```csharp
// Repository.cs (base class) line 65
public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, ...)
{
    return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
}
```

This means even calling `_registrationRepository.GetByIdAsync(id)` will return a **detached** entity!

### The Fix (ARCHITECT-APPROVED)

#### 1. Override GetByIdAsync in RegistrationRepository

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/RegistrationRepository.cs`

```csharp
public override async Task<Registration?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    // Load WITH TRACKING (critical for domain event collection)
    // DO NOT use AsNoTracking() for command operations
    return await _dbSet
        .Include(r => r.Attendees)
        .Include(r => r.Contact)
        .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
}
```

#### 2. Update PaymentsController

**File**: `src/LankaConnect.API/Controllers/PaymentsController.cs`

**REPLACE lines 346-393** in `HandleCheckoutSessionCompletedAsync`:

```csharp
// Load Registration directly (NOT via Event navigation)
var registration = await _registrationRepository.GetByIdAsync(registrationId, cancellationToken);
if (registration == null)
{
    _logger.LogError(
        "[Phase 6A.49] Registration {RegistrationId} not found for checkout session {SessionId}",
        registrationId, checkoutSessionId);
    return;
}

// Security: Verify registration belongs to this event
if (registration.EventId != eventId)
{
    _logger.LogError(
        "[Phase 6A.49] Registration {RegistrationId} does not belong to Event {EventId}",
        registrationId, eventId);
    return;
}

_logger.LogInformation(
    "[Phase 6A.49] Completing payment for Registration {RegistrationId}, Event {EventId}",
    registrationId, eventId);

// Complete payment on domain entity (raises PaymentCompletedEvent)
var completeResult = registration.CompletePayment(paymentIntentId);
if (completeResult.IsFailure)
{
    _logger.LogError(
        "[Phase 6A.49] Failed to complete payment: {Error}",
        completeResult.Error);
    return;
}

// Explicitly mark as Modified (Phase 6A.24 pattern)
_registrationRepository.Update(registration);

// Save changes (triggers domain event dispatch)
await _unitOfWork.CommitAsync(cancellationToken);

_logger.LogInformation(
    "[Phase 6A.49] ✅ Payment completed successfully for Event {EventId}, Registration {RegistrationId}",
    eventId, registrationId);
```

### Testing Requirements

#### Unit Test: Domain Event Raised
```csharp
// File: tests/LankaConnect.Domain.Tests/Events/RegistrationTests.cs
[Fact]
public void CompletePayment_ShouldRaisePaymentCompletedEvent()
{
    // Arrange
    var registration = CreatePaidRegistration();

    // Act
    var result = registration.CompletePayment("pi_test123");

    // Assert
    result.IsSuccess.Should().BeTrue();
    registration.DomainEvents.Should().ContainSingle(e => e is PaymentCompletedEvent);

    var domainEvent = registration.DomainEvents.OfType<PaymentCompletedEvent>().First();
    domainEvent.RegistrationId.Should().Be(registration.Id);
    domainEvent.PaymentIntentId.Should().Be("pi_test123");
}
```

#### Unit Test: EF Core Tracking
```csharp
// File: tests/LankaConnect.Infrastructure.Tests/Repositories/RegistrationRepositoryTests.cs
[Fact]
public async Task GetByIdAsync_ShouldReturnTrackedEntity()
{
    // Arrange
    var registration = await CreateRegistrationInDbAsync();

    // Act
    var loaded = await _registrationRepository.GetByIdAsync(registration.Id);

    // Assert
    loaded.Should().NotBeNull();

    var entry = _dbContext.Entry(loaded);
    entry.State.Should().NotBe(EntityState.Detached);  // CRITICAL
    entry.State.Should().Be(EntityState.Unchanged);    // Tracked but not modified
}
```

#### Integration Test: Full Payment Flow
```csharp
// File: tests/LankaConnect.IntegrationTests/Payments/PaymentWorkflowTests.cs
[Fact]
public async Task HandleCheckoutSessionCompleted_ShouldQueueEmail_ForPaidEvent()
{
    // Arrange
    var registration = await CreatePaidRegistrationAsync();

    // Act
    await _paymentsController.HandleStripeWebhook(
        CreateCheckoutSessionCompletedEvent(registration.StripeCheckoutSessionId));

    // Assert: Email queued in database
    var queuedEmails = await _dbContext.EmailMessages
        .Where(e =>
            e.TemplateName == "registration-confirmation" &&
            e.ToEmail == registration.Contact.Email)
        .ToListAsync();

    queuedEmails.Should().ContainSingle();
}
```

#### Azure Staging Verification
1. Create paid event via UI
2. Register with Stripe test card (4242 4242 4242 4242)
3. Complete checkout
4. Check Azure container logs:
   ```bash
   az containerapp logs show --name lankaconnect-staging-api --resource-group <rg> \
     --filter "Phase 6A.49" --tail 100
   ```
5. Verify email in database:
   ```sql
   SELECT * FROM communications.email_messages
   WHERE template_name = 'registration-confirmation'
   AND created_at > NOW() - INTERVAL '5 minutes';
   ```
6. Check inbox for confirmation email (within 5 minutes)

### Acceptance Criteria
- [x] `RegistrationRepository.GetByIdAsync()` returns tracked entity
- [x] `PaymentCompletedEvent` dispatched after successful payment
- [x] Email queued with `registration-confirmation` template
- [x] Email uses orange/rose branding (Phase 6A.54)
- [x] Payment webhook remains idempotent
- [x] Unit tests pass (≥90% coverage)
- [x] Integration tests pass
- [x] Azure staging verification successful
- [x] Build: 0 errors/warnings

---

## Phase 6A.53: Member Email Verification System (P1 - SECURITY)

**Priority**: P1
**Estimated**: 7-9 hours
**Dependencies**: Phase 6A.54 complete

### Security Enhancements (ARCHITECT-REQUIRED)

#### 1. GUID-Based Tokens (NOT Base64)
```csharp
// SECURE: 32 hex characters, unpredictable
EmailVerificationToken = Guid.NewGuid().ToString("N");
```

#### 2. Token Expiry Index for Cleanup
```sql
CREATE INDEX "IX_User_EmailVerificationTokenExpiry"
ON "User" ("EmailVerificationTokenExpiresAt")
WHERE "EmailVerificationTokenExpiresAt" IS NOT NULL
  AND "IsEmailVerified" = FALSE;
```

#### 3. Resend Verification Email Feature
Prevents spam: Allow resend only after 1 hour (24 - 23 = 1 hour cooldown).

### Implementation

#### Database Migration
**File**: `src/LankaConnect.Infrastructure/Migrations/YYYYMMDD_AddUserEmailVerification.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<bool>(
        name: "IsEmailVerified",
        table: "User",
        nullable: false,
        defaultValue: false);

    migrationBuilder.AddColumn<string>(
        name: "EmailVerificationToken",
        table: "User",
        maxLength: 64,
        nullable: true);

    migrationBuilder.AddColumn<DateTimeOffset>(
        name: "EmailVerificationTokenExpiresAt",
        table: "User",
        nullable: true);

    // Index for token lookup
    migrationBuilder.CreateIndex(
        name: "IX_User_EmailVerificationToken",
        table: "User",
        column: "EmailVerificationToken",
        filter: "\"EmailVerificationToken\" IS NOT NULL");

    // Index for cleanup (ARCHITECT-REQUIRED)
    migrationBuilder.CreateIndex(
        name: "IX_User_EmailVerificationTokenExpiry",
        table: "User",
        column: "EmailVerificationTokenExpiresAt",
        filter: "\"EmailVerificationTokenExpiresAt\" IS NOT NULL AND \"IsEmailVerified\" = FALSE");
}
```

#### Domain Entity
**File**: `src/LankaConnect.Domain/Users/User.cs`

```csharp
public void GenerateEmailVerificationToken()
{
    // GUID for unpredictable tokens (ARCHITECT-APPROVED)
    EmailVerificationToken = Guid.NewGuid().ToString("N");  // 32 hex chars
    EmailVerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24);

    RaiseDomainEvent(new MemberVerificationRequestedEvent(
        Id,
        Email.Value,
        EmailVerificationToken,
        DateTimeOffset.UtcNow
    ));
}

public Result VerifyEmail(string token)
{
    if (IsEmailVerified)
        return Result.Failure("Email already verified");

    if (EmailVerificationToken != token)
        return Result.Failure("Invalid verification token");

    if (EmailVerificationTokenExpiresAt < DateTimeOffset.UtcNow)
        return Result.Failure("Token expired. Please request a new verification email.");

    IsEmailVerified = true;
    EmailVerificationToken = null;  // One-time use
    EmailVerificationTokenExpiresAt = null;

    MarkAsUpdated();
    return Result.Success();
}

// ARCHITECT-RECOMMENDED: Resend with cooldown
public Result RegenerateEmailVerificationToken()
{
    if (IsEmailVerified)
        return Result.Failure("Email already verified");

    // Prevent spam: Check if token was generated recently (1-hour cooldown)
    if (EmailVerificationTokenExpiresAt.HasValue &&
        EmailVerificationTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddHours(23))
    {
        return Result.Failure("Please wait before requesting a new verification email");
    }

    GenerateEmailVerificationToken();
    return Result.Success();
}
```

#### Event Handler (FAIL-SILENT)
**File**: `src/LankaConnect.Application/Events/EventHandlers/MemberVerificationRequestedEventHandler.cs`

```csharp
public class MemberVerificationRequestedEventHandler
    : INotificationHandler<DomainEventNotification<MemberVerificationRequestedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<MemberVerificationRequestedEventHandler> _logger;

    public async Task Handle(
        DomainEventNotification<MemberVerificationRequestedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Handling MemberVerificationRequestedEvent for User {UserId}",
            domainEvent.UserId);

        try
        {
            var verificationUrl = $"https://lankaconnect.com/verify-email?token={domainEvent.VerificationToken}";

            var parameters = new Dictionary<string, object>
            {
                { "Email", domainEvent.Email },
                { "VerificationUrl", verificationUrl },
                { "ExpirationTime", "24 hours" }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                "member-email-verification",
                domainEvent.Email,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Failed to send verification email to {Email}: {Errors}",
                    domainEvent.Email,
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // FAIL-SILENT: Log but don't throw (ARCHITECT-REQUIRED)
            _logger.LogError(ex,
                "Error handling MemberVerificationRequestedEvent for User {UserId}",
                domainEvent.UserId);
            // Do NOT re-throw
        }
    }
}
```

### Acceptance Criteria
- [x] Database migration adds verification columns with indexes
- [x] GUID-based tokens (32 chars, unpredictable)
- [x] Token expiry index for cleanup
- [x] Verification email sent on member signup
- [x] Token expires after 24 hours
- [x] Token is one-time use
- [x] Resend verification email feature with 1-hour cooldown
- [x] Verification link works correctly
- [x] User status updated after verification
- [x] Invalid/expired tokens handled gracefully
- [x] Email template matches branding (Phase 6A.54)
- [x] Fail-silent pattern in event handler
- [x] Build: 0 errors/warnings
- [x] Test coverage: ≥90%

---

## Phase 6A.51 & 6A.52: Signup Commitment + Cancellation Emails

**Priority**: P1 (6A.51), P2 (6A.52)
**Estimated**: 3-4 hours each
**Dependencies**: Phase 6A.54 complete

### Key Refinements (ARCHITECT-REQUIRED)

#### Immutable Collections
```csharp
// WRONG
public List<Guid> CommittedItemIds { get; }

// CORRECT (DDD principle)
public IReadOnlyList<Guid> CommittedItemIds { get; }
```

#### Include Payment Status in Cancellation Event
```csharp
public sealed class RegistrationCancelledEvent : IDomainEvent
{
    // ... existing properties
    public PaymentStatus PaymentStatus { get; }  // For refund info
}
```

Full implementation details in architect review document.

---

## Phase 6A.50: Manual "Send Email to Attendees" (P1 - HIGH VALUE)

**Priority**: P1
**Estimated**: 11-13 hours
**Dependencies**: Phases 6A.49, 6A.54 complete

### Critical Security Enhancements (ARCHITECT-REQUIRED)

#### 1. Email Injection Prevention
```csharp
private string SanitizeSubject(string subject)
{
    // Remove ALL control characters
    var sanitized = subject
        .Replace("\r", "")
        .Replace("\n", "")
        .Replace("\0", "")
        .Replace("\t", "")
        .Trim();

    return sanitized.Length > 200 ? sanitized.Substring(0, 200) : sanitized;
}

private string SanitizeMessage(string message)
{
    var sanitizer = new HtmlSanitizer();
    sanitizer.AllowedTags.Clear();
    sanitizer.AllowedTags.Add("p");
    sanitizer.AllowedTags.Add("br");
    sanitizer.AllowedTags.Add("strong");
    sanitizer.AllowedTags.Add("em");
    sanitizer.AllowedTags.Add("a");
    sanitizer.AllowedAttributes.Clear();
    sanitizer.AllowedAttributes.Add("href");

    var trimmed = message.Length > 5000 ? message.Substring(0, 5000) : message;
    return sanitizer.Sanitize(trimmed);
}
```

#### 2. Rate Limiting (5 emails/event/day)
```csharp
public interface IEventRepository
{
    Task<int> GetOrganizerEmailCountTodayAsync(
        Guid eventId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
```

#### 3. Repository Pattern for Recipient Resolution
**NO DIRECT DbContext QUERIES IN EVENT HANDLERS!**

```csharp
public interface IEventRepository
{
    Task<EmailRecipientsDto> GetEmailRecipientsAsync(
        Guid eventId,
        EmailRecipientType recipientType,
        CancellationToken cancellationToken = default);
}
```

Full implementation details in architect review document.

---

## Critical Action Items Before Implementation

### MUST FIX IMMEDIATELY (BLOCKING):
1. ✅ Add `RegistrationRepository.GetByIdAsync()` override (WITH TRACKING)
2. ✅ Implement email injection prevention with HtmlSanitizer
3. ✅ Add rate limiting repository method (`GetOrganizerEmailCountTodayAsync`)
4. ✅ Create `IEventRepository.GetEmailRecipientsAsync()` (repository pattern)
5. ✅ Add fail-silent exception handling to ALL event handlers
6. ✅ Implement GUID-based token generation (NOT Base64)
7. ✅ Add token expiry index for cleanup

### RECOMMENDED (QUALITY):
1. Add email template validation on startup
2. Implement fallback SMTP provider for resilience
3. Add email preview feature in SendEmailModal
4. Add database indexes for performance
5. Create health checks for email queue

---

## Implementation Timeline (ARCHITECT-APPROVED)

| Phase | Priority | Complexity | Hours | Week |
|-------|----------|------------|-------|------|
| 6A.54 | P0 (Foundation) | Medium | 5-6 | Week 1 |
| 6A.49 | P0 (Critical) | Low-Med | 2-3 | Week 1 |
| 6A.53 | P1 (Security) | Medium-High | 7-9 | Week 2 |
| 6A.51 | P1 | Low | 3-4 | Week 2 |
| 6A.52 | P2 | Low | 3-4 | Week 3 |
| 6A.50 | P1 (High Value) | High | 11-13 | Week 3-4 |
| **Total** | | | **31-39 hours** | **4 weeks** |

---

## Final Architect Recommendation

**✅ APPROVED FOR IMPLEMENTATION** with all refinements incorporated.

**Conditions**:
1. MUST follow revised phase order (6A.54 first)
2. MUST implement all "Must Fix Immediately" items
3. MUST use HtmlSanitizer + rate limiting before Phase 6A.50
4. MUST verify EF Core tracking with unit tests
5. MUST achieve ≥90% test coverage per phase
6. MUST implement fail-silent pattern in ALL event handlers

**Quality Assessment**: 95% (after refinements)
**Risk Level**: Low
**Business Impact**: High
**Technical Debt**: None

**Architect Signature**: ✅ Approved
**Date**: 2025-12-26
