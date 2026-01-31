# Root Cause Analysis: Issue #29 - Guest User Paid Event Registration Email Not Sent

## Executive Summary

Guest users are not receiving confirmation emails after registering for paid events. This RCA identifies the email sending flow, analyzes potential failure points, and provides a prioritized list of likely root causes with recommended fixes.

---

## 1. Email Sending Flow for Guest Paid Event Registration

### 1.1 Complete Flow Diagram

```
Guest User Registration Flow for Paid Events:
==============================================

1. Frontend: Guest submits registration form with attendee details
   ↓
2. API: POST /api/events/{id}/register-anonymous
   ↓
3. RegisterAnonymousAttendeeCommandHandler.cs (lines 187-404)
   - Creates Registration entity with Status=Preliminary, PaymentStatus=Pending
   - Creates Stripe Checkout Session
   - Stores checkout session ID on registration
   - Saves to database via _unitOfWork.CommitAsync()
   - Returns Stripe Checkout Session URL to frontend
   ↓
4. Frontend: Redirects user to Stripe Checkout
   ↓
5. Stripe: User completes payment
   ↓
6. Stripe Webhook: checkout.session.completed
   ↓
7. PaymentsController.HandleCheckoutSessionCompletedAsync() (lines 308-446)
   - Extracts registration_id and event_id from Stripe session metadata
   - Loads Registration entity via _registrationRepository.GetByIdAsync()
   - Calls registration.CompletePayment(paymentIntentId)
   - Saves via _unitOfWork.CommitAsync() → dispatches domain events
   ↓
8. Registration.CompletePayment() (lines 295-342)
   - Validates Status == Preliminary
   - Transitions Status: Preliminary → Confirmed
   - Transitions PaymentStatus: Pending → Completed
   - Raises PaymentCompletedEvent domain event
   ↓
9. AppDbContext.CommitAsync() (lines 400-480)
   - Collects all domain events from tracked entities
   - Saves changes to database
   - Dispatches domain events via MediatR
   ↓
10. PaymentCompletedEventHandler.Handle() (lines 59-468)
    - Loads Event and Registration entities
    - Determines recipient email (guest: from domainEvent.ContactEmail)
    - Generates ticket with QR code via _ticketService
    - Renders email template via _emailTemplateService.RenderTemplateAsync()
    - Sends email via _emailService.SendEmailAsync()
```

### 1.2 Key Files in the Flow

| Step | File | Line Numbers | Purpose |
|------|------|--------------|---------|
| 3 | `RegisterAnonymousAttendeeCommandHandler.cs` | 187-404 | Creates Preliminary registration |
| 7 | `PaymentsController.cs` | 308-446 | Handles Stripe webhook |
| 8 | `Registration.cs` | 295-342 | CompletePayment() raises domain event |
| 9 | `AppDbContext.cs` | 400-480 | Dispatches domain events |
| 10 | `PaymentCompletedEventHandler.cs` | 59-468 | Sends confirmation email |
| 10 | `AzureEmailService.cs` | 56-140 | Sends via Azure Communication Services |

---

## 2. Potential Failure Points Analysis

### 2.1 Domain Event Chain Analysis

**Critical Observation**: The `PaymentCompletedEvent` is raised by `Registration.CompletePayment()` and contains:

```csharp
// Registration.cs, lines 331-339
RaiseDomainEvent(new PaymentCompletedEvent(
    EventId,
    Id,              // RegistrationId
    UserId,          // null for guest users
    contactEmail,    // From Contact?.Email ?? AttendeeInfo?.Email?.Value
    paymentIntentId,
    amountPaid,
    attendeeCount,
    DateTime.UtcNow));
```

**For Guest Users**: `UserId` is `null`, and `contactEmail` is extracted from:
1. `Contact?.Email` (from `RegistrationContact` value object)
2. `AttendeeInfo?.Email?.Value` (fallback for legacy format)

---

## 3. Prioritized Root Causes

### 3.1 CRITICAL - Missing CommitAsync() in AzureEmailService (Likelihood: 95%)

**Evidence**:
- Issue #45 analysis confirmed: `email_messages` table is empty (0 records)
- The `AzureEmailService.CreateDomainEmailMessage()` calls `_emailMessageRepository.AddAsync()` but does NOT call `CommitAsync()`

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\AzureEmailService.cs`

**Lines 686-690**:
```csharp
// Save to database
await _emailMessageRepository.AddAsync(domainEmail, cancellationToken);
// BUG: Missing await _unitOfWork.CommitAsync(cancellationToken);

return Result<DomainEmailMessage>.Success(domainEmail);
```

**Impact**:
- Email messages are added to the DbContext but NEVER persisted to database
- The Update() calls on lines 115 and 123 also won't persist because no CommitAsync()
- Email IS sent via Azure (the actual send works), but tracking record is lost

**However**: This explains missing tracking records but NOT missing emails. The email SHOULD still be sent because `SendViaAzureAsync()` is called before/after the tracking updates.

---

### 3.2 HIGH - ContactEmail Extraction for Guest Users (Likelihood: 75%)

**Potential Issue**: The `ContactEmail` in `PaymentCompletedEvent` might be empty for certain guest registration paths.

**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Registration.cs`

**Lines 327-328**:
```csharp
var contactEmail = Contact?.Email ?? AttendeeInfo?.Email?.Value ?? string.Empty;
```

**Analysis**:
- For **new multi-attendee format**: `Contact` is set via `RegistrationContact.Create(email, phone, address)`
- For **legacy format**: `AttendeeInfo` is set via `AttendeeInfo.Create(name, age, address, email, phone)`

**Risk Scenarios**:
1. If `Contact` is null AND `AttendeeInfo` is null → `contactEmail` = empty string
2. If registration created via path that doesn't set contact info → no email sent

**Verification Needed**: Check if `RegistrationContact` is always created with valid email.

---

### 3.3 HIGH - PaymentCompletedEventHandler Not Registered (Likelihood: 40%)

**Potential Issue**: The handler might not be registered in the DI container.

**File**: Check `DependencyInjection.cs` for MediatR handler registration.

**Evidence Required**: Verify that MediatR auto-scans the Application assembly for INotificationHandler implementations.

---

### 3.4 MEDIUM - Template Rendering Failure (Likelihood: 30%)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\PaymentCompletedEventHandler.cs`

**Lines 391-402**:
```csharp
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    EmailTemplateNames.PaidEventRegistration,  // "template-paid-event-registration-confirmation-with-ticket"
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError(
        "[Phase 6A.52] [PaymentEmail-ERROR] Template rendering failed...");
    return;  // SILENT FAILURE - No email sent
}
```

**Risk**: If template rendering fails:
- Handler returns early
- No email is sent
- Error is logged but handler completes "successfully"

**Check**: Verify template exists in database and is active.

---

### 3.5 MEDIUM - Email Sending Failure Silent Handling (Likelihood: 25%)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\PaymentCompletedEventHandler.cs`

**Lines 438-449**:
```csharp
var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

if (result.IsFailure)
{
    _logger.LogError(
        "PaymentCompleted FAILED: Email sending failed...");
    // No retry, no exception thrown - silent failure
}
else
{
    _logger.LogInformation(
        "PaymentCompleted COMPLETE: Email sent successfully...");
}
```

**Issue**: If `SendEmailAsync()` fails:
- Error is logged
- Handler completes without exception
- No retry mechanism
- User never receives email

---

### 3.6 LOW - Difference Between Member and Guest Handling (Likelihood: 20%)

**Analysis of PaymentCompletedEventHandler.cs (lines 116-155)**:

```csharp
// Determine recipient name and email
string recipientName;
string recipientEmail = domainEvent.ContactEmail;  // From PaymentCompletedEvent

if (domainEvent.UserId.HasValue)
{
    // MEMBER: Load user from repository
    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
    if (user != null)
    {
        recipientName = $"{user.FirstName} {user.LastName}";
        recipientEmail = user.Email.Value;  // Override with user's email
    }
    else
    {
        recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
            ? registration.Attendees.First().Name
            : "Guest";
    }
}
else
{
    // GUEST: Use first attendee name or fallback
    recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
        ? registration.Attendees.First().Name
        : "Guest";
    // recipientEmail stays as domainEvent.ContactEmail
}
```

**Key Difference**:
- **Member**: Email is overwritten with `user.Email.Value` (validated)
- **Guest**: Email stays as `domainEvent.ContactEmail` (from Registration.Contact or AttendeeInfo)

**Risk**: If `domainEvent.ContactEmail` is empty/invalid for guests, the email will fail.

---

## 4. Recommended Investigation Steps

### Step 1: Check Logs for PaymentCompletedEvent Dispatch

Search Azure logs for:
```
[Phase 6A.52] [PaymentEmail-
[DIAG-17] About to dispatch domain event: PaymentCompletedEvent
```

### Step 2: Verify Email Sending Logs

Search for:
```
[DIAG-EMAIL] SendEmailAsync START
[DIAG-EMAIL] SendTemplatedEmailAsync START
PaymentCompleted COMPLETE: Email sent successfully
PaymentCompleted FAILED: Email sending failed
```

### Step 3: Verify Template Exists

Query database:
```sql
SELECT name, is_active, subject_template, html_template
FROM communications.email_templates
WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
```

### Step 4: Check Registration Contact Email

Query database for guest registrations:
```sql
SELECT r.id, r.user_id, r.contact_email, r.contact_phone_number, r.status, r.payment_status
FROM events.registrations r
WHERE r.user_id IS NULL
  AND r.payment_status = 'Completed'
ORDER BY r.created_at DESC
LIMIT 10;
```

---

## 5. Recommended Fixes

### Fix 1: Add CommitAsync() to AzureEmailService (Priority: HIGH)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\AzureEmailService.cs`

**Current Code (line 688)**:
```csharp
await _emailMessageRepository.AddAsync(domainEmail, cancellationToken);
return Result<DomainEmailMessage>.Success(domainEmail);
```

**Fixed Code**:
```csharp
await _emailMessageRepository.AddAsync(domainEmail, cancellationToken);
await _unitOfWork.CommitAsync(cancellationToken);  // ADD THIS LINE
return Result<DomainEmailMessage>.Success(domainEmail);
```

**Note**: This requires injecting `IUnitOfWork` into `AzureEmailService`.

---

### Fix 2: Add Validation for ContactEmail in PaymentCompletedEvent (Priority: HIGH)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\PaymentCompletedEventHandler.cs`

**Add at line ~117**:
```csharp
// Validate contact email exists
if (string.IsNullOrWhiteSpace(domainEvent.ContactEmail))
{
    _logger.LogError(
        "[PaymentEmail-CRITICAL] ContactEmail is empty - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
        correlationId, domainEvent.RegistrationId);
    // Try to recover from registration entity
    recipientEmail = registration.Contact?.Email ??
                     registration.AttendeeInfo?.Email?.Value ??
                     string.Empty;

    if (string.IsNullOrWhiteSpace(recipientEmail))
    {
        _logger.LogError(
            "[PaymentEmail-CRITICAL] Cannot determine recipient email - skipping - RegistrationId: {RegistrationId}",
            domainEvent.RegistrationId);
        return;
    }
}
```

---

### Fix 3: Add Retry Mechanism for Email Failures (Priority: MEDIUM)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\PaymentCompletedEventHandler.cs`

Consider implementing Polly retry policy or background job for failed emails.

---

### Fix 4: Add Email Sending Monitoring Alert (Priority: MEDIUM)

Add monitoring for:
- Payment completed events with no corresponding email sent
- Email failures in PaymentCompletedEventHandler
- Empty ContactEmail in PaymentCompletedEvent

---

## 6. Summary of Findings

| # | Issue | Likelihood | Impact | Priority |
|---|-------|------------|--------|----------|
| 1 | Missing CommitAsync in AzureEmailService | 95% | Tracking only | Medium |
| 2 | ContactEmail empty for guest users | 75% | Email not sent | Critical |
| 3 | Handler not registered | 40% | Email not sent | Critical |
| 4 | Template rendering failure | 30% | Email not sent | High |
| 5 | Silent email sending failure | 25% | Email not sent | High |
| 6 | Member vs Guest handling difference | 20% | Email to wrong address | Medium |

---

## 7. Next Steps

1. **Immediate**: Check Azure logs for `[DIAG-EMAIL]` and `[Phase 6A.52]` entries
2. **Immediate**: Query database for guest registration contact emails
3. **Short-term**: Implement Fix 2 (ContactEmail validation)
4. **Medium-term**: Implement Fix 1 (CommitAsync for tracking)
5. **Long-term**: Add email retry mechanism

---

## Document Information

- **Issue**: #29 - Guest User Paid Event Registration Email Not Sent
- **Created**: 2026-01-30
- **Author**: Claude Code (Architecture Agent)
- **Related Issues**: #45 (Organizer Approval Email Not Sent)
- **Phase**: 6A-X (Email System Fixes)
