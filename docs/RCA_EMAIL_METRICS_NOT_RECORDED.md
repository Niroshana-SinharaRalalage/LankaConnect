# Root Cause Analysis: Email Metrics Not Being Recorded

**Date:** 2026-01-30
**Author:** Claude (System Architect)
**Severity:** High
**Status:** Analysis Complete, Fix Plan Ready

---

## 1. Executive Summary

The Email Metrics Dashboard shows 0 for all metrics despite:
1. Emails being successfully sent (confirmed via Hangfire showing succeeded jobs)
2. `IEmailMetrics` interface and `DefaultEmailMetrics` implementation existing
3. Dashboard UI and API endpoints working correctly

**Root Cause:** There are TWO parallel email sending paths in the codebase:
- **Path A (Typed):** Goes through `ITypedEmailService` -> `TypedEmailServiceAdapter` -> Records metrics
- **Path B (Direct):** Goes directly to `IEmailService` (AzureEmailService) -> **DOES NOT record metrics**

Most handlers use Path B (direct), bypassing the metrics recording entirely.

---

## 2. System Architecture Analysis

### 2.1 Current Email Service Architecture

```
                                    METRICS RECORDED
                                          |
    +-------------------+                 v
    | TypedEmailService |---> TypedEmailServiceAdapter ---> IEmailServiceBridge
    | (ITypedEmailService)|     (records metrics)              |
    +-------------------+                                       |
                                                               v
                                                    EmailServiceBridgeAdapter
                                                               |
    +-------------------+                                       |
    | Direct IEmailService |<----------------------------------+
    | (AzureEmailService)  |<--- EventCancellationEmailJob
    +-------------------+   <--- RegistrationConfirmedEventHandler
           |                <--- RefundCompletedEventHandler
           |                <--- RefundRequestedEventHandler
           |                <--- All other handlers (40+ files)
           |
           v                 METRICS NOT RECORDED!
    Azure Communication Services
```

### 2.2 Service Registration (DependencyInjection.cs)

```csharp
// IEmailService -> AzureEmailService (direct, no metrics)
services.AddScoped<IEmailService, AzureEmailService>();  // Line 204

// ITypedEmailService -> TypedEmailServiceAdapter (with metrics)
services.AddTypedEmailServices(configuration);           // Line 278
services.AddEmailServiceBridge();                        // Line 279
```

### 2.3 Two Parallel Paths Identified

**Path A: Typed Email Service (RECORDS METRICS)**
```
EventReminderJob
    |
    v
ITypedEmailService.SendEmailAsync(emailParams, handlerName)
    |
    v
TypedEmailServiceAdapter
    |
    +---> _metrics.RecordHandlerUsage(handlerName, useTypedParameters)
    +---> _metrics.RecordEmailSent(templateName, durationMs, success)
    +---> _metrics.RecordParameterValidationFailure(templateName) [on error]
    |
    v
IEmailServiceBridge.SendTemplatedEmailAsync()
    |
    v
EmailServiceBridgeAdapter
    |
    v
IEmailService.SendTemplatedEmailAsync() -> AzureEmailService
```

**Path B: Direct IEmailService (DOES NOT RECORD METRICS)**
```
EventCancellationEmailJob
RegistrationConfirmedEventHandler
RefundCompletedEventHandler
RefundRequestedEventHandler
PaymentCompletedEventHandler
... and 40+ other handlers
    |
    v
IEmailService.SendTemplatedEmailAsync() -> AzureEmailService
    |
    v
Azure Communication Services

    METRICS NEVER RECORDED!
```

---

## 3. Code Evidence

### 3.1 EventReminderJob (Uses Typed - RECORDS METRICS)

```csharp
// File: src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs
// Line 252-256

var typedResult = await _typedEmailService.SendEmailAsync(
    emailParams,
    HandlerName,  // "EventReminderJob"
    cancellationToken);
```

### 3.2 EventCancellationEmailJob (Uses Direct - NO METRICS)

```csharp
// File: src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs
// Line 266-270

var result = await _emailService.SendTemplatedEmailAsync(
    EmailTemplateNames.EventCancellation,
    email,
    recipientParameters,
    CancellationToken.None);
```

### 3.3 TypedEmailServiceAdapter (Where Metrics ARE Recorded)

```csharp
// File: src/LankaConnect.Shared/Email/Services/TypedEmailServiceAdapter.cs
// Lines 74, 88, 111

// Records handler usage (typed vs dictionary)
_metrics.RecordHandlerUsage(handlerName, useTypedParameters);  // Line 74

// Records validation failure
_metrics.RecordParameterValidationFailure(emailParams.TemplateName);  // Line 88

// Records email sent with duration and success
_metrics.RecordEmailSent(emailParams.TemplateName, durationMs, success);  // Line 111
```

### 3.4 AzureEmailService (Where Metrics ARE NOT Recorded)

```csharp
// File: src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs

public class AzureEmailService : IEmailService, IEmailTemplateService
{
    // Constructor does NOT inject IEmailMetrics
    public AzureEmailService(
        ILogger<AzureEmailService> logger,
        IEmailMessageRepository emailMessageRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IOptions<EmailSettings> emailSettings)
    {
        // No IEmailMetrics!
    }

    // SendEmailAsync and SendTemplatedEmailAsync do NOT call metrics
}
```

---

## 4. Handlers Analysis

### 4.1 Handlers Using Typed Email Service (Metrics Recorded)

| Handler | File | Metrics Status |
|---------|------|----------------|
| EventReminderJob | BackgroundJobs/EventReminderJob.cs | RECORDED |

**Total: 1 handler**

### 4.2 Handlers Using Direct IEmailService (Metrics NOT Recorded)

| Handler | File |
|---------|------|
| EventCancellationEmailJob | BackgroundJobs/EventCancellationEmailJob.cs |
| RegistrationConfirmedEventHandler | EventHandlers/RegistrationConfirmedEventHandler.cs |
| PaymentCompletedEventHandler | EventHandlers/PaymentCompletedEventHandler.cs |
| RefundCompletedEventHandler | EventHandlers/RefundCompletedEventHandler.cs |
| RefundRequestedEventHandler | EventHandlers/RefundRequestedEventHandler.cs |
| AnonymousRegistrationConfirmedEventHandler | EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs |
| RegistrationPendingPaymentEventHandler | EventHandlers/RegistrationPendingPaymentEventHandler.cs |
| RegistrationCancelledEventHandler | EventHandlers/RegistrationCancelledEventHandler.cs |
| EventApprovedEventHandler | EventHandlers/EventApprovedEventHandler.cs |
| EventPublishedEventHandler | EventHandlers/EventPublishedEventHandler.cs |
| UserCommittedToSignUpEventHandler | EventHandlers/UserCommittedToSignUpEventHandler.cs |
| CommitmentUpdatedEventHandler | EventHandlers/CommitmentUpdatedEventHandler.cs |
| CommitmentCancelledEmailHandler | EventHandlers/CommitmentCancelledEmailHandler.cs |
| MemberVerificationRequestedEventHandler | EventHandlers/MemberVerificationRequestedEventHandler.cs |
| NewsletterEmailJob | BackgroundJobs/NewsletterEmailJob.cs |
| EventNotificationEmailJob | BackgroundJobs/EventNotificationEmailJob.cs |
| SendPasswordResetCommandHandler | Commands/SendPasswordReset/ |
| SendWelcomeEmailCommandHandler | Commands/SendWelcomeEmail/ |
| ResetPasswordCommandHandler | Commands/ResetPassword/ |
| VerifyEmailCommandHandler | Commands/VerifyEmail/ |
| ApproveRoleUpgradeCommandHandler | Commands/ApproveRoleUpgrade/ |
| SubscribeToNewsletterCommandHandler | Commands/SubscribeToNewsletter/ |
| AdminLockUserCommandHandler | Commands/AdminLockUser/ |
| AdminUnlockUserCommandHandler | Commands/AdminUnlockUser/ |
| AdminActivateUserCommandHandler | Commands/AdminActivateUser/ |
| AdminDeactivateUserCommandHandler | Commands/AdminDeactivateUser/ |
| CreateSupportTicketCommandHandler | Commands/CreateSupportTicket/ |
| ReplySupportTicketCommandHandler | Commands/ReplySupportTicket/ |
| ResendTicketEmailCommandHandler | Commands/ResendTicketEmail/ |
| SendBusinessNotificationCommandHandler | Commands/SendBusinessNotification/ |
| RegistrationEmailService | Infrastructure/Services/ |

**Total: 30+ handlers NOT recording metrics**

---

## 5. EventDateTime Parameter Issue Analysis

### 5.1 Issue Description

An email was received with `{{EventDateTime}}` showing as literal text instead of the actual date value.

### 5.2 Root Cause

**Parameter Name Mismatch:**

The `template-event-reminder` template uses these parameters:
- `{{EventStartDate}}` - Date formatted as "MMMM dd, yyyy"
- `{{EventStartTime}}` - Time formatted as "h:mm tt"

However, some templates expect a combined `{{EventDateTime}}` parameter.

### 5.3 Template vs Handler Analysis

**template-event-reminder parameters (from migration):**
```
AttendeeName, EventTitle, EventStartDate, EventStartTime, Location,
Quantity, HoursUntilEvent, ReminderTimeframe, ReminderMessage,
EventDetailsUrl, HasOrganizerContact, OrganizerContactName,
OrganizerContactEmail, OrganizerContactPhone
```

**EventReminderEmailParams.ToDictionary() provides:**
```csharp
{ "AttendeeName", AttendeeName },
{ "EventTitle", EventTitle },
{ "EventStartDate", EventStartDate.ToString("MMMM dd, yyyy") },
{ "EventStartTime", EventStartTime },
{ "Location", Location },
{ "Quantity", Quantity },
// ... etc
```

**EventCancellationEmailJob provides:**
```csharp
["EventStartDate"] = @event.StartDate.ToString("MMMM dd, yyyy"),
["EventStartTime"] = @event.StartDate.ToString("h:mm tt"),
["EventDateTime"] = @event.StartDate.ToString("MMMM dd, yyyy 'at' h:mm tt"),
```

### 5.4 Conclusion

The `{{EventDateTime}}` literal text issue is NOT from `template-event-reminder` but likely from another template (e.g., `template-event-cancellation-notifications`) that expects `EventDateTime` but:

1. Either the handler is not providing it, OR
2. The template in the database has been modified incorrectly

This needs verification by checking the database template content.

---

## 6. Impact Assessment

### 6.1 Dashboard Impact

- **Total Emails Sent:** Shows 0 (should show actual count)
- **Success Rate:** Shows 0% (should show ~95%+)
- **Template Metrics:** Empty (should show per-template stats)
- **Handler Metrics:** Only EventReminderJob data (missing 30+ handlers)

### 6.2 Operational Impact

- Cannot monitor email system health
- Cannot detect delivery failures
- Cannot track template usage patterns
- Cannot identify parameter validation issues

---

## 7. Fix Plan

### 7.1 Option A: Wrap AzureEmailService with Metrics (Recommended)

Create a decorator pattern to wrap `AzureEmailService`:

```csharp
// New file: src/LankaConnect.Infrastructure/Email/Services/MetricsRecordingEmailServiceDecorator.cs

public class MetricsRecordingEmailServiceDecorator : IEmailService
{
    private readonly IEmailService _innerService;
    private readonly IEmailMetrics _metrics;
    private readonly ILogger<MetricsRecordingEmailServiceDecorator> _logger;

    public MetricsRecordingEmailServiceDecorator(
        IEmailService innerService,
        IEmailMetrics metrics,
        ILogger<MetricsRecordingEmailServiceDecorator> logger)
    {
        _innerService = innerService;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _innerService.SendTemplatedEmailAsync(
                templateName, recipientEmail, parameters, cancellationToken);

            stopwatch.Stop();

            // Record metrics
            _metrics.RecordEmailSent(templateName, (int)stopwatch.ElapsedMilliseconds, result.IsSuccess);

            // Record handler usage (unknown handler since called directly)
            _metrics.RecordHandlerUsage("DirectIEmailService", usedTypedParameters: false);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordEmailSent(templateName, (int)stopwatch.ElapsedMilliseconds, success: false);
            throw;
        }
    }

    // Implement other IEmailService methods similarly...
}
```

**DI Registration Update:**
```csharp
// In DependencyInjection.cs
services.AddScoped<AzureEmailService>();
services.AddScoped<IEmailService>(sp =>
    new MetricsRecordingEmailServiceDecorator(
        sp.GetRequiredService<AzureEmailService>(),
        sp.GetRequiredService<IEmailMetrics>(),
        sp.GetRequiredService<ILogger<MetricsRecordingEmailServiceDecorator>>()
    ));
```

### 7.2 Option B: Migrate All Handlers to ITypedEmailService

This is the long-term solution but requires:
1. Creating typed parameter classes for each email template
2. Updating all 30+ handlers to use `ITypedEmailService`
3. Extensive testing of each handler

**Estimated Effort:** 2-3 weeks

### 7.3 Option C: Add Metrics to AzureEmailService Directly

Modify `AzureEmailService` to inject and call `IEmailMetrics`.

**Pros:** Simplest change
**Cons:** Mixes concerns (infrastructure service doing observability)

### 7.4 Recommended Approach

**Phase 1 (Immediate - 1 day):**
- Implement Option A (Decorator) for immediate metrics capture
- All existing handlers automatically get metrics without code changes

**Phase 2 (Long-term):**
- Continue migrating handlers to `ITypedEmailService` (Option B)
- Typed parameters provide compile-time safety
- Better validation and error handling

---

## 8. Implementation Tasks

### 8.1 High Priority (Must Fix)

1. [ ] Create `MetricsRecordingEmailServiceDecorator` class
2. [ ] Update DI registration to use decorator pattern
3. [ ] Add unit tests for decorator
4. [ ] Deploy to staging and verify metrics appear

### 8.2 Medium Priority

5. [ ] Verify `EventDateTime` parameter issue by checking database templates
6. [ ] Add missing parameters to handlers if needed
7. [ ] Update EMAIL_TEMPLATE_PARAMETER_MANIFEST.md

### 8.3 Low Priority (Long-term)

8. [ ] Create typed parameter classes for all templates
9. [ ] Migrate remaining handlers to ITypedEmailService
10. [ ] Remove decorator once all handlers migrated

---

## 9. Verification Steps

After fix deployment:

1. Send a test email via any handler
2. Check Email Metrics Dashboard
3. Verify:
   - Total Emails Sent > 0
   - Template metrics populated
   - Handler metrics show "DirectIEmailService" entries

---

## 10. Related Documents

- `c:\Work\LankaConnect\docs\EMAIL_TEMPLATE_PARAMETER_MANIFEST.md`
- `c:\Work\LankaConnect\docs\PHASE_6A87_DAY1_FOUNDATION_SUMMARY.md`
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Observability\IEmailMetrics.cs`

---

## Appendix A: Email Flow Diagram

```
+-----------------------------------------------------------------------------------+
|                           EMAIL SENDING PATHS                                      |
+-----------------------------------------------------------------------------------+

PATH A: Typed Email Service (METRICS RECORDED)
============================================

  Handler (e.g., EventReminderJob)
          |
          v
  ITypedEmailService.SendEmailAsync(emailParams, handlerName)
          |
          v
  +-----------------------------------------------+
  | TypedEmailServiceAdapter                       |
  |-----------------------------------------------|
  | 1. _metrics.RecordHandlerUsage()              |
  | 2. Validate parameters                         |
  | 3. Convert to Dictionary                       |
  | 4. Call IEmailServiceBridge                    |
  | 5. _metrics.RecordEmailSent()                 |
  +-----------------------------------------------+
          |
          v
  EmailServiceBridgeAdapter
          |
          v
  IEmailService (AzureEmailService)
          |
          v
  Azure Communication Services


PATH B: Direct IEmailService (NO METRICS!)
==========================================

  Handler (e.g., EventCancellationEmailJob)
          |
          v
  IEmailService.SendTemplatedEmailAsync()
          |
          v
  +-----------------------------------------------+
  | AzureEmailService                              |
  |-----------------------------------------------|
  | - NO metrics recording                         |
  | - Sends email directly                         |
  +-----------------------------------------------+
          |
          v
  Azure Communication Services


FIX: Add Decorator
==================

  Handler (ANY handler)
          |
          v
  IEmailService (resolves to decorator)
          |
          v
  +-----------------------------------------------+
  | MetricsRecordingEmailServiceDecorator          |
  |-----------------------------------------------|
  | 1. Start stopwatch                             |
  | 2. Call inner AzureEmailService               |
  | 3. _metrics.RecordEmailSent()                 |
  | 4. _metrics.RecordHandlerUsage()              |
  +-----------------------------------------------+
          |
          v
  AzureEmailService
          |
          v
  Azure Communication Services

```

---

**Document Version:** 1.0
**Last Updated:** 2026-01-30