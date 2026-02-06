# Root Cause Analysis: "Resend Confirmation Email" Button Failures Not Recorded in Email Metrics Dashboard

**Date:** 2026-02-05
**Author:** Claude (System Architect Agent - SPARC)
**Severity:** Medium
**Status:** Root Cause Identified
**Related Issue:** Email Metrics Dashboard > Failures not showing resend ticket email failures

---

## 1. Executive Summary

**Issue:** When a user clicks "Resend Confirmation Email" on the ticket page (`/events/[id]`) and sees a toast error "Failed to resend ticket email. Please try again.", this failure is NOT being recorded in the Email Metrics > Failures dashboard.

**Root Cause:** The `ResendTicketEmailCommandHandler` uses `IEmailService` directly (AzureEmailService) which does NOT inject or call `IEmailMetrics.RecordFailedEmail()`. This is part of a broader architectural issue where the "direct" email path bypasses all metrics recording.

**Classification:** Backend API Issue - Missing Metrics Integration

---

## 2. Code Path Analysis

### 2.1 Frontend Flow

1. **User Action:** User clicks "Resend Confirmation Email" button
2. **Component:** `c:\Work\LankaConnect\web\src\presentation\components\features\events\TicketSection.tsx`
3. **Handler:** `handleResendEmail()` function (lines 90-116)

```typescript
const handleResendEmail = async () => {
  if (isResending) return;

  try {
    setIsResending(true);
    setResendSuccess(false);
    await eventsRepository.resendTicketEmail(eventId);  // <-- API call
    setResendSuccess(true);
    setTimeout(() => setResendSuccess(false), 5000);
  } catch (err: unknown) {
    console.error('Failed to resend email:', err);
    toast.error(errorMessage);  // <-- Toast shown on failure
  } finally {
    setIsResending(false);
  }
};
```

4. **Repository Method:** `eventsRepository.resendTicketEmail(eventId)`
   - File: `c:\Work\LankaConnect\web\src\infrastructure\api\repositories\events.repository.ts`
   - Lines 934-936

```typescript
async resendTicketEmail(eventId: string): Promise<void> {
  await apiClient.post(`${this.basePath}/${eventId}/my-registration/ticket/resend-email`, {});
}
```

### 2.2 Backend API Endpoint

**Endpoint:** `POST /api/events/{eventId}/my-registration/ticket/resend-email`
**Controller:** `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EventsController.cs`
**Method:** `ResendTicketEmail(Guid eventId)` (lines 813-843)

```csharp
[HttpPost("{eventId:guid}/my-registration/ticket/resend-email")]
[Authorize]
public async Task<IActionResult> ResendTicketEmail(Guid eventId)
{
    var userId = User.GetUserId();

    // Get registration
    var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
    var registrationResult = await Mediator.Send(registrationQuery);

    if (registrationResult.IsFailure || registrationResult.Value == null)
    {
        return NotFound(new { message = "You are not registered for this event" });
    }

    // Execute command
    var command = new ResendTicketEmailCommand(registrationResult.Value.Id, userId);
    var result = await Mediator.Send(command);  // <-- Handled by ResendTicketEmailCommandHandler

    if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
    {
        return NotFound(new { message = result.Error });
    }

    return HandleResult(result);
}
```

### 2.3 Command Handler (Where the Gap Is)

**Handler:** `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\ResendTicketEmail\ResendTicketEmailCommandHandler.cs`

```csharp
public class ResendTicketEmailCommandHandler : ICommandHandler<ResendTicketEmailCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;           // <-- DIRECT IEmailService
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResendTicketEmailCommandHandler> _logger;
    // NOTE: NO IEmailMetrics injected!

    public async Task<Result> Handle(ResendTicketEmailCommand request, CancellationToken cancellationToken)
    {
        // ... validation code ...

        // Send email (line 352)
        var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
        if (emailResult.IsFailure)
        {
            // Error is logged, but NO metrics recorded
            _logger.LogError(
                "ResendTicketEmail FAILED: Email sending failed - Email={Email}, RegistrationId={RegistrationId}, Error={Error}",
                recipientEmail, request.RegistrationId, errorMessage);

            return Result.Failure(errorMessage);  // <-- Returns failure, but NO metrics call
        }

        return Result.Success();
    }
}
```

### 2.4 Email Service (No Metrics Recording)

**Service:** `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\AzureEmailService.cs`

```csharp
public class AzureEmailService : IEmailService, IEmailTemplateService
{
    private readonly ILogger<AzureEmailService> _logger;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly EmailSettings _emailSettings;
    private readonly EmailClient? _azureEmailClient;
    // NOTE: NO IEmailMetrics injected!

    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        // ... sends email via Azure ...
        // NO call to _metrics.RecordEmailSent() or _metrics.RecordFailedEmail()
    }
}
```

---

## 3. Root Cause

### 3.1 Primary Root Cause

**The `ResendTicketEmailCommandHandler` uses the "direct" email path via `IEmailService` which does NOT record metrics.**

The LankaConnect codebase has TWO parallel email sending paths:

| Path | Service | Metrics Recording |
|------|---------|-------------------|
| **Path A: Typed** | `ITypedEmailService` -> `TypedEmailServiceAdapter` | YES - Records via `_metrics.RecordEmailSent()` |
| **Path B: Direct** | `IEmailService` -> `AzureEmailService` | **NO** - No metrics calls |

The `ResendTicketEmailCommandHandler` uses Path B (direct), so failures are:
1. Logged to application logs (Serilog)
2. Stored in `EmailMessage` table (marked as Failed)
3. **NOT** recorded in `IEmailMetrics` which feeds the dashboard

### 3.2 Architecture Diagram

```
                                    METRICS RECORDED?
                                          |
    +-------------------+                 v
    | TypedEmailService |---> TypedEmailServiceAdapter ---> YES (RecordEmailSent, RecordFailedEmail)
    | (ITypedEmailService)|          |
    +-------------------+            v
                              IEmailServiceBridge
                                     |
    +-------------------+            v
    | Direct IEmailService |<--------+
    | (AzureEmailService)  |<--- ResendTicketEmailCommandHandler
    +-------------------+    <--- 30+ other handlers
           |
           v                 NO METRICS RECORDED!
    Azure Communication Services
```

### 3.3 Why Other Failures Show in Dashboard

The dashboard DOES show failures from other templates (like `template-event-details-publication`, `direct-email`). This is because:

1. Some handlers might be using `ITypedEmailService` which records metrics
2. The failures you're seeing may be from a different code path that does record metrics
3. Or they were manually added to the metrics system

---

## 4. Impact Assessment

| Metric | Status |
|--------|--------|
| **Resend Ticket Email Failures in Dashboard** | NOT RECORDED |
| **Resend Ticket Email Success in Dashboard** | NOT RECORDED |
| **Resend Ticket Email in Template Stats** | NOT RECORDED |
| **Resend Ticket Email Handler Stats** | NOT RECORDED |
| **Application Logs** | RECORDED (via Serilog) |
| **EmailMessage Table** | RECORDED (status = Failed) |

---

## 5. Fix Recommendations

### 5.1 Option A: Decorator Pattern (Recommended - Non-Breaking)

As documented in `RCA_EMAIL_METRICS_NOT_RECORDED.md`, wrap `AzureEmailService` with a decorator:

```csharp
// New file: MetricsRecordingEmailServiceDecorator.cs
public class MetricsRecordingEmailServiceDecorator : IEmailService
{
    private readonly IEmailService _innerService;
    private readonly IEmailMetrics _metrics;
    private readonly ILogger _logger;

    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var templateName = ExtractTemplateName(emailMessage); // Or pass template name separately

        try
        {
            var result = await _innerService.SendEmailAsync(emailMessage, cancellationToken);
            stopwatch.Stop();

            _metrics.RecordEmailSent(templateName, (int)stopwatch.ElapsedMilliseconds, result.IsSuccess);

            if (result.IsFailure)
            {
                _metrics.RecordFailedEmail(
                    correlationId,
                    templateName,
                    emailMessage.ToEmail,
                    result.Error ?? "Unknown error",
                    "DirectIEmailService");
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordEmailSent(templateName, (int)stopwatch.ElapsedMilliseconds, false);
            _metrics.RecordFailedEmail(correlationId, templateName, emailMessage.ToEmail, ex.Message, "DirectIEmailService");
            throw;
        }
    }
}
```

**DI Registration:**
```csharp
services.AddScoped<AzureEmailService>();
services.AddScoped<IEmailService>(sp =>
    new MetricsRecordingEmailServiceDecorator(
        sp.GetRequiredService<AzureEmailService>(),
        sp.GetRequiredService<IEmailMetrics>(),
        sp.GetRequiredService<ILogger<MetricsRecordingEmailServiceDecorator>>()
    ));
```

### 5.2 Option B: Modify Handler Directly (Quick Fix)

Add metrics recording directly to `ResendTicketEmailCommandHandler`:

```csharp
public class ResendTicketEmailCommandHandler : ICommandHandler<ResendTicketEmailCommand>
{
    private readonly IEmailMetrics _emailMetrics;  // ADD THIS
    // ... other dependencies ...

    public async Task<Result> Handle(ResendTicketEmailCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();

        // ... existing code ...

        var emailResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
        stopwatch.Stop();

        // ADD METRICS RECORDING
        _emailMetrics.RecordEmailSent(
            EmailTemplateNames.PaidEventRegistration,
            (int)stopwatch.ElapsedMilliseconds,
            emailResult.IsSuccess);

        if (emailResult.IsFailure)
        {
            _emailMetrics.RecordFailedEmail(
                correlationId,
                EmailTemplateNames.PaidEventRegistration,
                recipientEmail,
                emailResult.Error ?? "Unknown error",
                "ResendTicketEmailCommandHandler");

            return Result.Failure(errorMessage);
        }

        return Result.Success();
    }
}
```

### 5.3 Option C: Migrate to ITypedEmailService (Long-term)

Create a typed parameter class for paid event registration emails and migrate the handler to use `ITypedEmailService`.

---

## 6. Classification

| Category | Value |
|----------|-------|
| **Issue Type** | Backend API |
| **Root Cause** | Missing Metrics Integration |
| **Scope** | `ResendTicketEmailCommandHandler` (and 30+ other handlers) |
| **Fix Complexity** | Low (Option B) / Medium (Option A) |
| **Priority** | Medium |

---

## 7. Related Files

| File | Purpose |
|------|---------|
| `web/src/presentation/components/features/events/TicketSection.tsx` | Frontend button handler |
| `web/src/infrastructure/api/repositories/events.repository.ts` | API repository method |
| `src/LankaConnect.API/Controllers/EventsController.cs` | API endpoint |
| `src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs` | Command handler (missing metrics) |
| `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs` | Email service (no metrics) |
| `src/LankaConnect.Infrastructure/Email/Services/DatabaseEmailMetrics.cs` | Metrics recording service |
| `docs/RCA_EMAIL_METRICS_NOT_RECORDED.md` | Related broader RCA |

---

## 8. Testing Verification

After implementing the fix:

1. Click "Resend Confirmation Email" button on a ticket page
2. Force a failure (e.g., by disconnecting network or using invalid email)
3. Navigate to Email Metrics > Failures dashboard
4. Verify the failure appears with:
   - Template name: `template-paid-event-registration`
   - Handler name: `ResendTicketEmailCommandHandler` (or `DirectIEmailService` for Option A)
   - Error message: The actual failure reason
   - Timestamp: Recent

---

## 9. Conclusion

The "Resend Confirmation Email" button failures are NOT being recorded in the Email Metrics dashboard because:

1. `ResendTicketEmailCommandHandler` uses direct `IEmailService` calls
2. `AzureEmailService` does not inject or call `IEmailMetrics`
3. This is part of a broader architectural issue affecting 30+ handlers

**Recommended Fix:** Implement the Decorator Pattern (Option A) to automatically record metrics for ALL handlers without modifying each one individually. This provides a single point of change that fixes the issue for all direct `IEmailService` callers.

---

**Document Version:** 1.0
**Last Updated:** 2026-02-05
